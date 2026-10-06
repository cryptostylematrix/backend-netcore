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

Without `ACTIVITY_TEST_POSTGRES_PORT`, these PostgreSQL tests are explicitly skipped in
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

## Recorded stage-2 baseline comparison

The same four legacy tests were run against an isolated archive of commit
`d6433d4` (before stage 2) and the stage-2 working tree. Both passed identical
assertions against fresh seeded databases. The two `New_` tests run only against
that implementation. This is a historical behavioral regression comparison, not a
byte-for-byte database dump comparison.

## Limits

Tables managed by EF are generated using `EnsureCreated`; the query-only
`structures` table has a minimal explicit fixture schema. This does not validate
the full SQL migration chain, production constraints/triggers/permissions or
production data. Selected setup/configuration scripts are executed by the Mini
tests described below.
No real wallets, TON calls, API host, scheduler or background workers are started.
Stage 3 additionally exercises purchase/clone/reinvest handlers, all seven
position strategies through real SQL, manual command validation, and activity
combinations. It does not execute the API or TON task transport. Existing unit
tests remain necessary.

## Stage 3 placement comparison

`Legacy_placement_snapshots_cover_ordering_depth_locks_and_system_priority`
executes 168 deterministic combinations across all seven algorithms. It can
write selected positions (including null results) to `ACTIVITY_TEST_SNAPSHOT_PATH`.
Run the same test against the pre-stage-3 commit `31ee0f6` and the working tree
using separately seeded databases, then compare the JSON files. No production
connection is needed. The test helper uses old-compatible call signatures so it
can be copied to the baseline alongside the partial `ActivityPostgresTests` fixture.

New placement tests cover 896 algorithm/permission combinations, deeper eligible
candidates, terminal clones, width, locks, program isolation, manual opt-in,
beneficiary identity for purchase/clone/reinvest, and committed volume/receipts.

## Opt-in performance measurements

```bash
ACTIVITY_TEST_PERF=1 \
ACTIVITY_TEST_FILTER='FullyQualifiedName~Measure_' \
ACTIVITY_TEST_PERF_OUTPUT=/tmp/cryptostyle-placement-perf \
bash tests/Modules/ReferalProgram.Application.Tests/Postgres/run.sh --no-restore
```

This runs three diagnostic benchmarks, not timing assertions. They seed 10,000
and 100,000 marketing places plus matching invites, capture the actual SQL and
parameters sent by Npgsql, and save `EXPLAIN (ANALYZE, BUFFERS, FORMAT JSON)` plans.
The main run has a 5-second per-statement limit and records timed-out plans without
ANALYZE. PostgreSQL execution medians use three warm samples; the initial query's
wall time also includes transport and Dapper materialization and is recorded
separately. Timings are local single-client measurements, not production latency
or throughput promises. No existing application database is used.

The index experiment creates a partial active-invite index only in its temporary
database. The SQL experiment derives a correlated EXISTS alternative from the captured
production membership predicate and checks result-set equality with EXCEPT ALL
before measuring both variants. Neither experiment changes runtime
SQL or deploys an index. See [measurement report](ACTIVITY_QUERY_PERFORMANCE.md).

## Stage 4 rewards and compression

Stage-4 coverage includes real bonus handlers (including referral recipients in
structure 0), independent clone/reinvest permissions, combined task branch
selection, and compression committed through EF repositories in both structure
types. It checks retained inactive chains, parent/depth/matrix filling, unchanged
activation state, paid-clone volumes and receipts. Compression rank tables are
minimal test tables; live TON sends and API transport are not covered.

## Mini configuration and expiration

Mini tests execute the setup, existing-program configuration, group update and
scheduler SQL in disposable PostgreSQL. The fixture adds migration-defined
column defaults missing from EF EnsureCreated (task-processing flag and matrix
filling). Programs and Tasks test tables share one disposable database for script
checks; deployment still targets their separate databases. Coverage includes
calendar-month expiry boundaries, null/system/root/additional-place exclusion,
program/structure isolation, unchanged nonzero volumes, retries, concurrent
renewal under a row lock, invitation prerequisites and automatic-only spillover
restrictions on structures 1–3. No existing application database is used.

## Shared activity sources

Shared-source coverage exercises all seven algorithms with own/source statuses
in opposition and each own/spillover permission combination; manual resolution,
program-scoped group minima, missing source and missing group, reward permissions,
compression and source reactivation. Mini uses structure 1 as the source for
structures 1–3 and does not expire invites. The performance harness also measures
`source_invite` and `source_group` modes (200 combinations total after removing the obsolete additional-invite mode).
