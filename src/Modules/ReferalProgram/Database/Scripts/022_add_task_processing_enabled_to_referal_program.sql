-- Adds the Program-owned switch used by the blockchain task processor.
-- Run this script as the postgres user; it explicitly grants the backend roles
-- their runtime rights.

BEGIN;

ALTER TABLE public.referal_program
    ADD COLUMN IF NOT EXISTS is_task_processing_enabled boolean NOT NULL DEFAULT true;

DO $$
DECLARE
    v_role text;
BEGIN
    FOREACH v_role IN ARRAY ARRAY['cs_programs_user', 'dev_cs_programs_user']
    LOOP
        IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = v_role) THEN
            EXECUTE format(
                'GRANT CONNECT ON DATABASE %I TO %I',
                current_database(),
                v_role);
            EXECUTE format('GRANT USAGE ON SCHEMA public TO %I', v_role);
            EXECUTE format(
                'GRANT SELECT ON TABLE public.referal_program TO %I',
                v_role);
            EXECUTE format(
                'GRANT UPDATE (is_task_processing_enabled) '
                'ON TABLE public.referal_program TO %I',
                v_role);
        END IF;
    END LOOP;
END;
$$;

COMMIT;
