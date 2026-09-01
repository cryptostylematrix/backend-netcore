# Program Volume Recalculator

Dry-run-first administrative application for rebuilding one profile-volume type
for one Referral Program structure. Personal volume counts activated profiled
places by their profile. Referral volume counts the same places by the current
direct inviter recorded in structure `0`. Group volume is reserved and reports
that recalculation is not implemented.

Run database script `026_create_profile_volumes.sql` first. Stop the API task
processor and other Programs-database writers before using `--apply`.

```bash
dotnet run --project src/ProgramVolumeRecalculator -- \
  --marketing-addr REPLACE_WITH_MARKETING_ADDRESS \
  --structure-number 1 \
  --type referral \
  --connection-string 'Host=localhost;Database=cs_programs;Username=program_user;Password=replace_me'
```

The command is a dry run by default. Add `--apply` to write and verify the
selected volume in one transaction.
