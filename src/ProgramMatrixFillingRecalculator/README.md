# Program Matrix Filling Recalculator

This console application checks `places.filling` and `places.matrix_filling` for every place of
every Referral Program, or one selected program. It is intended for existing programs that were created or
migrated before the persisted matrix-filling counter was introduced.

`filling` must equal the number of direct children in the same program and
structure, excluding the place itself (zero for a leaf).

For a matrix structure (`width > 0` and `height > 0`), a place's filling is the
number of places in the matrix rooted at that place, including the place itself
and descendants no farther away than `height`. Every place in a non-matrix
structure has a filling of `1`.

Run database script
`src/Modules/ReferalProgram/Database/Scripts/020_add_matrix_filling_to_places.sql`
before using this tool.

## Configuration

Copy `.env.example` to `.env` in this directory and fill in the values. Real
`.env` files are ignored by Git. Command-line options override environment
variables. Leave `PROGRAM_MATRIX_FILLING_MARKETING_ADDR` unset to process all
Referral Programs, or set it to restrict the operation to one program.

## Run

Check the values without modifying PostgreSQL:

```bash
dotnet run --project src/ProgramMatrixFillingRecalculator
```

Checks use a read-only, repeatable-read transaction per program and print each
incorrect place ID with stored and expected values, plus totals per structure.
Exit codes: `0` means all checked values match, `3` means mismatches were found,
`1` means execution failed, `2` means invalid options, and `130` means cancelled.
The check requires SELECT access only and does not require stopping writers.
It assumes valid parent links and checks counters, not tree integrity.

Apply the matrix-filling recalculation (`filling` is never modified):

```bash
dotnet run --project src/ProgramMatrixFillingRecalculator -- --apply
```

When your current directory is `src/ProgramMatrixFillingRecalculator`, use
`dotnet run` for a dry run and `dotnet run -- --apply` to write the result.

You may also provide values directly:

```bash
dotnet run --project src/ProgramMatrixFillingRecalculator -- \
  --marketing-addr REPLACE_WITH_MARKETING_ADDRESS \
  --connection-string 'Host=localhost;Database=cs_programs;Username=program_user;Password=replace_me'
```

Each program is verified and committed in its own transaction. Apply operations
lock `public.places` while the current program is recalculated. Run the tool
during maintenance with the API task processor and all other Programs-database
writers stopped. The tool prints progress per program and structure and never
prints the connection string.
