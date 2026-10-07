-- Apply to the configured UI database (or Programs fallback).
-- Set the API role below before execution. Language preferences are independent of connection telemetry.
BEGIN;
CREATE TABLE public.wallet_preferences
(
    wallet_addr varchar(600) PRIMARY KEY,
    language varchar(2) NOT NULL,
    updated_at timestamptz NOT NULL,
    CONSTRAINT wallet_preferences_wallet_not_blank CHECK (BTRIM(wallet_addr) <> ''),
    CONSTRAINT wallet_preferences_language_supported
        CHECK (language IN ('de', 'en', 'es', 'fr', 'hu', 'it', 'kk', 'pl', 'pt', 'ru', 'uk'))
);
DO $$
DECLARE
    v_database_username text := '';
BEGIN
    IF NULLIF(BTRIM(v_database_username), '') IS NULL THEN
        RAISE EXCEPTION 'v_database_username must be filled.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = v_database_username) THEN
        RAISE EXCEPTION 'Database role % does not exist.', v_database_username;
    END IF;
    EXECUTE format('GRANT SELECT, INSERT, UPDATE ON public.wallet_preferences TO %I', v_database_username);
END $$;
COMMIT;
