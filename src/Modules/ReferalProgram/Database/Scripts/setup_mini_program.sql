-- Creates the MINI referral program, structures 0 through 17, and the first
-- place in every structure as one atomic operation.
--
-- Fill the four environment-specific variables below before running:
--   v_database_username
--   v_marketing_addr
--   v_owner_profile_addr
--   v_owner_profile_login
--
-- v_cut_factor defaults to 2. For create_clone and create_reinvest, every Nth
-- direct clone child of the selected parent becomes a terminal clone (kind 2).
-- Terminal clones cannot have children. The minimum supported factor is 2.
--
-- Suggested database users:
--   development: dev_cs_programs_user
--   production:  cs_programs_user
--
-- MINI structure groups and purchase prerequisites:
--   1 -> 2 -> 3
--   4 -> 5
--   6 -> 7
--   8 -> 9
--   10 -> 11
--   12 -> 13
--   14 -> 15
--   16 -> 17
-- The first structure of every group has no previous-structure requirement.
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
    v_cut_factor integer := 2;

    v_classic_config jsonb;
    v_trimmed_classic_config jsonb;
    v_pos_algo jsonb;
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

    IF v_cut_factor < 2 THEN
        RAISE EXCEPTION 'v_cut_factor must be at least 2.';
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

    v_classic_config := jsonb_build_object(
        'root', 'profile',
        'relation', 'relative',
        'groups', jsonb_build_array(
            jsonb_build_object(
                'id', 0,
                'algo', 'classic',
                'weight', 1)));

    v_trimmed_classic_config := jsonb_build_object(
        'root', 'profile',
        'relation', 'relative',
        'groups', jsonb_build_array(
            jsonb_build_object(
                'id', 0,
                'algo', 'trimmed_classic',
                'weight', 1,
                'cut_factor', v_cut_factor)));

    v_pos_algo := jsonb_build_object(
        'v', 2,
        'default', v_classic_config,
        'operations', jsonb_build_object(
            'create_clone', v_trimmed_classic_config,
            'create_reinvest', v_trimmed_classic_config));

    INSERT INTO public.referal_program (marketing_addr)
    VALUES (v_marketing_addr);

    FOR v_structure_number IN 0..17
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
            v_structure_number IN (2, 3, 5, 7, 9, 11, 13, 15, 17),
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
            deep,
            personal_volume,
            group_volume,
            task_key,
            task_query_id,
            task_source_addr
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
            0,                              -- purchased/top place kind
            0,                              -- pos
            0,                              -- filling
            1,                              -- deep
            0,                              -- personal_volume
            0,                              -- group_volume
            0,                              -- task_key
            0,                              -- task_query_id
            NULL                            -- task_source_addr
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

    IF v_inserted_structures <> 18 OR v_inserted_places <> 18 THEN
        RAISE EXCEPTION
            'MINI verification failed: expected 18 structures and 18 places, found % structures and % places.',
            v_inserted_structures,
            v_inserted_places;
    END IF;
END;
$$;

COMMIT;
