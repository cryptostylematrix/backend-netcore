-- Recreates the processed marketing-task table with its final identity.
--
-- WARNING: this drops every existing row from public.marketing_tasks.
-- Run while connected to the correct programs database as the table owner.

BEGIN;

DROP TABLE IF EXISTS public.marketing_tasks;

CREATE TABLE public.marketing_tasks
(
    marketing_addr text NOT NULL,
    task_key integer NOT NULL,
    task_query_id bigint NOT NULL,
    status varchar(50) NOT NULL DEFAULT 'completed',
    created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT marketing_tasks_pkey
        PRIMARY KEY (marketing_addr, task_key),
    CONSTRAINT marketing_tasks_status_check
        CHECK (status IN ('completed'))
);

CREATE INDEX idx_marketing_tasks_status
    ON public.marketing_tasks (status);

DO $$
DECLARE
    v_role text;
BEGIN
    FOREACH v_role IN ARRAY ARRAY['cs_programs_user', 'dev_cs_programs_user']
    LOOP
        IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = v_role) THEN
            EXECUTE format(
                'GRANT USAGE ON SCHEMA public TO %I',
                v_role);
            EXECUTE format(
                'GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE public.marketing_tasks TO %I',
                v_role);
        END IF;
    END LOOP;
END;
$$;

COMMIT;
