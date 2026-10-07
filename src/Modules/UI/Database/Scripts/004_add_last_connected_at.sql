-- Apply after 003 to the configured UI database (or Programs fallback).
-- Existing rows stay NULL until the next connection: their previous connection time is unknown.
BEGIN;
ALTER TABLE public.ton_connections
    ADD COLUMN last_connected_at timestamptz NULL;
COMMIT;
