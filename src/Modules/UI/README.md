# UI module

The UI module persists wallet-specific frontend state: saved profile intents,
TON Connect metadata and the last connection time, and the selected interface
language. It also caches profile content and records profile-intent history.
The database is the source of truth for saved profiles and connected-wallet
language preferences; browser storage is retained only for migration, guest
language, pending language saves, and the current profile selection.

The stored relationship is an **intent**, not proof that a wallet owns a
profile. On-chain ownership is checked separately and stored in the `owned`
field as the latest known state.

## Projects

- `UI.Core` contains profile, wallet-profile intent, TonConnection, and wallet-preference domain models.
- `UI.Application` contains commands, queries, contract synchronization, and
  domain-event handlers.
- `UI.Dto` contains API response models, modes, and stable error codes.
- `UI.Infrastructure` contains PostgreSQL persistence, queries, repositories,
  address normalization, and dependency registration.
- `UI.Presentation` contains the FastEndpoints API endpoints.

## Data model

| Table | Identity | Stored data | Update policy |
| --- | --- | --- | --- |
| `profiles` | Profile NFT address | Login and cached content | On add/check when contract content changes |
| `wallet_profile_intents` | Wallet + profile address | Display mode and last checked ownership | Add/remove and ownership checks |
| `wallet_profile_intent_events` | Event ID | Profile relationship history | Append-only domain events |
| `ton_connections` | Wallet address | Contract/app versions, app name, platform, connection timestamps | Latest accepted connection snapshot |
| `wallet_preferences` | Wallet address | Selected language and change time | Initialize if absent; replace only on explicit selection |

Wallet addresses use the same canonical non-bounceable, non-test-only, URL-safe
representation across all wallet-keyed tables. TonConnection and preferences do
not require a saved profile and have no foreign key to each other. Their records
survive disconnecting a wallet or removing its profile intents.

```text
profiles
   1
   |
   | profile_addr
   *
wallet_profile_intents        wallet_profile_intent_events
(current frontend intent)     (append-only history)
```

### `profiles`

Caches data read from the Profile NFT contract:

- `address`: canonical Profile NFT address and primary key.
- `login`: normalized lowercase login; unique.
- `content`: profile content stored as `jsonb`.
- `updated_at`: time the cached data last changed.

The cache is refreshed when a profile is added and when a wallet's profiles
are checked. A content or login change raises an in-process
`ProfileContentChangedDomainEvent`.

### `wallet_profile_intents`

Stores the current profile-display intention of a wallet:

- `wallet_addr`: normalized TON wallet address in user-friendly,
  non-bounceable, URL-safe format.
- `profile_addr`: cached Profile NFT address.
- `mode`: `owner` or `preview`.
- `owned`: whether the wallet owned the profile during the latest successful
  contract check.
- `created_at` and `updated_at`: UTC timestamps.

The `(wallet_addr, profile_addr)` combination is unique.

Incoming wallet addresses may be raw or user-friendly, bounceable or
non-bounceable, and URL-safe or standard Base64. Every accepted representation
is converted to the user-friendly, non-bounceable, non-test-only, URL-safe form
before persistence. As a result, different textual representations of the same
TON account resolve to one wallet intent list.

Because the wallet address is a route segment, non-URL-safe Base64 input must
be URL-encoded by the client before it is sent.

`mode` and `owned` deliberately mean different things:

- `mode` records how the wallet intended to use the profile in the UI.
- `owned` records the last verified on-chain ownership state.

Losing ownership sets `owned` to `false`; it does not remove the relationship
or silently change `mode` to `preview`. This preserves the original intent.

### `wallet_profile_intent_events`

Stores append-only relationship history:

- `added`: a new display intent was created.
- `removed`: the wallet removed an existing display intent.
- `ownership_lost`: a successful check observed an `owned: true` to
  `owned: false` transition.
- `ownership_gained`: a successful check observed an `owned: false` to
  `owned: true` transition. This includes both newly acquired and restored
  ownership; it is not recorded as another `added` event.

Each event includes wallet and profile addresses, UTC occurrence time, and a
`jsonb` data object containing relevant mode and ownership values. Events are
inserted by domain-event handlers in the same `SaveChanges` operation as the
current relationship change.

An `added` event is emitted only when the display intent is first created.
Later ownership changes never emit another `added` event.

## TON connection metadata

`ton_connections` stores one latest snapshot per canonical wallet address,
independently of profiles. It is not a connection history or an authenticated session.

- `wallet_addr`: primary key, normalized using the same rules as profile intents.
- `contract_version`: recognized standard code revision (`v1r1` through `v5r1`,
  including `v4r1` and `v4r2`); unrecognized code or absent StateInit is
  `unknown version`.
- `wallet_name`: TON Connect wallet display name, falling back to `device.appName`.
- `app_version`: `device.appVersion`, separate from the contract version.
- `platform`: `device.platform` reported by the wallet, not the browser user agent.
- `created_at`, `updated_at`: UTC timestamps; `updated_at` changes only when metadata changes.
- `last_connected_at`: server UTC time of the latest accepted connection snapshot,
  including restored sessions after a page reload. Also refreshed on metadata-change
  notifications; ordinary React renders do not send snapshots. Existing rows remain
  NULL until the first snapshot after migration 004 (no historical time is guessed).
  This is receipt time, not the original time at which the wallet session was established.

```http
PUT /api/ui/wallets/{wallet_addr}/ton-connection
Content-Type: application/json

{
  "wallet_state_init": "<base64 StateInit BOC from account.walletStateInit>",
  "wallet_name": "Tonkeeper",
  "app_version": "5.0.0",
  "platform": "android"
}
```

Returns `{ "success": true, "errors": [] }`; validation failures return
`success: false` with `err_invalid_wallet_address`, `err_invalid_ton_connection`,
or `err_invalid_wallet_state_init`. Names/version/platform are required, trimmed,
control-character-free strings limited to 128/64/32 characters respectively.
StateInit is optional and limited to 65,536 Base64 characters. If supplied, it
must be valid and hash to the wallet address. The server identifies the code
using the [standard wallet hashes](https://docs.ton.org/contracts/standard/wallets/history).
The StateInit itself is not persisted. This identifies initial code supplied by
TON Connect, not a fresh read of current on-chain code after a contract upgrade.

The repository uses a single atomic PostgreSQL upsert; duplicate connections
cannot create duplicate rows. Identical metadata preserves `updated_at` but refreshes
`last_connected_at`; concurrent requests cannot move that timestamp backwards. The latest
processed changed snapshot replaces all four metadata fields while preserving
`created_at`. Different apps/devices for one address share that same record.
As with profile intents, this anonymous endpoint stores client-reported metadata;
it is not proof of wallet ownership.

The frontend syncs both existing/restored sessions and connection status events,
without waiting for a selected profile. Saves are serialized and temporary
failures are retried up to three attempts; failures do not interrupt wallet use.
A page reload or a later status event can retry a failed snapshot.

Deployment: apply `Database/Scripts/003_create_ton_connections.sql` to the
configured UI database (or Programs fallback), setting its API role variable,
apply `Database/Scripts/004_add_last_connected_at.sql`, then deploy the API and frontend. The API does not apply this migration itself.
New installations need the numbered UI schema scripts in order.

## Wallet language preferences

`wallet_preferences` stores one preference per canonical wallet address,
independently of profiles and TonConnection telemetry. Fields are `wallet_addr`
(primary key), `language`, and `updated_at` (UTC, changed only when language changes).
The database and backend validate language-tag syntax, not the frontend catalog.
Tags are lowercased, at most 63 characters, with a 2–8-letter primary subtag and
optional hyphen-separated 1–8-character alphanumeric subtags. Examples include
`ja`, `pt-br`, `zh-hant`, and `sr-latn-rs`. This is a bounded syntax check, not
validation against an external language registry.

- `POST /api/ui/wallets/{wallet_addr}/language/resolve` with
  `{ "language": "ru" }` loads the database value or atomically initializes an
  absent preference from the supplied fallback. It never replaces an existing
  preference, including during concurrent initialization from other devices.
- `PUT /api/ui/wallets/{wallet_addr}/language` with the same body saves an
  explicit user choice, creating the row if needed.

Both return `{ "success": true, "language": "ru", "errors": [] }`.
Invalid addresses/languages return `success: false` with
`err_invalid_wallet_address` or `err_invalid_language`. Input is trimmed and
lowercased; the API retains regional/script subtags and accepts future language tags.
These endpoints follow the module's existing anonymous, client-reported wallet
identity semantics. Preferences are not authentication or proof of ownership.

Frontend migration preserves the old detector's precedence: `i18nextLng` in
localStorage, then the `i18next` cookie, then a supported browser language.
The legacy value is only an initialization fallback: saved database values win.
After a successful resolve/save for the active wallet, legacy storage is removed.
Failures retain migration data. Language changes are explicitly saved rather than
subscribing to every i18next change, so loading a database value cannot trigger
an accidental write back.

Connected wallets reload their language on connection/session restoration or
wallet switches. Requests are serialized and late responses from previous wallets
or superseded selections cannot change the active language. Failed explicit
selections are retained in a per-wallet pending outbox in localStorage until
acknowledged; this is retry data, not the source of saved preferences. Requests
have a timeout and up to three attempts; reconnecting, reloading or going online
retries pending changes. Without a wallet, guest selections remain browser-local
until migration on connection.

Apply `Database/Scripts/005_create_wallet_preferences.sql` and then
`Database/Scripts/006_allow_extensible_language_tags.sql` to the configured UI
database (skip already applied scripts), then deploy the backend and frontend. There is no
automatic production migration. The [Docker regression runner](#ui-persistence-postgresql-regression-test) also applies
005/006 and checks initialization races, database precedence, explicit updates,
per-wallet isolation and unchanged timestamps for identical selections.

## Contract lookup

Profile resolution uses the Contracts module through MediatR request/response
queries:

1. Resolve the Profile NFT address from the normalized login.
2. Read the Profile NFT data.
3. Cache its login and content.
4. Compare its owner address with the normalized wallet address.

The UI module does not directly call Contracts infrastructure.

## Profile API

Business failures for add, remove, and check are returned in the response body
using `success: false` and stable error codes. This lets the frontend map each
code to localized text without parsing server messages.

### Add a profile intent

```http
POST /api/ui/wallets/{wallet_addr}/profiles
Content-Type: application/json

{
  "login": "alice",
  "mode": "owner"
}
```

Valid modes are `owner` and `preview`.

Example success:

```json
{
  "success": true,
  "errors": [],
  "available_modes": ["owner", "preview"]
}
```

Behavior:

- The login is resolved through the Profile contracts before anything is
  added.
- `owner` succeeds only when the wallet currently owns the profile.
- `preview` succeeds for any wallet when the profile is valid.
- Requesting `owner` without ownership returns
  `err_contract_doesnot_belong_to_the_wallet` and offers `preview` in
  `available_modes`.
- Profile lookup failures return an empty `available_modes` array.
- Adding an existing wallet-profile relationship is idempotent: it does not
  create another `added` event.
- An existing intent's mode is not changed by another add request. To replace
  the intent mode, remove the relationship and add it again.
- Even for an existing relationship, cached content and current ownership are
  refreshed. A detected ownership loss is recorded.

### Remove a profile intent

```http
DELETE /api/ui/wallets/{wallet_addr}/profiles/{login}
```

Example success:

```json
{
  "success": true,
  "errors": [],
  "available_modes": []
}
```

Removal does not call the blockchain. This ensures a wallet can remove its UI
intent even when the contracts provider is unavailable. A successful removal
adds a `removed` history event. Removing a relationship that does not exist
returns `err_profile_relationship_not_found`.

### Check all profiles for a wallet

```http
POST /api/ui/wallets/{wallet_addr}/profiles/check
```

The profiles are checked sequentially. For each relationship, the operation:

1. Reads the current profile contract data by its cached login.
2. Refreshes cached content when it changed.
3. Refreshes the `owned` value.
4. Records `ownership_lost` for a verified `true` to `false` transition.

Example response:

```json
{
  "success": true,
  "errors": [],
  "profiles": [
    {
      "wallet_addr": "EQ...",
      "profile_addr": "EQ...",
      "login": "alice",
      "mode": "owner",
      "owned": true,
      "content": {
        "login": "alice",
        "image_url": "https://example.com/image.png"
      }
    }
  ]
}
```

Checks are partially tolerant. Successfully read profiles are updated even if
another profile fails. In that case `success` is `false`, predefined errors
are returned, and the response still contains the current stored profile list.

### List a wallet's profiles

```http
GET /api/ui/wallets/{wallet_addr}/profiles
```

Returns the current relationships joined with cached profile content. It does
not invoke the blockchain. Use the check endpoint when fresh ownership and
content information is required.

## Error codes

| Code | Meaning |
| --- | --- |
| `err_wallet_not_connected` | Wallet address was not provided. |
| `err_invalid_wallet_address` | Wallet address is not a valid TON address. |
| `err_invalid_ton_connection` | Wallet name, app version, platform, or StateInit size is invalid. |
| `err_invalid_wallet_state_init` | Supplied StateInit is malformed or does not match the address. |
| `err_invalid_language` | The language tag is empty, malformed, or longer than 63 characters. |
| `err_invalid_login` | Login is empty. |
| `err_invalid_profile_mode` | Add mode is missing or invalid. |
| `err_profile_not_found` | A valid deployed profile could not be found. |
| `err_contract_request_failed` | Contract data could not be read. |
| `err_contract_doesnot_belong_to_the_wallet` | Owner mode was requested by a non-owner wallet. |
| `err_profile_relationship_not_found` | The requested current intent does not exist. |

These constants are defined in `UI.Dto/UiErrorCodes.cs`. Profile flows map
business errors to localized messages; background connection/language sync logs
failures and retries without interrupting wallet use.

## Database setup

For production migration through pgAdmin, use the
[pgAdmin scripts and walkthrough](Database/PgAdmin/README.md).

Apply schema scripts in order, only when not already present in the target database:

| Script | Change |
| --- | --- |
| [001](Database/Scripts/001_create_ui_profile_intents.sql) | Profile cache, intents, and event history |
| [002](Database/Scripts/002_add_ownership_gained_event.sql) | Allow ownership-gained history events |
| [003](Database/Scripts/003_create_ton_connections.sql) | Connection metadata and initial timestamps |
| [004](Database/Scripts/004_add_last_connected_at.sql) | Nullable last connection timestamp; no invented historical backfill |
| [005](Database/Scripts/005_create_wallet_preferences.sql) | Per-wallet language preferences |
| [006](Database/Scripts/006_allow_extensible_language_tags.sql) | Remove the fixed language list and allow tags up to 63 characters |

Set `v_database_username` to the API role in scripts 001, 003, and 005.
For an existing database with 001–004, apply 005 and 006 for the language change; if 005 is already applied, apply only 006.
For a fresh installation, apply all six. These scripts are not an automatic
migration runner and must not be blindly replayed. Deploy schema changes first,
then the backend, then the frontend.

The pgAdmin copy guide predates the connection/preference tables; its original
copy/grant/verification scripts cover profile data only. See its scope note
before relocating a database that already contains tables from 003–005.

The preferred configuration is a dedicated connection:

```dotenv
ConnectionStrings__UI=Host=127.0.0.1;Port=5432;Database=DATABASE_NAME;Username=DATABASE_USER;Password=DATABASE_PASSWORD
```

If `ConnectionStrings__UI` is empty or absent, the module uses
`ConnectionStrings__Programs`. Run the schema script in whichever database is
selected.

## Frontend synchronization and migration

| Event | Profile state | TonConnection | Language |
| --- | --- | --- | --- |
| Wallet connection or session restoration | Migrate legacy profiles, then check server profiles | Save snapshot and refresh last connection time | Resolve saved preference; initialize only if absent |
| Switch to another wallet | Load/check that wallet's profiles | Save the new wallet's snapshot | Load the new wallet's language |
| Explicit language selection | No change | No change | Save via PUT |
| TonConnect metadata notification | No profile request solely for metadata | Save changed snapshot | No language request solely for metadata |
| Ordinary React render | No request solely for rendering | No request solely for rendering | No request solely for rendering |
| Wallet disconnect | Clear active frontend profile state | Keep database record | Use guest language; keep database preference |

Profile migration reads legacy cookie/localStorage entries and submits each
through the add endpoint. Rejected owner intents may be retried as preview when
that mode is offered. Successfully migrated entries are removed locally; failed
entries remain for retry. The frontend then calls `profiles/check` even if the
browser has no profiles, using its returned list without an extra list request.
Late responses for an old wallet do not replace the active wallet's profile list.
The selected profile login remains a browser-local preference.

For language migration, database precedence, pending saves, and guest behavior,
see [Wallet language preferences](#wallet-language-preferences). Profile,
connection, and language synchronization are independent: selecting a profile
is not required to save connection information or load the wallet's language.

## Authentication limitation

The endpoints currently follow the rest of the API and are anonymous. A caller
can therefore submit another wallet's address. The data represents a claimed
wallet intention until wallet-signature authentication is added. Do not use it
as cryptographic evidence of wallet behavior or ownership.

## Generated profile avatars

`GET /avatar?login=alice` (also `/api/ui/avatar?login=alice`) returns a 512×512
SVG with a dimensional solid-color CS monogram, a CRYPTO STYLE ribbon,
subtle orbital decoration, and centered login text below it. The accent palette
and orbit angle are derived deterministically from the login hash.
The seed is SHA-256 of the trimmed, lowercase UTF-8 login, interpreted as the
Profile Collection's unsigned big-endian NFT index. The login identifies the
avatar; the accent color alone is not guaranteed unique. No database or
TON request is required, so avatars also work before profile deployment.

The avatar endpoint accepts 4–20 ASCII letters/digits/hyphens, with a letter
or digit at each end, matching profile creation and the documented 20-character
profile login maximum.
Long avatar labels shrink to fit within the frame. Invalid input returns HTTP 400. Successful images use
`image/svg+xml` and one-day public caching. The frontend uses this URL when an
image is omitted during creation/update and displays generated avatars for
cached profiles with missing images or the old shared default. Existing NFT
metadata is not rewritten automatically. Deploy the API before the frontend.

### Indexer SVG compatibility

All visible lettering is embedded vector geometry. The login uses open-licensed
serif outlines and the brand uses sans-serif outlines; indexers need no system
fonts. Letter depth uses offset copies rather than SVG filter primitives, which
can suppress shapes in image proxies. The generator has no runtime font or
native rendering dependency. See `UI.Application/Features/Avatars/Assets/README.md`
for source fonts, licenses, and regeneration instructions.

Existing NFT image URLs remain valid. External indexers may retain an older
rasterized preview after deployment; the API cannot purge their caches. Refresh
through the indexer when available, or update the NFT image URL with a new
version query parameter using the normal wallet-approved profile update.


## UI persistence PostgreSQL regression test

From the backend repository root:

```bash
bash tests/Modules/UI.Infrastructure.Tests/Postgres/run.sh --no-restore -m:1 /nodeReuse:false
```

The runner creates a disposable PostgreSQL 17 Docker container on a random
loopback port, applies migrations 003–006, and exercises the actual
`TonConnectionRepository` and `WalletPreferencesRepository` under a restricted
application role. It checks legacy
NULL timestamps, initial insertion, reconnection with unchanged metadata,
metadata changes, concurrent inserts, monotonic `last_connected_at`, per-wallet
language isolation, initialization races, and protection of existing language
preferences from stale migration data.
The container is removed on exit; application database configuration is not used.
The test is skipped in ordinary test runs without the runner's explicit port.


### Adding a frontend language

`frontend/src/languages.ts` is the shared catalog for the selector, i18next,
and browser-language resolution. Add the language code and native label there,
and supply `public/locales/<code>/translation.json`. Use lowercase hyphenated
codes and matching directory names. No backend code change or further database
migration is needed for tags matching the syntax above.

The API client accepts stored tags independently of the bundled translations.
An older frontend that does not support a saved tag resolves the closest available
parent language or displays English, without saving that fallback over the database
preference. Only an explicit selection replaces the preference. Regional and script
subtags are retained in persistence; i18next loads the catalog's exact code.
