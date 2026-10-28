# ProgramPlaceOwnerChanger

Interactive administrative tool for transferring places between profiles within
one program. Uses the Programs PostgreSQL database and the Contracts API.

Copy `.env.example` to `.env` in the tool directory and configure the database
connection and API URL. Run from the backend repository root:

```sh
dotnet run --project src/ProgramPlaceOwnerChanger
```

Enter the marketing contract address, structure number (1–255; leave empty for
all structures > 0), old login, and new login. Both profile addresses are resolved
through the Contracts API. The new profile must have its first place in this
program's referral structure 0. Structure 0 supplies referral relationships for
volume calculations; transferring its places is not supported.

The preview does not write to the database. To apply the transfer, stop the task
processor and other Programs database writers, review the displayed parameters,
and enter `CHANGE`. Checks are repeated under table locks before writing. If the
displayed counts or referral parent have changed, run the tool again. Places,
locks, and volumes are committed in one transaction. An error, uniqueness
conflict, or cancellation before commit rolls back the entire operation.

The tool updates:

- `profile_addr`, `profile_login`, and the search `index` of transferred places;
- `parent_profile_addr`, `parent_profile_login`, and `parent_place_number` on
  transferred places and their immediate children, using each physical parent's
  current data via `parent_id` after ownership and numbering have changed;
- `locks.profile_addr` (lock owner), `place_profile_addr`, and
  `place_profile_login` (owner of the referenced place) when they reference the
  old profile;
- personal and referral volumes for all profiles in the selected structures,
  using activated places (`activated_at IS NOT NULL`) and current structure-0
  referral relationships.

Transferred places are renumbered in old-number order after the new owner's
highest existing place number, separately in each structure (starting at 1 when
there are no existing places). Referenced place numbers in locks are updated
using the transfer mapping. Physical parent fields are synchronized using
`parent_id`. Missing, self-referencing, or out-of-scope physical parents in affected
rows block the transfer. Place-number overflow
or a lock uniqueness conflict rolls back the entire transaction.

Physical placement (`parent_id`, `mp`, `deep`, `pos`), matrix filling, and the
referral structure remain unchanged. Parent fields refer to physical placement,
not the new owner's inviter. Root parent fields are null; a system parent's
profile fields are null while its place number is preserved. Group volume is
preserved because its calculation is not yet implemented in the backend. The tool does not modify TON
contracts, marketing tasks, or UI profiles.

Required permissions: SELECT on `structures`; SELECT/UPDATE on `places` and
`locks`; SELECT/INSERT/UPDATE on `profile_volumes`. The database schema must
include migration 026. Store real connection settings and secrets only in a local
`.env` file or environment variables.

Run the tests:

```sh
dotnet test tests/ProgramPlaceOwnerChanger.Tests/ProgramPlaceOwnerChanger.Tests.csproj
```

Without `OWNER_CHANGER_TEST_POSTGRES`, input-validation tests run and
PostgreSQL tests are skipped. To run the full suite, set this variable to a
connection string for a **disposable local** PostgreSQL server with CREATE
DATABASE permission. Each test creates a separate temporary database and drops
it afterward. Tests cover transfer and renumbering, locks, parent place numbers,
volumes, structure selection, and full transaction rollback on a lock conflict.
These tests do not call the real Contracts API or TON.

To check and repair parent fields already affected by the old transfer behavior,
use [ProgramPlaceParentRepairer](../ProgramPlaceParentRepairer/README.md). It checks
all places in the selected paid structures, including children of transferred places.
