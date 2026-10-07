# Migrate UI with pgAdmin

These are plain SQL files for pgAdmin's Query Tool.

## Scope of these scripts

This procedure and scripts 03–05 were written for the original three profile
tables and their two identity sequences. They do not grant, verify, or remove
`ton_connections` (schema migrations 003/004) or `wallet_preferences` (005).
When either table exists in the source, include it in the backup and restore,
grant the runtime role SELECT/INSERT/UPDATE on it, and separately verify its
row count and contents before cutover. Do not treat script 04's five output rows
as verification of these additional tables. Script 05 leaves them in the source.
Do not recreate a table using a schema script if it was already restored.

For a new installation or an upgrade within the existing UI database, use the
[ordered schema migrations](../../README.md#database-setup), not this database-copy
procedure. The current data model and synchronization behavior are documented in
the [UI module README](../../README.md).

## Copy the original profile data

The source is `cs_programs`; the destination is `cs_ui` on the same PostgreSQL
server, with runtime login `cs_ui_app`. Use an administrator for creation,
backup, restore, grants and verification. Use `cs_ui_app` only for the runtime
connection check and API. If you choose different names, update the scripts,
database guards and connection setting consistently.

Run each SQL file in full in its own Query Tool session, in the database named
below. Enable Auto-commit for the creation steps. On a SQL error, execute
`ROLLBACK;` before retrying. Script 05 is optional and deliberately requires a
separate commit; do not run it as part of the initial copy.

1. Take your normal production backup. Stop all API replicas and other UI writers,
   drain active requests, and leave them stopped until cutover finishes.
2. Connect Query Tool to `postgres` as administrator. Run
   [01_create_role.sql](01_create_role.sql).
   Refresh Login/Group Roles, open `cs_ui_app` Properties, and set a new password
   under Definition. Keep it in your deployment secret store.
3. With Auto-commit enabled, run **only**
   [02_create_database.sql](02_create_database.sql). Refresh Databases.
   The administrator remains owner. Existing names deliberately fail; inspect them
   rather than overwriting an existing database or role.
4. Right-click `cs_programs` → Backup. Choose **Custom** format and a protected
   backup filename. Include **Pre-data, Data, Post-data**; leave Only data and
   Only schema off. Exclude Owner and Privileges. On Objects, select only these
   objects within public (do not select the entire database/schema):

   - `profiles`
   - `wallet_profile_intents`
   - `wallet_profile_intent_events`
   - `wallet_profile_intents_id_seq`
   - `wallet_profile_intent_events_id_seq`

5. Run Backup and check the process log for success. Inspect the generated command
   to confirm it has `--table`/`-t` filters for the selected objects rather than
   just a schema filter. Identity sequences owned by
   selected tables are included automatically; select them explicitly if shown.
   The archive must contain both sequence definitions and sequence values.
6. Right-click **cs_ui** → Restore and select the archive. Include all sections;
   exclude Owner and Privileges. Enable **Single transaction** and **Exit on error**.
   Leave Number of jobs blank or set it to 1; a single transaction cannot use
   parallel restore jobs.
   Leave Create database and Clean before restore off. Restore into the fresh,
   empty database and check for success. Do not run schema scripts 001/002 first.
7. Open Query Tool connected to **cs_ui**, run
   [03_grant_permissions.sql](03_grant_permissions.sql).
8. As administrator, run [04_verify_copy.sql](04_verify_copy.sql) in both
   **cs_programs** and **cs_ui**, with UI writers still stopped. Compare all five
   output rows: counts, content fingerprints and sequence states must match.
   Stop and investigate any difference. For large tables this check can take time.
   Save the results before reopening writes; later destination changes will
   naturally make the fingerprints and sequence values differ from the source.
9. Update every API replica's production secret, preserving existing host/TLS settings:

   ```dotenv
   ConnectionStrings__UI=Host=YOUR_HOST;Port=5432;Database=cs_ui;Username=cs_ui_app;Password=YOUR_NEW_PASSWORD
   ```

   Leave `ConnectionStrings__Programs` unchanged. Ensure the effective deployment
   environment uses the new UI value; empty UI configuration falls back to Programs.
10. Test a connection as `cs_ui_app`, restart the API, check a known wallet's list,
    and exercise add/check/remove with a controlled wallet. Confirm new history
    is written in `cs_ui` before reopening traffic.

Source tables remain intact. Before any destination writes, rollback means restoring
the old UI connection. After new writes, stop writers and reconcile the new UI data
before switching back. Delete old tables only as a separate cleanup after acceptance.
If restore fails, do not cut over; inspect its log and destination before retrying.

## Remove the old source objects after acceptance

Keep a verified backup and confirm every API replica now uses `cs_ui` for UI
data. Open a fresh Query Tool connected to **cs_programs** as administrator and
run [05_remove_source_ui_objects.sql](05_remove_source_ui_objects.sql) in full.
It checks the database name and
removes only the three UI tables, including their owned identity sequences,
indexes, constraints and table grants. External dependencies block deletion;
the script does not use `CASCADE` or remove any database or login role.

The transaction is deliberately left open. After a successful result, execute
**`COMMIT;`** to finalize or **`ROLLBACK;`** to undo, in the **same Query Tool
connection**. Do this promptly because locks remain until the transaction ends.
If any statement fails, execute `ROLLBACK;` and inspect the error before retrying.
Once committed, restoring the old tables requires the backup; changing the
connection string alone is no longer a rollback.

pgAdmin version labels may differ. Official references:
[Backup dialog](https://www.pgadmin.org/docs/pgadmin4/latest/backup_dialog.html),
[Restore dialog](https://www.pgadmin.org/docs/pgadmin4/latest/restore_dialog.html).
