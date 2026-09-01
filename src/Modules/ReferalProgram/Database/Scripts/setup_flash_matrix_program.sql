-- Creates the Flash Matrix referral program, structures 0 through 8, and the
-- first place in every structure as one atomic operation.
--
-- Fill the four variables below before running the script:
--   v_database_username
--   v_marketing_addr
--   v_owner_profile_addr
--   v_owner_profile_login
--
-- Suggested database users:
--   development: dev_cs_programs_user
--   production:  cs_programs_user
--
-- This script is intended for first-time initialization and is not idempotent.
-- Run it while connected to the correct programs database as its table owner.

BEGIN;

DO $$
DECLARE
    v_database_username text := '';
    v_marketing_addr text := '';
    v_owner_profile_addr text := '';
    v_owner_profile_login text := '';

    v_pos_algo jsonb :=
        '{
            "v": 1,
            "root": "profile",
            "relation": "relative",
            "groups": [
                { "id": 0, "algo": "classic", "weight": 1 }
            ]
        }'::jsonb;

    v_structure_number integer;
    v_created_at bigint := EXTRACT(EPOCH FROM CURRENT_TIMESTAMP)::bigint;
    v_places_id_sequence text;
    v_inserted_structures integer;
    v_inserted_places integer;
BEGIN
    IF NULLIF(BTRIM(v_database_username), '') IS NULL THEN
        RAISE EXCEPTION 'v_database_username must be filled.';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM pg_roles
        WHERE rolname = v_database_username
    ) THEN
        RAISE EXCEPTION 'Database role % does not exist.', v_database_username;
    END IF;

    IF NULLIF(BTRIM(v_marketing_addr), '') IS NULL THEN
        RAISE EXCEPTION 'v_marketing_addr must be filled.';
    END IF;

    IF NULLIF(BTRIM(v_owner_profile_addr), '') IS NULL
        OR NULLIF(BTRIM(v_owner_profile_login), '') IS NULL
    THEN
        RAISE EXCEPTION 'Owner profile address and login must be filled.';
    END IF;

    IF NOT (
        has_table_privilege(
            v_database_username,
            'public.referal_program',
            'SELECT')
        AND has_table_privilege(
            v_database_username,
            'public.referal_program',
            'INSERT')
    ) THEN
        RAISE EXCEPTION
            'Role % needs SELECT and INSERT on public.referal_program.',
            v_database_username;
    END IF;

    IF NOT (
        has_table_privilege(
            v_database_username,
            'public.structures',
            'SELECT')
        AND has_table_privilege(
            v_database_username,
            'public.structures',
            'INSERT')
        AND has_table_privilege(
            v_database_username,
            'public.structures',
            'UPDATE')
    ) THEN
        RAISE EXCEPTION
            'Role % needs SELECT, INSERT, and UPDATE on public.structures.',
            v_database_username;
    END IF;

    IF NOT (
        has_table_privilege(
            v_database_username,
            'public.places',
            'SELECT')
        AND has_table_privilege(
            v_database_username,
            'public.places',
            'INSERT')
        AND has_table_privilege(
            v_database_username,
            'public.places',
            'UPDATE')
        AND has_table_privilege(
            v_database_username,
            'public.places',
            'DELETE')
    ) THEN
        RAISE EXCEPTION
            'Role % needs SELECT, INSERT, UPDATE, and DELETE on public.places.',
            v_database_username;
    END IF;

    v_places_id_sequence := pg_get_serial_sequence('public.places', 'id');
    IF v_places_id_sequence IS NOT NULL
        AND NOT (
            has_sequence_privilege(
                v_database_username,
                v_places_id_sequence,
                'USAGE')
            AND has_sequence_privilege(
                v_database_username,
                v_places_id_sequence,
                'SELECT')
        )
    THEN
        RAISE EXCEPTION
            'Role % needs USAGE and SELECT on sequence %.',
            v_database_username,
            v_places_id_sequence;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM public.referal_program
        WHERE marketing_addr = v_marketing_addr
    ) THEN
        RAISE EXCEPTION
            'Referral program % already exists.',
            v_marketing_addr;
    END IF;

    INSERT INTO public.referal_program (marketing_addr)
    VALUES (v_marketing_addr);

    FOR v_structure_number IN 0..8
    LOOP
        INSERT INTO public.structures
        (
            marketing_addr,
            structure_number,
            max_places_per_profile,
            width,
            height,
            display_height,
            prev_required,
            pos_algo
        )
        VALUES
        (
            v_marketing_addr,
            v_structure_number,
            CASE WHEN v_structure_number = 0 THEN 1 ELSE 0 END,
            CASE WHEN v_structure_number = 0 THEN 0 ELSE 2 END,
            1,
            1,
            v_structure_number BETWEEN 2 AND 8,
            v_pos_algo
        );

        INSERT INTO public.places
        (
            parent_id,
            mp,
            pos_group,
            marketing_addr,
            structure_number,
            profile_addr,
            place_number,
            profile_login,
            "index",
            parent_profile_addr,
            parent_profile_login,
            parent_place_number,
            created_at,
            activated_at,
            is_active,
            kind,
            pos,
            filling,
            deep
        )
        VALUES
        (
            NULL,                           -- parent_id
            '00000000',                     -- fixed-width hexadecimal pos 0
            0,                              -- pos_group
            v_marketing_addr,
            v_structure_number,
            v_owner_profile_addr,
            1,                              -- place_number
            v_owner_profile_login,
            v_owner_profile_login || '1',
            NULL,                           -- parent_profile_addr
            NULL,                           -- parent_profile_login
            NULL,                           -- parent_place_number
            v_created_at,
            v_created_at,
            true,                           -- is_active
            0,                              -- kind
            0,                              -- pos
            0,                              -- filling
            1                               -- deep
        );
    END LOOP;

    SELECT COUNT(*)
    INTO v_inserted_structures
    FROM public.structures
    WHERE marketing_addr = v_marketing_addr;

    SELECT COUNT(*)
    INTO v_inserted_places
    FROM public.places
    WHERE marketing_addr = v_marketing_addr;

    IF v_inserted_structures <> 9 OR v_inserted_places <> 9 THEN
        RAISE EXCEPTION
            'Flash Matrix verification failed: expected 9 structures and 9 places, found % structures and % places.',
            v_inserted_structures,
            v_inserted_places;
    END IF;
END;
$$;

COMMIT;
