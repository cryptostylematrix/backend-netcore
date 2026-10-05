# CryptoStyle Matrix backend

ASP.NET Core backend for CryptoStyle Matrix. The API combines TON contract
access, the current Referral Program domain,
and UI profile-intent persistence.

The codebase targets **.NET 10** and uses FastEndpoints, PostgreSQL, Dapper,
Entity Framework Core, MediatR, and the included TON SDK projects.

## Repository layout

| Path | Purpose |
| --- | --- |
| `src/API/CryptoStyle.Api` | HTTP API, Swagger, dependency composition, logging, CORS, and the background task processor. |
| `src/Modules/Contracts` | TON contract queries, message construction, transaction sending, caching, and TonCenter integration. |
| `src/Modules/ReferalProgram` | Current referral-program domain, placement policies, APIs, persistence, and database scripts. |
| `src/Modules/UI` | Wallet profile-display intents, cached profile data, ownership checks, and history. |
| `src/Modules/ScheduledTasks` | System-wide UTC task scheduling, sequential in-process command execution, and marketing coordination. |
| `src/ProgramMatrixFillingRecalculator` | Dry-run-first maintenance tool for recalculating persisted matrix filling in all existing programs or one selected program. |
| `src/ProgramVolumeRecalculator` | Dry-run-first maintenance tool for rebuilding one profile-volume type in one program structure. |
| `src/LegacyPlacesXmindExporter` | Read-only export of a legacy PostgreSQL places structure to an editable XMind file. |
| `src/ProgramInviterChanger` | Administrative console application for moving a referral subtree. |
| `src/BuildingBlocks` | Shared domain, integration-event, and messaging infrastructure. |
| `src/Libs/TonSdk.*` | TON client and core libraries used by the Contracts module. |
| `tests/Modules` | Referral Program, UI, and Scheduled Tasks automated tests. |

`ReferalProgram` is the existing project and database-schema spelling, so its
name is intentionally preserved in paths and namespaces.

## Documentation

- [Legacy Places XMind Exporter](src/LegacyPlacesXmindExporter/README.md) describes exporting old-system places for manual editing before migration.

- [UI database migration with pgAdmin](src/Modules/UI/Database/PgAdmin/README.md)
  covers creating a dedicated UI database/login and moving existing data from Programs.
- [Referral Program processing invariants](src/Modules/ReferalProgram/PROGRAM_PROCESSING.md)
  explains purchase prerequisites, command selection, source-place responses,
  and the current activation status.
- [Position algorithms](src/Modules/ReferalProgram/POSITION_ALGORITHMS.md)
  documents configuration versions, operation overrides, classic, chess,
  radar, and trimmed-classic placement.
- [UI module](src/Modules/UI/README.md) covers its intent-based data model,
  ownership synchronization, endpoints, errors, and database setup.
- [Scheduled Tasks module](src/Modules/ScheduledTasks/README.md) covers task JSON,
  recurrence, deterministic correlation IDs, retries, and database setup.
- [Program Matrix Filling Recalculator](src/ProgramMatrixFillingRecalculator/README.md)
  describes checking and backfilling matrix counts for existing programs.
- [Program Volume Recalculator](src/ProgramVolumeRecalculator/README.md)
  describes checking and rebuilding personal or referral profile volume.
- [Program Inviter Changer](src/ProgramInviterChanger/README.md) describes its
  safety checks, required permissions, and invocation.
- [Referral Program database scripts](src/Modules/ReferalProgram/Database/Scripts)
  contain schema changes, permissions, cleanup utilities, and program setup
  scripts.
- [UI database scripts](src/Modules/UI/Database/Scripts) create and update the
  profile-intent schema.

Swagger is available at `/swagger` while the API is running in Development.
Legacy `/api/matrix/*` and `/api/marketing/*` routes are intentionally not
implemented and therefore are unavailable both at runtime and in Swagger.
The same applies to legacy Contracts endpoints under `Invite`, `Marketing`,
`Multi`, and `Place`, plus `ProfileItem/BuildChooseInviterBody` and
`ProfileItem/GetPrograms`. The `MarketingV3` contract endpoints and other
current Contracts endpoints remain registered.

## Jetton display metadata

`GET /contracts/jetton-wallet/{addr}/metadata` resolves the wallet's minter and
returns `minter_addr`, `name`, `symbol`, and `decimals`. The frontend uses this
endpoint instead of downloading token JSON directly from the browser.

The backend supports on-chain, off-chain, and semi-chain metadata in
[TEP-64](https://github.com/ton-blockchain/TEPs/blob/master/text/0064-token-data-standard.md),
including snake/chunked values and IPFS URIs. On-chain fields take precedence.
Missing decimals default to 9 only after metadata was successfully read; invalid
decimals and failed external requests are not treated as valid metadata.

Wallet-to-minter mappings and resolved metadata have separate cache entries,
keyed by normalized TON addresses. The default TTL is 24 hours, configurable with
`TonQueryCache__JettonMetadataTtlHours` (1–168 hours). Balances and total supply
are not cached by this endpoint. Failed lookups are retried on later requests.
Concurrent requests share one lookup per key within an API process.
The existing `IDistributedCache` implementation determines whether entries
survive API restarts; the default in-memory cache does not.

Remote JSON reads have a 10-second timeout, a 256 KiB limit, and checked redirects.
Only public HTTP(S) destinations are allowed. Internal addresses are blocked,
including after DNS resolution. For display, the frontend uses the symbol, then
the name if the symbol is absent. `JETTON` remains a fallback only when both are
missing in successfully loaded metadata.

Metadata regression tests (no database or network required):

```bash
dotnet test tests/Modules/Contracts.Infrastructure.Tests/Contracts.Infrastructure.Tests.csproj
```

## Local setup

Requirements:

- .NET 10 SDK;
- PostgreSQL databases for Referral Program, UI, and Scheduled Tasks;
- a TonCenter endpoint and API key;
- configured TON contract addresses and a 24-word processor-wallet mnemonic.

Create a local development configuration:

```bash
cp src/API/CryptoStyle.Api/.env.example \
  src/API/CryptoStyle.Api/.env.development
```

Fill the copied file with local values. Referral Program uses
`ConnectionStrings__Programs`. Scheduled Tasks uses its dedicated
`ConnectionStrings__Tasks` connection. UI uses `ConnectionStrings__UI` when supplied
and otherwise falls back to Programs.

Run the API from the repository root:

```bash
dotnet restore src/API/CryptoStyle.Api/CryptoStyle.Api.csproj
dotnet run --project src/API/CryptoStyle.Api/CryptoStyle.Api.csproj
```

The shared VS Code launch and task configurations under `.vscode` can also be
used. The default HTTP address is `http://localhost:5004`, with Swagger at
`http://localhost:5004/swagger`.

The Referral Program and Scheduled Tasks processors are disabled in Development. In other
environments the Referral Program processor runs at the configured
`TaskProcessor__IntervalSeconds` interval. `activate_place` is processed for configured profiled places; see the
[processing invariants](src/Modules/ReferalProgram/PROGRAM_PROCESSING.md#activation).

## Tests

Run the test projects independently:

```bash
dotnet test tests/Modules/ReferalProgram.Application.Tests/ReferalProgram.Application.Tests.csproj
dotnet test tests/Modules/UI.Application.Tests/UI.Application.Tests.csproj
dotnet test tests/Modules/UI.Infrastructure.Tests/UI.Infrastructure.Tests.csproj
dotnet test tests/Modules/ScheduledTasks.Application.Tests/ScheduledTasks.Application.Tests.csproj
```

Referral Program tests include placement strategies, purchase policies,
source resolution, clone kinds, setup-script topology, and infrastructure query
invariants. UI tests cover profile intents, contract adapters, and wallet address
handling. Scheduled Tasks tests cover schedules, command parsing/execution,
correlation IDs, dispatch, aggregates, and persistence mappings.

Optional activity regression tests use a disposable local Docker PostgreSQL:

```bash
bash tests/Modules/ReferalProgram.Application.Tests/Postgres/run.sh
```

See [coverage and limits](tests/Modules/ReferalProgram.Application.Tests/Postgres/README.md).
They exercise invitation, root lookup, and activation persistence with domain
events; they do not connect to existing application databases. Without explicit
opt-in, these PostgreSQL tests are skipped.

These unit and source-invariant tests do not exercise live PostgreSQL, TON calls,
or API endpoint discovery. Database cleanup scripts require separate validation
on a disposable PostgreSQL instance before operational use.

## Database scripts

Schema scripts under each module are numbered in execution order. Program
setup scripts are first-time initialization scripts and deliberately reject an
existing program. Read their headers and fill only the declared variables
before running them against the intended Programs database.

Never run a setup, cleanup, permission, or migration script against production
without reviewing its target database, role, and marketing address.

To label program contracts in the database, apply
`027_add_comment_to_referal_program.sql`. The optional `referal_program.comment`
text column can be edited directly by the table owner, for example:

```sql
UPDATE public.referal_program
SET comment = 'Silver Matrix — main contract'
WHERE marketing_addr = '<contract address>';
```

Comments are administrative database notes and are not exposed by the public API.

## Public-repository security

- Real `.env` files are ignored by Git. Commit only `.env.example` templates
  containing placeholders or local-only defaults.
- All `.env*` files are excluded from Docker build contexts.
- Never commit database passwords, TonCenter or Seq API keys, processor-wallet
  mnemonics, private keys, production connection strings, or database dumps.
- Supply production secrets through deployment environment variables or a
  secret manager, not `appsettings*.json`, command-line arguments, SQL files,
  logs, or documentation.
- Before publishing changes, review `git diff --staged` and consider running a
  dedicated history-aware secret scanner such as Gitleaks.
- If a secret is committed, removing it in a later commit is insufficient:
  rotate it immediately and remove it from Git history before relying on the
  repository as public-safe.

Public TON contract, marketing, profile, and wallet addresses are identifiers,
not private keys, but examples should still use neutral placeholders unless a
specific deployed address is intentionally being documented.

## Retired legacy database

Matrix, Marketing, the old task processor, and ProgramMigrator have been removed.
Their dedicated legacy PostgreSQL database can be removed manually in pgAdmin
after backing it up and confirming it contains no current application data.

### Specification read APIs

- `GET /api/program/{marketing_addr}/structures`: Referral Program structure settings.
- `GET /api/scheduled-tasks/schedules?module=program&scope={stored_address}&resource_type=structure`: Scheduled Tasks public schedules with generic target references.

Each module owns its endpoint and database reads. The frontend composes the
specification; the schedule read path uses a shared public-target descriptor contract instead of
querying structures from ScheduledTasks.
The old combined `/specification` endpoint is removed.
