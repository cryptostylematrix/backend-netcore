-- DESTRUCTIVE CLEANUP: run only after accepting the migration, retaining a
-- verified backup, and confirming every API replica uses cs_ui for UI data.
-- Open a fresh Query Tool connected to cs_programs as administrator.
-- Run this entire file. It deliberately leaves the transaction uncommitted.
-- Then execute COMMIT; to keep the deletion or ROLLBACK; to undo it, in the
-- SAME Query Tool connection. Resolve this promptly: locks remain until then.
-- On any error, execute ROLLBACK; before doing anything else.

BEGIN;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '30s';

DO $$
DECLARE
    v_source_database text := 'cs_programs';
BEGIN
    IF current_database() <> v_source_database THEN
        RAISE EXCEPTION 'Wrong database: expected %, connected to %.',
            v_source_database, current_database();
    END IF;

    -- No CASCADE: dependencies outside these three tables must block cleanup.
    -- No IF EXISTS: an unexpected or already-cleaned schema must be reviewed.
    -- Owned identity sequences, indexes, constraints and table grants are
    -- removed automatically along with their tables.
    DROP TABLE public.wallet_profile_intent_events,
        public.wallet_profile_intents,
        public.profiles RESTRICT;

    IF to_regclass('public.wallet_profile_intents_id_seq') IS NOT NULL
        OR to_regclass('public.wallet_profile_intent_events_id_seq') IS NOT NULL THEN
        RAISE EXCEPTION 'UI sequences remain: unexpected sequence ownership. Roll back and inspect.';
    END IF;
END $$;

SELECT current_database() AS source_database,
    'UI tables and owned sequences removed in this transaction only. Execute COMMIT or ROLLBACK in this connection now.'
        AS next_step;

-- Intentionally no COMMIT here. After reviewing the successful result, execute
-- ONE of these commands separately in this same Query Tool connection:
-- COMMIT;
-- ROLLBACK;
