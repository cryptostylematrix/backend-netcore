-- Records idempotent Program integration commands. A future structural mutation
-- and this record must be committed in the same Programs-database transaction.

BEGIN;

CREATE TABLE IF NOT EXISTS public.processed_program_commands
(
    correlation_id uuid PRIMARY KEY,
    command_type varchar(100) NOT NULL,
    marketing_address text NOT NULL,
    structure_number integer NOT NULL,
    completed_at_utc timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT processed_program_commands_structure_number_check
        CHECK (structure_number >= 0)
);

CREATE INDEX IF NOT EXISTS idx_processed_program_commands_target
    ON public.processed_program_commands
        (marketing_address, structure_number, command_type);

DO $$
DECLARE
    v_role text;
BEGIN
    FOREACH v_role IN ARRAY ARRAY['cs_programs_user', 'dev_cs_programs_user']
    LOOP
        IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = v_role) THEN
            EXECUTE format('GRANT USAGE ON SCHEMA public TO %I', v_role);
            EXECUTE format(
                'GRANT SELECT, INSERT ON TABLE public.processed_program_commands TO %I',
                v_role);
        END IF;
    END LOOP;
END;
$$;

COMMIT;
