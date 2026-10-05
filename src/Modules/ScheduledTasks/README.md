# Scheduled Tasks

See [BENEFITS.md](BENEFITS.md) for a concise English and Russian overview of
the system-wide benefits provided by this module.

The Scheduled Tasks module executes system-wide declarative commands in UTC.
It owns one table, `public.tasks`, in a dedicated PostgreSQL database configured
through `ConnectionStrings__Tasks`. Tasks are inserted and administered directly
in that database; this version intentionally has no HTTP CRUD endpoints.

`ScheduledTasks.Core` contains the `ScheduledTask` aggregate and its lifecycle
invariants. Infrastructure persists that aggregate with EF Core. The scheduler
uses at-least-once execution: parallel workers may dispatch the same occurrence,
while deterministic correlation IDs and target-module idempotency prevent repeated
business effects. PostgreSQL `xmin` ensures only one worker advances the task row
and protects manual database edits.

## Public schedules

`GET /api/scheduled-tasks/schedules?module=program&scope={stored_address}&resource_type=structure`
returns UTC timing, recurrence, status and public action references. Each action
has a type and a target (`module`, `scope`, `resource_type`, `resource_id`).
All three filters are required. Unknown modules or unmatched targets return an
empty array; database failures remain errors.

ScheduledTasks reads only its own Tasks database. It knows no program addresses,
structure settings or structure command whitelist. Command owners implement
`IntegrationRequests.Scheduling.IPublicTaskCommandDescriptor`: a parameterized
JSON containment filter and an explicit public projection of their commands.
ReferalProgram's adapter maps the existing stored command format to structure
references and permits only supported maintenance commands. Raw arguments,
internal errors and task-control commands are never returned. Actions retain
execution order and are filtered to the requested module, scope and resource type.

The frontend combines these references with
`GET /api/program/{marketing_addr}/structures` and Marketing V3 data. Pass the
stored address returned by the structures API as `scope`; the scheduler treats
it as an opaque identifier. Structure names, grouping and labels belong to the
program report. Schedule failures are shown independently of other sections.

The combined program specification route and program-specific scheduler route
are removed. Deploy backend and frontend together; persisted commands and the
DDD execution/write path are unchanged, so no database migration is needed.

## Lifecycle

- `execute_at_utc IS NULL` disables a scheduled task.
- A due `active` task executes synchronously in JSON array order. Parallel workers
  may execute the same occurrence, so every target handler must be idempotent.
- The first failed command stops execution. The task becomes `error`, retains its
  execution time and execution number, and does not recur.
- Program task processing is not disabled automatically. Add explicit disable
  and enable commands only to workflows that require normal Program processing
  to be paused. If an intermediate command fails after an explicit disable,
  processing remains disabled until the task succeeds on retry or an operator
  enables it manually.
- Set an errored task back to `active` to retry it with the same correlation IDs.
- A successful one-time task becomes `completed` and clears `execute_at_utc`.
- A successful recurring task stays `active`, increments `execution_number`, and
  moves `execute_at_utc` to the next future occurrence. Missed occurrences are
  skipped.
- Clear `execute_at_utc` to stop any task. A concurrent worker will not overwrite
  that manual decision when it finishes.

The API registers both background processors only outside Development, matching
the existing blockchain Task Processor behavior.

## Command format

Commands are a JSON array. The array position is the command sequence number.

```json
[
  {
    "module": "program",
    "type": "program.structure.update-activity",
    "version": 1,
    "target": {
      "marketingAddress": "EQ_REPLACE_ME"
    },
    "arguments": {
      "structureNumber": 1
    }
  }
]
```

The presence of Program commands does not imply that Program task processing
must be paused. When a particular workflow requires a pause, place an explicit
`program.task-processing.disable` command before the affected commands and an
explicit `program.task-processing.enable` command after them.

Supported Program command types are:

- `program.task-processing.disable`
- `program.task-processing.enable`
- `program.structure.update-activity`
- `program.structure.deactivate-expired-first-places` (recognized, temporarily disabled)
- `program.structure.compress`
- `program.structure.calculate-referral-volume`
- `program.structure.reset-referral-volume`

Each target module registers a command factory and owns its concrete MassTransit
consumers. The shared message-broker setup
discovers those consumers and configures an in-memory endpoint for each one.
The scheduler sends commands through MassTransit's request/response transport,
so it can stop the sequence and mark the task as `error` when a consumer fails.
UI and other command types can be added without changing the scheduler executor.

## Expired first-place task (disabled)

`program.structure.deactivate-expired-first-places` is recognized but temporarily
disabled. Its consumer returns an explicit disabled error; the scheduler marks
the occurrence as `error` and does not advance it. The service performs no
place queries or business mutations and writes no processed-command receipt.
The future target is profiles' first places (`place_number = 1`) in the requested
structure; its behavior will be defined separately.

The reserved command format is:

```json
{
  "module": "program",
  "type": "program.structure.deactivate-expired-first-places",
  "version": 1,
  "target": { "marketingAddress": "EQ_REPLACE_ME" },
  "arguments": {
    "structureNumber": 1,
    "period": { "unit": "months", "value": 1 }
  }
}
```

The request parser accepts a positive integer period with units `years`,
`months`, `weeks`, `days`, `hours`, or `minutes`. These parameters do not enable
the disabled service. No task creation or rename script is provided while its
behavior is on hold. `program.structure.update-activity` retains its existing
period-reset behavior.

## Correlation IDs and idempotency

The scheduler derives a UUID v5 from `(task ID, execution number, command
sequence)`. Retries of one occurrence therefore keep their IDs, while the next
recurrence receives different IDs. Do not reorder or replace commands in an
active or errored occurrence.

The processing enable/disable commands are naturally idempotent because they set
an explicit boolean value. Each Program consumer decides whether it requires a
processed-command record. Consumers that require one must write the correlation
ID to `processed_program_commands` in the Programs database. The business
mutation must use the same connection and transaction as that insert. The
correlation ID uniquely identifies the command, so the processed-command record
does not depend on a structure number.

## Schedules

`schedule = NULL` means one-time execution. Fixed UTC intervals use:

```json
{"type":"interval","unit":"seconds","value":5}
```

Supported interval units are `seconds`, `minutes`, `hours`, `days`, and `weeks`.
Calendar-month schedules use:

```json
{
  "type": "calendar",
  "unit": "months",
  "interval": 3,
  "dayOfMonth": 15,
  "timeUtc": "00:00:00"
}
```

See `Database/Scripts/002_example_tasks.sql` for manual insertion examples.
Use `Database/Scripts/004_add_cryptocash_program_tasks.sql` to create the five
recurring maintenance tasks for a CryptoCash Program after setting its marketing
address and the initial UTC execution timestamp for each structure.

## Database setup

Use Query Tool as the `postgres` user while connected to the `postgres` database
to run `ScheduledTasks/Database/Scripts/000_create_tasks_database.sql`. Execute
its single `CREATE DATABASE` statement by itself. For development, change the
database name to `dev_cs_tasks` before execution. The backend PostgreSQL role is
separate from the `postgres` migration user and does not own database objects.

Reconnect Query Tool to the newly created Tasks database and run the remaining
scripts manually in order:

1. As `postgres`, run `ScheduledTasks/Database/Scripts/001_create_tasks.sql` to
   create the table. Tasks continue to be inserted manually by an administrative
   user.
2. Open `ScheduledTasks/Database/Scripts/003_grant_tasks_permissions.sql`, set
   its empty `v_database_name` and `v_backend_role` variables, and run it as
   `postgres` while connected to that Tasks database. It removes public access,
   resets direct privileges for the selected backend role, and then grants only
   database `CONNECT`, schema `USAGE`, and table `SELECT`/`UPDATE`.
3. `ReferalProgram/Database/Scripts/021_create_processed_program_commands.sql`
   in the Programs database.
4. `ReferalProgram/Database/Scripts/022_add_task_processing_enabled_to_referal_program.sql`
   in the Programs database.

After deploying profile-scoped volume support, run
`ScheduledTasks/Database/Scripts/005_rename_referral_volume_commands.sql` in an
existing Tasks database. It updates stored personal-volume command names to the
new referral-volume names without changing command order.
