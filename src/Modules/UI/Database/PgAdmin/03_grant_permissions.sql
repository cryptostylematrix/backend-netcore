-- Query Tool connected to cs_ui, as administrator, AFTER restoring the UI tables.
BEGIN;
DO $$
BEGIN
    IF current_database() <> 'cs_ui' THEN
        RAISE EXCEPTION 'Connect to cs_ui before running this script.';
    END IF;
END $$;

REVOKE ALL ON DATABASE cs_ui FROM PUBLIC;
GRANT CONNECT ON DATABASE cs_ui TO cs_ui_app;
REVOKE CREATE ON SCHEMA public FROM PUBLIC;
GRANT USAGE ON SCHEMA public TO cs_ui_app;
GRANT SELECT, INSERT, UPDATE ON public.profiles TO cs_ui_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON public.wallet_profile_intents TO cs_ui_app;
GRANT SELECT, INSERT ON public.wallet_profile_intent_events TO cs_ui_app;
GRANT USAGE, SELECT ON SEQUENCE public.wallet_profile_intents_id_seq,
    public.wallet_profile_intent_events_id_seq TO cs_ui_app;
COMMIT;
