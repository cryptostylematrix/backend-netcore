# ProgramMigrator

Imports a legacy Multi or Neo program into the ReferralProgram schema in two
independent steps.

- Structure `0` comes from the root Profile program and its Invite-contract tree.
- Multi structures `1+` come from `multi_places`; locks come from `multi_locks2`.
- Neo structures `1+` come from `marketing_places`; locks come from `marketing_locks`.
- The legacy Matrix/Marketing tables are read through the Matrix database connection.
- Scope `invite` imports only structure `0`.
- Scope `structures` imports only structures `1+` and their locks.
- Each applied scope is written in its own PostgreSQL transaction.
- Each imported structure gets its persisted `matrix_filling` recalculated from
  its parent hierarchy and configured height.

The migrator is a dry run unless `--apply` is supplied. During an applied migration,
every destination structure in the selected scope must contain only its initial
top place. Imported structures must exactly match the configured structures in
that scope.

Apply database script `020_add_matrix_filling_to_places.sql` before running a
current migrator build.

## Local configuration

The application automatically loads `.env` from the `ProgramMigrator` directory.
Copy `.env.example` when setting up a new checkout, fill both database connection
strings and the program values. Select the scope explicitly when running from
this directory.

```bash
dotnet run --no-restore -- --scope invite
dotnet run --no-restore -- --scope structures
```

To write the imported data:

```bash
dotnet run --no-restore -- --scope invite --apply
dotnet run --no-restore -- --scope structures --apply
```

Real `.env` files are ignored by Git; `.env.example` is committed as the template.

Separate local files are available for Multi and Neo. Select one explicitly:

```bash
dotnet run --no-restore -- --env-file .env.multi --scope invite
dotnet run --no-restore -- --env-file .env.multi --scope structures
dotnet run --no-restore -- --env-file .env.neo --scope invite
dotnet run --no-restore -- --env-file .env.neo --scope structures
```

Add `--apply` after the selected file to write the migration:

```bash
dotnet run --no-restore -- --env-file .env.multi --scope invite --apply
dotnet run --no-restore -- --env-file .env.multi --scope structures --apply
```

## Multi dry runs

Import structure `0` from the contracts API:

```bash
dotnet run --project src/ProgramMigrator -- \
  --marketing-addr "DESTINATION_MARKETING_ADDRESS" \
  --program-id "0x1ce8c484" \
  --scope invite \
  --root-profile-addr "ROOT_PROFILE_ADDRESS"
```

Import structures `1+` and locks from the legacy database:

```bash
dotnet run --project src/ProgramMigrator -- \
  --marketing-addr "DESTINATION_MARKETING_ADDRESS" \
  --program-id "0x1ce8c484" \
  --scope structures \
  --source-connection-string "Host=127.0.0.1;Database=cs_matrix;Username=USER;Password=PASSWORD"
```

## Neo dry runs

`--source-marketing-addr` selects the legacy Neo rows. It defaults to the
destination marketing address.

Import structure `0` from the contracts API:

```bash
dotnet run --project src/ProgramMigrator -- \
  --marketing-addr "DESTINATION_MARKETING_ADDRESS" \
  --program-id "0x435acabf" \
  --scope invite \
  --root-profile-login "ROOT_PROFILE_LOGIN"
```

Import structures `1+` and locks from the legacy database:

```bash
dotnet run --project src/ProgramMigrator -- \
  --marketing-addr "DESTINATION_MARKETING_ADDRESS" \
  --source-marketing-addr "LEGACY_NEO_MARKETING_ADDRESS" \
  --program-id "0x435acabf" \
  --scope structures \
  --source-connection-string "Host=127.0.0.1;Database=cs_matrix;Username=USER;Password=PASSWORD"
```

To write the result, also provide the Programs connection and `--apply`:

```bash
dotnet run --project src/ProgramMigrator -- \
  ... \
  --connection-string "Host=127.0.0.1;Database=cs_programs;Username=USER;Password=PASSWORD" \
  --apply
```

The `invite` scope does not require the source Matrix connection. The `structures`
scope does not require a root profile or access to the contracts API. Connection
strings can instead be supplied with `ConnectionStrings__Matrix` and
`ConnectionStrings__Programs`.

## Legacy fields

The migration reconstructs `mp`, `deep`, `filling`, and parent profile fields from
the legacy parent relationships. Multi positions and lock positions are converted
from `0/1` to `1/2`, and Multi `clone` is mapped to `kind`.

Legacy contract addresses are used only to resolve parent places and locks. Old
`parent_addr`, `width`, `height`, `seq_no`, `filling2`, `inviter_profile_addr`,
`confirmed`, task metadata, and legacy place volumes are not copied. Profile
volumes can be rebuilt after import with `ProgramVolumeRecalculator`.

Exact duplicate legacy locks are treated idempotently: the first row is imported
and subsequent rows with the same structure, place address, locking profile, and
locked position are skipped.
