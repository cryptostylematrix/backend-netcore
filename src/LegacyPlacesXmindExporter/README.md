# Legacy places export to XMind

A standalone .NET 10 console application that reads `public.places` from the
legacy PostgreSQL database, joins `public.partners` by
`places.partner_id = partners.id`, filters by `places.structure`, and creates an editable
`.xmind` file (a ZIP archive containing `content.json`, `metadata.json`, and
`manifest.json`). It has no dependencies on CryptoStyle modules.
The format targets modern JSON-based XMind versions, not XMind 8/XML.
Model reference: [official XMind SDK](https://github.com/xmindltd/xmind-sdk-js).

## Running the exporter

From the `backend-netcore` directory:

```bash
cp src/LegacyPlacesXmindExporter/.env.example src/LegacyPlacesXmindExporter/.env
```

Set the connection string for the **legacy** database in the new `.env` file.
The database user needs access to the `public` schema and `SELECT` permission
on `public.places` and `public.partners` (`id` and `login` columns).

```bash
dotnet run --project src/LegacyPlacesXmindExporter -- \
  --env-file src/LegacyPlacesXmindExporter/.env
```

You can set the `LEGACY_PLACES_CONNECTION_STRING` environment variable instead
of using a file. Environment variables take precedence over `.env` values.
The file is loaded only when explicitly specified with `--env-file`.
The connection string is not logged.

The application always asks for the structure number and output filename at
startup. `--structure` and `--output` arguments are no longer accepted.
The current console prompts are in Russian and mean “Structure number” and
“Output filename”:

```text
Номер структуры: 5
Имя выходного файла [places-5.xmind]: places-5.xmind
```

Enter a structure number in the PostgreSQL `smallint` range (-32768 to 32767),
then a path ending in `.xmind`. Press Enter at the filename prompt to use
`places-<structure>.xmind` in the current directory. Invalid answers prompt
for another attempt. End of input without an answer causes an error.
The destination directory must exist. Existing files are not overwritten.
`--help` displays usage without prompting or connecting to the database.
Ctrl+C cancels the export.

To run without the source tree, publish the application:

```bash
dotnet publish src/LegacyPlacesXmindExporter -c Release -o artifacts/legacy-places-exporter
dotnet artifacts/legacy-places-exporter/LegacyPlacesXmindExporter.dll
```

## Map contents

Each place becomes one topic with three title lines for purchased places and
an additional `clone` line for clones. Example of a purchased place:

```text
[12345]
ivan.2
03.02.20 07:05:06
```

- The first line is the legacy `places.place_id` in square brackets, such as `[12345]`.
- The second line is `partners.login` from the legacy database, without
  normalization or lookup of a new-system login. `places.index` is not exported.
- The third line is `places.created_at` formatted as `dd.MM.yy HH:mm:ss` (24-hour
  time, two-digit year, no fractional seconds), converted from UTC to
  `Europe/Moscow`. Although the source column is `timestamp without time zone`,
  its values are interpreted as UTC by agreement with the source system owner.
  Conversion uses Moscow time-zone rules for the creation date, independently
  of the machine's local time zone. For example, `2020-02-03 04:05:06` UTC
  is exported as `03.02.20 07:05:06`. Source database values are not modified.
- A fourth line containing exactly `clone` is added for `p_type=0`. Purchased
  places (`p_type=1`) have no type label or trailing empty line. These are
  **legacy** type values, not the CryptoStyle place type enum.
- Relationships follow `parent_id`. Children are ordered by ascending `pos`
  (starting at zero), using a top-down organization chart layout. Gaps in positions do
  not create empty topics. Numeric positions are not stored; only the relative
  order of children is retained.
- Each root (`parent_id IS NULL`) becomes a separate sheet named after its
  partner login. No artificial places are added.
- `structure`, `partner_id`, `filling`, `hashcode`, and `transaction_id` are not
  added to the map. All places in the selected structure are included,
  regardless of `is_expired`.
- `place_id` is displayed in the title and also used as the internal topic ID. Future processing should
  use the editable login to identify the profile. Multiple places belonging
  to the same partner remain separate topics with the same login.

You can edit titles and move branches in XMind. To prepare for future import,
keep the ID, login, and date lines, with an optional fourth line `clone` only
for clones.
Import into CryptoStyle is outside this project's scope; the import contract
still needs to be implemented.

## Visual appearance

Each sheet uses a shared theme inspired by the supplied reference image:

- A top-down organization chart with siblings ordered from left to right.
- A teal background, dark blue root, and cream first-level topics.
- Compact white text for deeper topics and pale rounded connector lines.
- Centered titles, with the place ID, login, place creation timestamp, and place type retained.

The theme is stored once per sheet rather than repeated on every topic.
It uses the JSON theme structure shown in the
[official XMind viewer example](https://github.com/xmindltd/xmind-viewer/blob/master/example/content.json).
Exact rendering depends on the installed XMind version and available fonts;
visual matching in the desktop application has not been verified.
Re-export to a new filename to apply the appearance to existing data.
Previously exported files are not modified automatically.

## Progress for large exports

Every two seconds, the console reports the current stage, processed count,
throughput (places per second), and total elapsed time. Stage starts and
completions are reported immediately. Updates continue while waiting for the
database or disk, although the count may remain unchanged. Progress also works
when output is redirected to a file.

During reading, progress shows the number of places received without a
percentage: no separate `COUNT` query is executed. After reading, indexing,
relationship building, sorting, tree validation, and XMind writing report
counts and percentages based on the known total. Success is announced only
after the archive has been closed and saved.

JSON is flushed to the archive in chunks of approximately 64 KiB, without
buffering the entire document. Records and the tree still reside in memory,
so memory usage grows with the place count and the length of login values.
A load test exports 1,000,001 synthetic places. It does not measure real
database performance or verify that XMind can open a map of that size.

## Validation and limitations

The exporter executes one parameterized `SELECT` inside a `READ ONLY`
transaction. It does not modify the database. An empty structure, unknown
place type, negative position, duplicate ID or sibling position, cycle, or
parent outside the selected structure causes an error. A missing linked
partner or null login also causes an error instead of silently dropping a place. Affected places are
not silently omitted or moved to the root.

The former 1,000-level tree depth limit has been removed. Validation and writing
use iterative traversal, and the JSON writer allows nesting up to its integer
limit instead of imposing a small fixed depth. Trees are exported intact, without
splitting chains or introducing artificial roots. Tests validate chains of 1,001,
10,000, and 100,000 levels by reading the resulting JSON tokens.
Actual XMind support for extreme nesting has not been verified; a successful
export does not guarantee that XMind can open or edit such a deep map.
All selected places are loaded into memory.
Large maps are also constrained by available memory and XMind performance.
The archive is first written to a temporary file beside the destination and
moved to the final path after successful completion. On a normal error or
cancellation, the temporary file is deleted.

Exit codes: `0` for success/help, `2` for argument errors, `1` for data,
connection, or file errors, and `130` for cancellation.

```bash
dotnet test tests/LegacyPlacesXmindExporter.Tests/LegacyPlacesXmindExporter.Tests.csproj
```

Tests cover archive structure, titles, ordering, multiple roots, invalid
trees, depth, cancellation, existing-file protection, structure and filename prompts, and
a million-place export with progress reporting. They do not replace testing
against the real legacy database or opening the file in the installed XMind
version.

## Connection troubleshooting

A failure during the connection stage occurs before the output file is created.
Network diagnostics distinguish DNS failures, refused connections, unreachable
hosts, and timeouts. TLS errors indicate certificate or encryption negotiation
problems. Errors returned by PostgreSQL include their SQLSTATE code. Diagnostics
omit raw driver messages and connection strings to avoid exposing credentials.

Check `Host` and `Port`, server availability, and any required VPN or SSH tunnel.
For TLS errors, verify `SSL Mode` and certificate configuration. For server
errors, check the SQLSTATE and server logs. An existing
`LEGACY_PLACES_CONNECTION_STRING` environment variable overrides the `.env` file;
updating only the file will not replace that environment variable.
