-- Creates and provisions the system-wide scheduled-task store.
-- Run as the postgres user after connecting to the dedicated Tasks database.
-- The runtime roles must already exist; their passwords are managed separately.

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

DO $$
DECLARE
    v_role text;
BEGIN
    EXECUTE format(
        'REVOKE CONNECT ON DATABASE %I FROM PUBLIC',
        current_database());

    FOREACH v_role IN ARRAY ARRAY['cs_tasks_user', 'dev_cs_tasks_user']
    LOOP
        IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = v_role) THEN
            EXECUTE format(
                'GRANT CONNECT ON DATABASE %I TO %I',
                current_database(),
                v_role);
            EXECUTE format('GRANT USAGE ON SCHEMA public TO %I', v_role);
            EXECUTE format(
                'GRANT SELECT, UPDATE ON TABLE public.tasks TO %I',
                v_role);
        END IF;
    END LOOP;
END;
$$;

COMMIT;
