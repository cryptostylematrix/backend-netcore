# ProgramInviterChanger

Administrative console tool for moving a structure `0` referral subtree to a new
inviter in one PostgreSQL transaction.

The tool resolves both logins through the contracts API and asks for explicit
confirmation. It updates:

- the referral's parent and position;
- `mp` and `deep` for the referral and every descendant;
- `mp` for locks targeting places in the moved subtree;
- `filling` for the old and new inviter places.

It rejects moving the program root, moving a referral below its own descendant,
invalid MP data, and occupied destination MP ranges. If the selected inviter is
already the current inviter, it exits without changing the database.

Copy `.env.example` to `.env` and fill the contracts API URL and Programs database
connection. Real `.env` files are ignored by Git and configuration values are not
printed by the application.

Run from this directory:

```bash
dotnet run --no-restore
```

The application asks for the marketing address, referral login, new inviter login,
and final confirmation. Database permissions require `SELECT` and `UPDATE` on
`public.places`, plus `SELECT` and `UPDATE` on `public.locks`.

Run `grant_permissions.sql` as `postgres` or the table owner after setting its
single `v_username` variable to the username from the connection string. The script
grants and verifies all four required permissions.
