# CryptoStyle Matrix backend

ASP.NET Core backend for CryptoStyle Matrix. The API combines TON contract
access, legacy Matrix and Marketing reads, the current Referral Program domain,
and UI profile-intent persistence.

The codebase targets **.NET 10** and uses FastEndpoints, PostgreSQL, Dapper,
Entity Framework Core, MediatR, and the included TON SDK projects.

## Repository layout

| Path | Purpose |
| --- | --- |
| `src/API/CryptoStyle.Api` | HTTP API, Swagger, dependency composition, logging, CORS, and the background task processor. |
| `src/Modules/Contracts` | TON contract queries, message construction, transaction sending, caching, and TonCenter integration. |
| `src/Modules/Matrix` | Legacy Multi matrix code retained for internal compatibility; its presentation assembly is not registered publicly. |
| `src/Modules/Marketing` | Legacy Neo marketing code retained for internal compatibility; its presentation assembly is not registered publicly. |
| `src/Modules/ReferalProgram` | Current referral-program domain, placement policies, APIs, persistence, and database scripts. |
| `src/Modules/UI` | Wallet profile-display intents, cached profile data, ownership checks, and history. |
| `src/Modules/ScheduledTasks` | System-wide UTC task scheduling, sequential in-process command execution, and marketing coordination. |
| `src/ProgramMigrator` | Console application for importing legacy Multi and Neo data. |
| `src/ProgramMatrixFillingRecalculator` | Dry-run-first maintenance tool for recalculating persisted matrix filling in all existing programs or one selected program. |
| `src/ProgramInviterChanger` | Administrative console application for moving a referral subtree. |
| `src/BuildingBlocks` | Shared domain, integration-event, and messaging infrastructure. |
| `src/Libs/TonSdk.*` | TON client and core libraries used by the Contracts module. |
| `tests/Modules` | Referral Program and UI automated tests. |

`ReferalProgram` is the existing project and database-schema spelling, so its
name is intentionally preserved in paths and namespaces.

## Documentation

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
- [Program Migrator](src/ProgramMigrator/README.md) describes dry runs,
  structure-specific imports, Multi and Neo configuration, and applying data.
- [Program Matrix Filling Recalculator](src/ProgramMatrixFillingRecalculator/README.md)
  describes checking and backfilling matrix counts for existing programs.
- [Program Inviter Changer](src/ProgramInviterChanger/README.md) describes its
  safety checks, required permissions, and invocation.
- [Referral Program database scripts](src/Modules/ReferalProgram/Database/Scripts)
  contain schema changes, permissions, cleanup utilities, and program setup
  scripts.
- [UI database scripts](src/Modules/UI/Database/Scripts) create and update the
  profile-intent schema.

Swagger is available at `/swagger` while the API is running in Development.
Legacy `/api/matrix/*` and `/api/marketing/*` routes are intentionally not
registered and therefore are unavailable both at runtime and in Swagger.
The same applies to legacy Contracts endpoints under `Invite`, `Marketing`,
`Multi`, and `Place`, plus `ProfileItem/BuildChooseInviterBody` and
`ProfileItem/GetPrograms`. The `MarketingV3` contract endpoints and other
current Contracts endpoints remain registered.

## Local setup

Requirements:

- .NET 10 SDK;
- PostgreSQL databases for Matrix/Marketing and Referral Program;
- a TonCenter endpoint and API key;
- configured TON contract addresses and a 24-word processor-wallet mnemonic.

Create a local development configuration:

```bash
cp src/API/CryptoStyle.Api/.env.example \
  src/API/CryptoStyle.Api/.env.development
```

Fill the copied file with local values. Matrix and Marketing share
`ConnectionStrings__Matrix`; Referral Program uses
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

The Referral Program task processor is disabled in Development. In other
environments it runs at the configured `TaskProcessor__IntervalSeconds`
interval. `activate_place` handling is intentionally deferred; see the
[processing invariants](src/Modules/ReferalProgram/PROGRAM_PROCESSING.md#activation).

## Tests

Run the test projects independently:

```bash
dotnet test tests/Modules/ReferalProgram.Application.Tests/ReferalProgram.Application.Tests.csproj
dotnet test tests/Modules/UI.Application.Tests/UI.Application.Tests.csproj
dotnet test tests/Modules/UI.Infrastructure.Tests/UI.Infrastructure.Tests.csproj
```

Referral Program tests include placement strategies, purchase policies,
source resolution, clone kinds, setup-script topology, and infrastructure query
invariants.

## Database scripts

Schema scripts under each module are numbered in execution order. Program
setup scripts are first-time initialization scripts and deliberately reject an
existing program. Read their headers and fill only the declared variables
before running them against the intended Programs database.

Never run a setup, cleanup, permission, or migration script against production
without reviewing its target database, role, and marketing address.

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
