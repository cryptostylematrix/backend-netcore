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
- `program.structure.compress`
- `program.structure.reset-personal-volume`

Each target module registers a command factory and owns its concrete MassTransit
consumers. The shared message-broker setup
discovers those consumers and configures an in-memory endpoint for each one.
The scheduler sends commands through MassTransit's request/response transport,
so it can stop the sequence and mark the task as `error` when a consumer fails.
UI and other command types can be added without changing the scheduler executor.

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
