-- Creates the system-wide scheduled-task store.
-- Run as the postgres user after connecting to the dedicated Tasks database.
-- Run 003_grant_tasks_permissions.sql afterwards to configure backend access.

BEGIN;

CREATE TABLE IF NOT EXISTS public.tasks
(
    id uuid PRIMARY KEY,
    execution_number bigint NOT NULL DEFAULT 1,
    execute_at_utc timestamp with time zone NULL,
    schedule jsonb NULL,
    status varchar(20) NOT NULL DEFAULT 'active',
    commands jsonb NOT NULL,
    error text NULL,
    created_at_utc timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT tasks_execution_number_check CHECK (execution_number > 0),
    CONSTRAINT tasks_status_check CHECK (status IN ('active', 'error', 'completed')),
    CONSTRAINT tasks_commands_array_check CHECK (jsonb_typeof(commands) = 'array')
);

CREATE INDEX IF NOT EXISTS idx_tasks_due
    ON public.tasks (status, execute_at_utc)
    WHERE execute_at_utc IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_tasks_commands_gin
    ON public.tasks USING gin (commands jsonb_path_ops);

COMMIT;
