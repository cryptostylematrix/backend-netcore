# ProgramPlaceParentRepairer

Checks and repairs denormalized physical-parent fields in program structures:
`parent_profile_addr`, `parent_profile_login`, and `parent_place_number`.
Use this tool to repair data affected by older versions of ProgramPlaceOwnerChanger.
The authoritative relationship is `places.parent_id` within the same marketing
program and structure. No Contracts API connection or login lookup is needed.

Copy `.env.example` to `.env` in this directory and configure
`PROGRAM_PLACE_PARENT_REPAIRER_CONNECTION_STRING` for the Programs database.
From the backend repository root, run a read-only check:

```sh
dotnet run --project src/ProgramPlaceParentRepairer
```

Both prompts are optional and filter independently:

- Marketing contract address: leave empty to select every program registered in
  `public.referal_program`, including disabled programs.
- Structure number (0–255): leave empty to select all structures stored in
  `public.structures` for the selected programs, including referral structure 0.

For example, an empty marketing address with structure `0` checks the referral
structures of all registered programs. An address with an empty structure checks
all structures of that program. Leaving both empty checks all registered programs
and all their database structures. Programs without matching structures contribute
no work; if nothing matches, the tool reports this and exits without writing.
Places belonging to unregistered programs or structures are not selected.

The tool checks all places in scope, including children owned by other profiles;
filtering by the transferred profile alone would miss those children. Structure 0
uses the same physical-parent rules: parent metadata is restored from `parent_id`
without changing the referral topology.

Progress is printed for each structure during checking, rechecking, repair, and
verification. It shows the current program and structure, completed/total structure
counts, and cumulative checked-place and mismatch counts. Counts describe completed
queries rather than an estimated time to completion. Empty structures are included.

The report shows the total checked, mismatched, and invalid-parent counts, plus
current and expected values for the first 50 mismatches. Every mismatch is
included in the repair, even when the report displays only a sample.

To repair, stop the task processor and other Programs database writers, then run:

```sh
dotnet run --project src/ProgramPlaceParentRepairer -- --apply
```

Review the new report and enter `REPAIR`. The tool locks `referal_program`, `structures`, and `places`,
rechecks the inspected data, and requires a new check if the selected structures, mismatch list, or
checked count changed. Updates and verification run in one transaction; any
failure before commit rolls back the repair. A second check should report zero
mismatches. Running the repair again is safe and makes no changes when data is
already consistent.

For each place, expected fields come from the physical parent's current
`profile_addr`, `profile_login`, and `place_number`. A root (`parent_id IS NULL`)
must have all three parent fields null. A child of a system place has null parent
profile fields but retains the system parent's place number.

Missing parents, references to a different program or structure, and direct
self-references are reported and block the entire repair. These cases require
manual investigation of `parent_id`; the tool never guesses a new parent. It does
not audit MP topology or detect longer parent cycles. It never changes `parent_id`,
place ownership or numbering, `mp`, depth, filling, locks, volumes, or contract state.

Required permissions: SELECT on `referal_program` and `structures`, and
SELECT/UPDATE on `places`.
Keep real credentials in a local `.env` file or environment variables.

Regression tests are included in
`tests/ProgramPlaceOwnerChanger.Tests`. Set `OWNER_CHANGER_TEST_POSTGRES` only to
an explicitly disposable local PostgreSQL server with CREATE DATABASE permission:

```sh
dotnet test tests/ProgramPlaceOwnerChanger.Tests/ProgramPlaceOwnerChanger.Tests.csproj
```

Database tests create a fresh database per test and drop it afterward. They cover
legacy-transfer repair, child metadata, roots, system parents, scope isolation,
idempotence, stale previews, all-program/structure-0 selection, progress reporting,
and refusal to write when physical parents are invalid.
Without the environment variable, PostgreSQL tests are skipped.
