# AGENTS.md

This file is the working guide for coding agents in this repository. It applies
to the entire tree unless a more specific `AGENTS.md` exists below it.

## Project overview

CryptoStyle Matrix is a modular ASP.NET Core backend for TON-based referral and
matrix programs. It targets .NET 10 and uses FastEndpoints, MediatR, PostgreSQL,
Dapper, Entity Framework Core, Serilog, and the TON SDK projects included under
`src/Libs`.

The current business module is named `ReferalProgram`. The spelling is
historical and intentional in project names, namespaces, configuration, and
database assets. Do not rename it opportunistically.

This repository is one of three separately versioned repositories in the
CryptoStyle workspace:

- `../frontend`: React/TypeScript browser application;
- `../marketing-contract-v3`: FunC/TL-B contracts and TypeScript wrappers;
- this repository: .NET API and off-chain state.

Read `../ARCHITECTURE.md` before any cross-repository change. Do not assume the
workspace parent is a Git repository, and do not commit or push in any
repository unless the user explicitly requests it.

Read the root `README.md` first. Important focused documentation:

- `src/Modules/ReferalProgram/PROGRAM_PROCESSING.md`
- `src/Modules/ReferalProgram/POSITION_ALGORITHMS.md`
- `src/Modules/UI/README.md`
- `src/ProgramMigrator/README.md`
- `src/ProgramInviterChanger/README.md`

## Repository map

- `src/API/CryptoStyle.Api`: composition root, FastEndpoints discovery,
  Swagger, logging, CORS, and the background task processor.
- `src/Modules/Contracts`: TON reads, message construction, transaction
  sending, TonCenter access, and caching.
- `src/Modules/ReferalProgram`: current referral-program business domain,
  placement algorithms, public program APIs, persistence, and SQL scripts.
- `src/Modules/UI`: profile-display intents, cached profile content, ownership
  synchronization, and intent history.
- `src/Modules/Matrix`: legacy Multi implementation retained for internal and
  migration compatibility.
- `src/Modules/Marketing`: legacy Neo/Marketing implementation retained for
  internal and migration compatibility.
- `src/ProgramMigrator`: legacy Multi and Neo import console application.
- `src/ProgramInviterChanger`: administrative referral-subtree mover.
- `src/TaskProcessor`: orphaned legacy reference artifacts with no project file;
  it is not built or registered. Do not treat it as the live task processor.
- `src/BuildingBlocks`: shared domain and messaging abstractions.
- `src/Libs/TonSdk.*`: vendored TON SDK projects; expect existing compiler
  warnings and avoid unrelated rewrites.
- `tests/Modules`: automated tests for Referral Program and UI behavior.

## Public API boundary

Endpoint exposure is security-sensitive. `CryptoStyle.Api/Program.cs` uses
explicit FastEndpoints assembly discovery.

Public presentation assemblies are currently:

- Contracts
- ReferalProgram
- UI

The Matrix and Marketing presentation assemblies are deliberately not
registered. Their `/api/matrix/*` and `/api/marketing/*` routes must remain
unavailable at runtime and absent from Swagger unless the user explicitly asks
to restore them.

The following endpoint groups inside Contracts are legacy and are filtered out
of discovery even though their source remains in the repository:

- `Invite/*`
- `Marketing/*` (but not `MarketingV3/*`)
- `Multi/*`
- `Place/*`
- `ProfileItem/BuildChooseInviterBody`
- `ProfileItem/GetPrograms`

Do not merely hide legacy routes from Swagger. They must not be registered in
the runtime route table. Preserve the code until deletion is explicitly
requested.

## Sources of truth

Use the maintained backend code, tests, `PROGRAM_PROCESSING.md`, and
`POSITION_ALGORITHMS.md` as the source of truth for off-chain authorization,
placement, locks, tree actions, and persistence. Program descriptions in the
contracts repository explain contract intent but do not override backend
policies.

Use `../marketing-contract-v3/contracts`, its TL-B files, and its generated
TypeScript wrappers as the source of truth for op-codes, cell layouts, task
payloads, and contract message serialization. Never guess a wire format from a
DTO name or an old processor implementation.

When adding a setup script from a program document, confirm every ambiguous
topology or policy choice with the user. Keep confirmed backend policies even
when the contract document is silent about them.

## Module architecture

Modules generally use these layers:

- Core: aggregates, entities, value semantics, domain events, repositories.
- Application: commands, queries, handlers, policies, strategies, and ports.
- Dto: serialized request/response contracts.
- Infrastructure: EF Core persistence, Dapper reads, external adapters, DI.
- Presentation: FastEndpoints endpoints and HTTP mapping.

Keep dependencies pointing inward. Business logic belongs in aggregates,
domain services, policies, strategies, or command handlers—not in the API
background service or endpoint classes.

Use MediatR request/response adapters across module boundaries. In particular,
application code outside Contracts must not directly depend on Contracts
infrastructure query implementations.

Commands mutate through repositories and `IProgramUnitOfWork`. Query handlers
use query abstractions and DTOs. Do not make Dapper query objects perform
domain mutations.

Use domain events for effects that must accompany aggregate changes. Ensure
all required decisions are made before `SaveChangesAsync`; a failed business
path must not persist a partial place or lock.

`DataContext.SaveChangesAsync` dispatches and clears domain events before EF
persists the tracked graph. Event handlers mutate tracked aggregates and must
not call `SaveChangesAsync` recursively. These mutations and the originating
aggregate are committed by the same EF save.

## Referral Program invariants

Treat `PROGRAM_PROCESSING.md` and `POSITION_ALGORITHMS.md` as the maintained
source of truth. Important rules include:

- A place identity is unique by marketing address, structure number, profile
  address, and place number. Profile address may be null for system places.
- `max_places_per_profile = 0` means unlimited. A classic structure width of
  `0` is also unlimited; chess and radar require a positive width.
- `height` controls source-place response traversal. `display_height` controls
  how many levels the tree endpoint renders; do not substitute one for the
  other.
- Normalize an incoming profile address to null when it is null, empty, or
  whitespace where the query explicitly supports system places.
- Place kinds are: purchased `0`, clone/reinvest `1`, terminal clone `2`.
- Terminal clones cannot receive children and must be excluded from every
  open-position candidate query.
- `classic` may honor an explicitly selected position. `chess` and `radar`
  ignore selected positions and calculate their own candidate.
- Selected classic positions for profiled purchases must be inside the
  profile's resolved subtree and outside its locks. System purchases skip the
  profile-subtree check but still validate position and locks.
- Locks belong to the resolved root profile. If profile-root resolution falls
  back to an active inviter, use that inviter profile and its locks.
- Chess and radar can prioritize profiled places and search a configured depth
  spread. Respect both settings and all locks.
- `trimmed_classic` is used by Mini clone/reinvest overrides. Count direct kind
  `1` and kind `2` clone children of the selected parent. Every Nth clone is
  kind `2`; `cut_factor` must be at least 2.
- `prev_required` is checked before command selection and is not bypassed by
  `buy_first_place`.
- `buy_first_place` applies only when the profile has no place in any structure
  whose contract configuration exposes that command.
- `buy_top_place` is retired. Do not restore it.
- Any viewer may buy for the requested profile. Wallet ownership affects UI
  warnings, not purchase authorization for that profile.
- Tree actions, buy/lock allowances, roots, next positions, and viewer locks
  are calculated on the backend and depend on viewer context.
- Tree requests require both viewer profile and viewer wallet addresses. The
  wallet determines ownership-sensitive lock actions, but it does not have to
  own the viewer profile for buying on that profile's behalf.
- A filled tree node exposes both matrix-place count and descendant count.
  Matrix-place count uses persisted `matrix_filling`, includes the root, and is
  bounded by structure `height`; non-Matrix structures return `1`. Descendant
  count excludes the root and covers the complete subtree. GetTree loads both
  values for all displayed filled nodes in one batched parent-tree query.
- A Matrix structure is a structure whose width and height are both positive.
  A matrix is the group rooted at a place and bounded by those dimensions;
  every child is also the root of its own overlapping matrix.
- GetPlaces returns `matrix_size` and `matrix_filling`, both including the
  listed root place. Matrix size is `1 + width + ... + width^height` for a
  Matrix structure. Otherwise both values are the single-place fallback `1`,
  which does not make the place a matrix. The optional `only_not_closed` filter
  applies only to Matrix structures, keeps rows where filling is less than
  size, and must be applied before pagination totals are calculated.
- Matrix filling is persisted on `places`, starts at `1`, and is incremented
  atomically for the new place's ancestors through the structure height when
  `PlaceCreatedDomainEvent` is handled. The transactional repository SQL reads
  width and height from `structures`; do not reintroduce a separate Dapper
  structure read into this write path. It selects ancestors by MP prefix and
  depth difference in one atomic update; do not replace it with handler-side
  read/modify/write iteration, which can lose concurrent increments.
  Existing/imported programs must be
  backfilled with `ProgramMatrixFillingRecalculator`; do not restore expensive
  per-row descendant counting to GetPlaces.
- Source-place resolution walks up by structure height. If the requested
  height cannot be reached, use the last reachable parent (or the created
  place itself) and return response code 0. Height 0 uses the created place as
  its source.
- Structure rank shown in a filled tree node is calculated dynamically from
  structure ranks and personal volume. System-place rank is null.
- Choosing an inviter creates the profile's inactive structure-0 first place
  beneath an active profiled inviter. The profile's first paid place in any
  structure above zero—purchase, clone, or reinvest—activates that invite;
  later paid places never reactivate it. A purchase increments the inviter's
  first-place personal volume in the purchased structure when that place
  exists. System places do not produce profile paid-place effects.
- Creating any child updates its tracked parent's filling through
  `PlaceCreatedDomainEvent`; the expected filling must still equal
  `position - 1`, which protects left-to-right insertion from stale writes.

Activation targets an existing profiled place whose `activated_at` is null.
The structure must expose `activate_place` and have non-null activity JSON.
Activation uses purchase-style source resolution and the centralized Marketing
task receipt for idempotency.

## Task processor

The Referral Program task processor is registered only outside Development and
runs according to `TaskProcessor__IntervalSeconds`.

For each configured referral program it gets the first contract task and
processes at most that program's current task. A processed command is identified
by `(marketing_addr, task_key)`. If its receipt has no recorded response attempt,
resend the stored response without executing the command again. If the contract
returns the same task after an attempt was recorded, mark delivery as failed and
disable all task processing for that marketing until manual resolution.

`marketing_tasks` is the authoritative idempotency boundary for every marketing
command, including commands that do not create a place. A successful command
atomically persists its domain changes plus an immutable processed-task receipt.
The receipt references both the affected place and the response source place
and stores the exact response code. A retry must resend that stored response
without re-running the command or recalculating mutable matrix state. A receipt
has no lifecycle status: its existence means that Programs processing committed.
Places do not store task keys or task query IDs; task-to-place correlation
belongs in `marketing_tasks`.

Delivery metadata is separate from the immutable response snapshot.
`response_attempted_at` is written only after the processor wallet accepts the
send. Seeing the same contract task after that point atomically sets `error_at`
and `error_reason` and disables the referral program. Manually enabling the
program clears the latest failed receipt's delivery metadata so only its stored
response is retried; the Programs command must never run again.

Command handlers resolve their response, raise a
`MarketingCommandProcessedDomainEvent` on the affected place, and perform one
unit-of-work save. Its synchronous handler adds the receipt without recursively
saving, so all domain events, aggregate changes, and the receipt commit
atomically. A business error that leads to cancellation must not persist a
partial command result or receipt. Query and cancelled tasks do not create
receipts because they do not commit Programs mutations.

`MarketingTransactionSender` serializes wallet sends, retries configured TON
failures, and waits for the processor wallet seqno to advance. Durable receipt
delivery metadata controls command-response retries; do not add in-memory task
suppression that would prevent a manual retry. The stored receipt makes a later
send retry safe if the process crashes.

Keep task decoding and parameter validation in task processing. Put business
authorization and state changes in application command handlers/policies.
Lock and unlock success sends a command response using the parent place as the
source; it does not cancel the task. Cancel only genuine failed task paths.

Do not reintroduce direct Contracts infrastructure calls from task processing;
use application requests and responses.

Combined command/query tasks are handled as move-or-structure-bonus: the
create-clone command and structure-bonus query must refer to the same relative
source. The resolver chooses exactly one response path. Profile-info queries,
bonus queries, clone, reinvest, lock, unlock, activation, purchases, and inviter
selection have explicit branches; unknown commands are unsupported.

## Runtime configuration

The API loads `.env.development` in Development and `.env` otherwise, looking
from the content root, the repository-relative API directory, and the app base
directory. Environment variables are added to configuration after dotenv
loading. Keep option names aligned with `.env.example`.

Swagger and Swagger UI are Development-only. Seq is enabled only in Production
and only when `SEQ_URL` is configured. The task processor is enabled in every
environment except Development.

The current API uses the `OpenCors` policy and has no authentication middleware.
UI intent endpoints are therefore anonymous and wallet addresses are caller
claims, not authenticated identities. Do not describe UI intent or ownership
state as cryptographic proof, and treat any change to CORS or authentication as
a deliberate public API/security change.

## Databases and persistence

There are distinct logical connection strings:

- `ConnectionStrings__Matrix`: shared by legacy Matrix and Marketing modules.
- `ConnectionStrings__Programs`: Referral Program database.
- `ConnectionStrings__UI`: optional UI database; falls back to Programs when
  empty.

Even when UI and Referral Program use the same physical database, each module
must resolve and use its own correctly configured context/unit of work. Never
replace multiple module registrations with one unqualified `NpgsqlDataSource`
or connection string.

PostgreSQL conventions:

- SQL names use snake_case.
- Use `=` for SQL equality, never C#-style `==`.
- Map unsigned .NET values to PostgreSQL types/check constraints carefully;
  PostgreSQL has no native unsigned integer types.
- Parameterize all runtime queries.
- Keep marketing address and structure number predicates in placement queries.
- Preserve nullable profile semantics with `IS NOT DISTINCT FROM` where null
  is part of identity matching.

Referral Program SQL scripts are under
`src/Modules/ReferalProgram/Database/Scripts`. Numbered files are schema
changes. Setup files create a program, its structures, and top places as one
transaction. They are intentionally non-idempotent unless their header says
otherwise.

When adding or changing a setup script:

- declare environment-specific values once at the top;
- keep usernames, addresses, and owner profile values as variables;
- validate variables, role permissions, duplicate programs, and inserted row
  counts;
- use UTC epoch seconds where the existing schema expects bigint timestamps;
- preserve fixed-width eight-character hexadecimal MP segments. Existing data
  can contain lowercase seed/migration segments and uppercase runtime segments,
  so do not assume uniform casing or change case-sensitive prefix behavior
  without a migration and tests;
- update `ProgramSetupScriptTests` with the confirmed topology;
- never put production credentials in SQL.

Do not run setup, cleanup, permission, or migration scripts against a database
unless the user explicitly asks for execution and the target is unambiguous.

## Administrative console applications

`ProgramMigrator` is dry-run by default and imports structure 0 separately from
structures 1+ and locks. `--apply` is required for writes, and each selected
scope is one PostgreSQL transaction. Exact duplicate legacy locks are skipped;
do not weaken other identity or topology validation merely to make an import
finish. It recalculates matrix filling for each imported structure. Consult its
README before changing legacy-field mappings.

`ProgramMatrixFillingRecalculator` checks all existing programs in dry-run mode
by default; `--marketing-addr` restricts it to one program, and `--apply` enables
writes. Apply mode processes and verifies each program in its own transaction,
locking `places` while that program is updated. Stop the task processor and
other Programs-database writers while it runs.

`ProgramInviterChanger` moves only a structure-0 referral subtree. It resolves
logins through the contracts API, recalculates descendant MP and depth values,
updates affected lock MPs, and applies the database mutation atomically. Keep
its preview/confirmation and permission checks. Never expose it as a public API
without explicit authorization and an authentication design.

## API and DTO conventions

- Endpoints are thin FastEndpoints adapters around MediatR requests.
- Keep stable route and JSON contracts unless the user requests a breaking
  change.
- DTO JSON names use snake_case via `JsonPropertyName` where needed.
- Return structured Result errors consistently; do not leak raw connection
  strings, mnemonics, SQL parameters, or internal exception details publicly.
- Swagger schema IDs use fully qualified type names because modules contain
  DTOs with the same class names.
- Development Swagger is available at `/swagger`; it is not enabled by the
  current composition root outside Development.

## UI module semantics

Wallet/profile records represent a wallet's intention to view a profile, not
cryptographic proof of ownership.

- Normalize wallet addresses to user-friendly, non-bounceable, URL-safe form
  before persistence.
- `mode` records intent (`owner` or `preview`); `owned` records the last verified
  on-chain state.
- Add is idempotent and must not duplicate `added` events.
- Ownership transitions create `ownership_lost` or `ownership_gained`, never a
  second `added` event.
- Removal retains append-only history.
- Contract reads go through Contracts application requests.

Consult `src/Modules/UI/README.md` before changing this module's behavior or
public responses.

## Security and configuration

This is a public repository.

- Never commit real `.env` files. `.env.example` may contain only blanks,
  placeholders, and localhost-safe defaults.
- Never commit database passwords, authenticated connection strings, TonCenter
  or Seq keys, wallet mnemonics, private keys, dumps, or production-only URLs
  containing credentials.
- `.gitignore` allows `.env.example` and ignores other `.env*` files.
- `.dockerignore` must continue excluding all `.env*` files from Docker build
  contexts.
- Do not print secrets while diagnosing configuration. Report key names and
  whether values are missing, not the values themselves.
- If a credential may have entered Git history, advise immediate rotation and
  history cleanup; deleting it in a later commit is insufficient.
- Preserve explicit public endpoint discovery and all legacy endpoint filters.

There is currently a restore/build advisory for `Microsoft.OpenApi` 2.3.0
(`NU1903`, high severity). Do not silently upgrade unrelated dependencies, but
surface the warning and address it when dependency remediation is requested.

## Coding conventions

- Follow existing C# style: file-scoped namespaces, nullable annotations,
  primary constructors where already used, async APIs, and cancellation-token
  propagation.
- Prefer descriptive domain names over abbreviations in new code, while
  preserving established external/schema names.
- Use checked conversions/arithmetic for bounded place numbers, depths,
  positions, and counters.
- Handle cancellation separately from normal failures; do not convert a
  requested cancellation into a business error.
- Avoid broad catch blocks unless translating an application boundary result;
  never swallow exceptions silently.
- Keep policies under `Application/Policies` and strategies under the relevant
  `Application/Services/*Strategies` folder.
- Add an abstraction and resolver when introducing a new root or position
  strategy; do not grow central switches indefinitely.
- Preserve unrelated dirty-worktree changes.

## Build and test

Build the API:

```bash
dotnet restore src/API/CryptoStyle.Api/CryptoStyle.Api.csproj
dotnet build src/API/CryptoStyle.Api/CryptoStyle.Api.csproj --no-restore
```

For a full repository build, use the tracked solution:

```bash
dotnet build backend-netcore.sln
```

Run tests:

```bash
dotnet test tests/Modules/ReferalProgram.Application.Tests/ReferalProgram.Application.Tests.csproj
dotnet test tests/Modules/UI.Application.Tests/UI.Application.Tests.csproj
dotnet test tests/Modules/UI.Infrastructure.Tests/UI.Infrastructure.Tests.csproj
```

These are currently the only automated test projects. A green solution test
run verifies Referral Program application behavior plus selected SQL-text
invariants/setup topologies, UI application behavior, and UI wallet-address
infrastructure behavior. It does **not** establish coverage of every project.

There is currently no automated integration/end-to-end coverage for:

- API startup, middleware, endpoint discovery/filtering, Swagger, or CORS;
- the background task processor and real transaction-send lifecycle;
- Contracts parsing, message building, TonCenter resilience, or live TON calls;
- Referral Program Dapper queries against a real PostgreSQL schema and public
  FastEndpoints request/response mapping;
- legacy Matrix and Marketing modules;
- ProgramMigrator or ProgramInviterChanger against real source/destination
  databases;
- actually executing schema/setup/permission/cleanup SQL; or
- frontend and smart-contract behavior, which live in separate repositories.

Do not say "everything is tested" merely because `dotnet test` passes. State
the passing projects/counts from that run and disclose relevant untested
boundaries. Database, TON, or cross-repository tests require explicit safe test
configuration and must never target production implicitly.

For a focused change, run the nearest test project first. Before handoff, run
all affected projects and `git diff --check`. Build output contains many
pre-existing nullable/unused-field warnings from the vendored TON SDK; identify
new warnings separately rather than claiming the whole build is warning-free.

Important Referral Program coverage includes:

- position configuration parsing and operation overrides;
- root and algorithm strategy resolution;
- locks and viewer-dependent action policies;
- buy, system buy, clone, and reinvest command handlers;
- source-place resolution;
- domain-event effects;
- terminal clone behavior and infrastructure candidate predicates;
- all program setup-script topologies.

When changing a critical placement rule, add tests at the smallest pure unit
and at the handler/infrastructure boundary where practical. Do not rely only on
Swagger or manual tree inspection.

## Working safely

- Start by reading relevant files and checking `git status`.
- Preserve user changes and avoid broad formatting passes.
- Use reversible, scoped edits.
- Do not delete legacy modules, routes, scripts, or compatibility code merely
  because they are hidden or unused; deletion requires explicit authorization.
- Diagnose read-only when the user asks only for explanation or review.
- For implementation requests, verify behavior proportionally and report
  exact commands and any remaining warnings or deferred behavior.
