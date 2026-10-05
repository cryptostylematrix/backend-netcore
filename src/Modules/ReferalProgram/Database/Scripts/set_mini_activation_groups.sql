-- Configures structural groups for an existing MINI program.
-- Requires migration 028_add_group_to_structures.sql.
-- Fill v_marketing_addr with the MINI program address, then run the whole file
-- against the Programs database as the table owner or a role with SELECT on
-- referal_program and SELECT/UPDATE on structures.
-- Safe to repeat. Removes retired activation_sync settings from structures 1-17.
-- Preserves other activity settings and does not mutate places.
-- Groups are organizational labels; structures 1-3 activate immediately.

BEGIN;

DO $$
DECLARE
    v_marketing_addr text := '';
    v_updated_rows integer;
BEGIN
    v_marketing_addr := NULLIF(BTRIM(v_marketing_addr), '');
    IF v_marketing_addr IS NULL THEN
        RAISE EXCEPTION 'v_marketing_addr must contain the MINI program address.';
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM public.referal_program
        WHERE marketing_addr = v_marketing_addr
    ) THEN
        RAISE EXCEPTION 'Program % does not exist.', v_marketing_addr;
    END IF;

    PERFORM 1 FROM public.structures
    WHERE marketing_addr = v_marketing_addr
      AND structure_number BETWEEN 1 AND 17
    ORDER BY structure_number
    FOR UPDATE;

    IF EXISTS (
        SELECT 1 FROM public.structures
        WHERE marketing_addr = v_marketing_addr
          AND structure_number BETWEEN 1 AND 3
          AND activity IS NOT NULL
          AND jsonb_typeof(activity) <> 'object'
    ) THEN
        RAISE EXCEPTION 'MINI structures 1-3 must have an activity object or SQL NULL.';
    END IF;

    UPDATE public.structures AS structure
    SET "group" = groups.group_name,
        activity = CASE WHEN structure.structure_number BETWEEN 1 AND 3
            THEN (COALESCE(structure.activity, '{}'::jsonb) - 'activation_sync')
                || '{"set_active_on_activation":true}'::jsonb
            WHEN jsonb_typeof(structure.activity) = 'object'
                THEN structure.activity - 'activation_sync'
            ELSE structure.activity
        END
    FROM (VALUES
        (1, 'Mini 10'),
        (2, 'Mini 10'),
        (3, 'Mini 10'),
        (4, 'Mini 50'),
        (5, 'Mini 50'),
        (6, 'Mini 100'),
        (7, 'Mini 100'),
        (8, 'Mini 200'),
        (9, 'Mini 200'),
        (10, 'Mini 500'),
        (11, 'Mini 500'),
        (12, 'Mini 1200'),
        (13, 'Mini 1200'),
        (14, 'Mini 3000'),
        (15, 'Mini 3000'),
        (16, 'Mini 7000'),
        (17, 'Mini 7000')
    ) AS groups(structure_number, group_name)
    WHERE structure.marketing_addr = v_marketing_addr
      AND structure.structure_number = groups.structure_number;

    GET DIAGNOSTICS v_updated_rows = ROW_COUNT;
    IF v_updated_rows <> 17 THEN
        RAISE EXCEPTION 'Expected 17 MINI structures, updated %. Transaction aborted.',
            v_updated_rows;
    END IF;

    RAISE NOTICE 'Configured MINI groups for %. Immediate activation enabled for structures 1-3.',
        v_marketing_addr;
END;
$$;

COMMIT;
