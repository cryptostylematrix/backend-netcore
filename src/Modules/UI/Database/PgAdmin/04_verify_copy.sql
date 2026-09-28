-- Run in BOTH cs_programs and cs_ui while all UI writers remain stopped.
-- Compare every result, including sequence is_called. All must match.
-- Content fingerprints use a streaming rolling MD5 over ordered rows to avoid
-- building one large string in memory. This is a copy check, not a security hash.
BEGIN;
SET LOCAL timezone = 'UTC';
SET LOCAL datestyle = 'ISO, YMD';
SET LOCAL extra_float_digits = 3;

CREATE TEMP TABLE ui_copy_verification (
    table_name text, row_count bigint, content_fingerprint text
) ON COMMIT DROP;

DO $$
DECLARE
    table_name text;
    sort_key text;
    row_json text;
    fingerprint text;
    total bigint;
BEGIN
    FOREACH table_name IN ARRAY ARRAY[
        'profiles', 'wallet_profile_intents', 'wallet_profile_intent_events'
    ] LOOP
        sort_key := CASE WHEN table_name = 'profiles'
            THEN 'address COLLATE "C"' ELSE 'id' END;
        fingerprint := '';
        total := 0;
        FOR row_json IN EXECUTE format(
            'SELECT to_jsonb(t)::text FROM public.%I t ORDER BY %s', table_name, sort_key
        ) LOOP
            fingerprint := md5(fingerprint || row_json);
            total := total + 1;
        END LOOP;
        INSERT INTO ui_copy_verification VALUES (table_name, total, fingerprint);
    END LOOP;
END $$;

-- pgAdmin may display only the last result set, so combine all checks in one.
SELECT table_name AS object_name, row_count::text AS row_count_or_last_value,
    content_fingerprint AS fingerprint_or_is_called
FROM ui_copy_verification
UNION ALL
SELECT 'wallet_profile_intents_id_seq', last_value::text, is_called::text
FROM public.wallet_profile_intents_id_seq
UNION ALL
SELECT 'wallet_profile_intent_events_id_seq', last_value::text, is_called::text
FROM public.wallet_profile_intent_events_id_seq
ORDER BY object_name;
COMMIT;
