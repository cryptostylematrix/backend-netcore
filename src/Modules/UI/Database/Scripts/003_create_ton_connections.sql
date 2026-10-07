-- Apply to ConnectionStrings__UI (or Programs when UI falls back to it).
-- Set the API database role below before execution. No existing data is changed.
BEGIN;

CREATE TABLE public.ton_connections
(
    wallet_addr      varchar(600) PRIMARY KEY,
    contract_version varchar(32) NOT NULL,
    wallet_name      varchar(128) NOT NULL,
    app_version      varchar(64) NOT NULL,
    platform         varchar(32) NOT NULL,
    created_at       timestamptz NOT NULL,
    updated_at       timestamptz NOT NULL,
    CONSTRAINT ton_connections_wallet_not_blank CHECK (BTRIM(wallet_addr) <> ''),
    CONSTRAINT ton_connections_version_not_blank CHECK (BTRIM(contract_version) <> ''),
    CONSTRAINT ton_connections_name_not_blank CHECK (BTRIM(wallet_name) <> ''),
    CONSTRAINT ton_connections_app_version_not_blank CHECK (BTRIM(app_version) <> ''),
    CONSTRAINT ton_connections_platform_not_blank CHECK (BTRIM(platform) <> '')
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
    EXECUTE format('GRANT SELECT, INSERT, UPDATE ON TABLE public.ton_connections TO %I', v_database_username);
END $$;

COMMIT;
