-- Apply after 005 in the configured UI database. Existing language values are preserved.
BEGIN;
ALTER TABLE public.wallet_preferences
    DROP CONSTRAINT wallet_preferences_language_supported,
    ALTER COLUMN language TYPE varchar(63);
ALTER TABLE public.wallet_preferences
    ADD CONSTRAINT wallet_preferences_language_format
        CHECK (language ~ '^[a-z]{2,8}(-[a-z0-9]{1,8})*$');
COMMIT;
