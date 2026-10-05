# Activity PostgreSQL checks

Run from the backend repository with Docker Desktop running and .NET 10 installed:

```bash
bash tests/Modules/ReferalProgram.Application.Tests/Postgres/run.sh
```

The runner starts a disposable PostgreSQL 17 container on a random localhost-only
port and stops/removes it on exit. No application connection strings or `.env`
files are loaded. The password in the runner is a public disposable test credential,
not a deployment credential. Each test creates a randomly named database and drops
only that database afterward. No existing tables or databases are cleared.
The downloaded Docker image remains cached.

Without `ACTIVITY_TEST_POSTGRES_PORT`, these six tests are explicitly skipped in
ordinary unit-test runs. Set this variable only to the port of this disposable
container: the fixture assumes database `activity_test`, user `postgres`, and the
runner's test password. Supplying it opts into database creation/deletion.

## Coverage

- Four legacy activity forms (SQL NULL, empty object, immediate activation true
  and false), active/inactive inviters, with/without marketing places: 16 cases.
  Check command results, persisted child state, parent filling and task receipts.
- Profile-root fallback skips inactive inviters by default. Own first places and
  owner-root selection remain independent of activity.
- Real SQL place-presence checks ignore invites and other programs, and count
  inactive clones.
- CryptoCash structures 1–4 use activity JSON read from its actual setup script.
  Activation persists dates, flags, personal/referral volume and task receipts;
  repeat activation is rejected; successive period resets do not add volume.
- Enabled invitation flags distinguish the presence of places in the same program.
- Enabled fallback affects profile-root selection but not owner-root selection.

The fixture uses production Dapper queries, EF mappings/repositories, MediatR domain
event dispatch and transaction saving. Only on-chain command availability is a
stub. Period reset exercises aggregate reset plus real persistence, without the
MassTransit transport.

## Baseline comparison performed

The same four legacy tests were run against an isolated archive of commit
`d6433d4` (before stage 2) and the current working tree. Both passed identical
assertions against fresh seeded databases. The two `New_` tests run only against
the current implementation. This is a behavioral regression comparison, not a
byte-for-byte database dump comparison.

## Limits

Tables managed by EF are generated using `EnsureCreated`; the query-only
`structures` table has a minimal explicit fixture schema. This does not validate
SQL migrations, production constraints/triggers/permissions or production data.
No real wallets, TON calls, API host, scheduler or background workers are started.
Purchase/clone/reinvest handlers and complete chess/radar placement flows are not
covered by these PostgreSQL tests. Existing unit tests remain necessary.
