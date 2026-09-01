-- Creates the test Multi referral program, structures 0 through 6, and the
-- first place in every structure as one atomic operation.
--
-- Set v_database_username for the target environment:
--   development: dev_cs_programs_user
--   production:  cs_programs_user
--
-- This script is intended for first-time initialization and is not idempotent.
-- Run it while connected to the correct programs database as its table owner.

BEGIN;

DO $$
DECLARE
    v_database_username text := '';
    v_marketing_addr text :=
        'EQCayDLxd-C3iOB051M5FUHx565Xyjt4vwArt2VPYkIOlbQ5';
    v_owner_profile_addr text :=
        'EQCzVLbzpG8Ev3ar55Jc051KARuHGRZtpW_1MbUS8cZoZppq';
    v_owner_profile_login text := 'admin';

    v_structures jsonb :=
    '[
        {
            "structure_number": 0,
            "max_places_per_profile": 1,
            "width": 0,
            "height": 1,
            "display_height": 1,
            "prev_required": false,
            "pos_algo": {"v":1,"root":"profile","groups":[{"id":0,"algo":"classic","weight":1}],"relation":"relative"}
        },
		 {
            "structure_number": 1,
            "max_places_per_profile": 0,
            "width": 2,
            "height": 2,
            "display_height": 2,
            "prev_required": false,
            "pos_algo": {"v":1,"root":"profile","groups":[{"id":0,"algo":"classic","weight":1}],"relation":"relative"}
        },
	  	{
            "structure_number": 2,
            "max_places_per_profile": 0,
            "width": 2,
            "height": 2,
            "display_height": 2,
            "prev_required": true,
            "pos_algo": {"v":1,"root":"profile","groups":[{"id":0,"algo":"classic","weight":1}],"relation":"relative"}
        },
	  	{
            "structure_number": 3,
            "max_places_per_profile": 0,
            "width": 2,
            "height": 2,
            "display_height": 2,
            "prev_required": true,
            "pos_algo": {"v":1,"root":"profile","groups":[{"id":0,"algo":"classic","weight":1}],"relation":"relative"}
        },
	  	{
            "structure_number": 4,
            "max_places_per_profile": 0,
            "width": 2,
            "height": 2,
            "display_height": 2,
            "prev_required": true,
            "pos_algo": {"v":1,"root":"profile","groups":[{"id":0,"algo":"classic","weight":1}],"relation":"relative"}
        },
		{
            "structure_number": 5,
            "max_places_per_profile": 0,
            "width": 2,
            "height": 2,
            "display_height": 2,
            "prev_required": true,
            "pos_algo": {"v":1,"root":"profile","groups":[{"id":0,"algo":"classic","weight":1}],"relation":"relative"}
        },
		{
            "structure_number": 6,
            "max_places_per_profile": 0,
            "width": 2,
            "height": 2,
            "display_height": 2,
            "prev_required": true,
            "pos_algo": {"v":1,"root":"profile","groups":[{"id":0,"algo":"classic","weight":1}],"relation":"relative"}
        }
    ]'::jsonb;

    v_structure record;
    v_created_at bigint := EXTRACT(EPOCH FROM CURRENT_TIMESTAMP)::bigint;
    v_places_id_sequence text;
BEGIN
    IF NULLIF(BTRIM(v_database_username), '') IS NULL THEN
        RAISE EXCEPTION 'v_database_username must be filled.';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = v_database_username) THEN
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
        has_table_privilege(v_database_username, 'public.referal_program', 'SELECT')
        AND has_table_privilege(v_database_username, 'public.referal_program', 'INSERT')
    )
    THEN
        RAISE EXCEPTION
            'Role % needs SELECT and INSERT on public.referal_program.',
            v_database_username;
    END IF;

    IF NOT (
        has_table_privilege(v_database_username, 'public.structures', 'SELECT')
        AND has_table_privilege(v_database_username, 'public.structures', 'INSERT')
        AND has_table_privilege(v_database_username, 'public.structures', 'UPDATE')
    )
    THEN
        RAISE EXCEPTION
            'Role % needs SELECT, INSERT, and UPDATE on public.structures.',
            v_database_username;
    END IF;

    IF NOT (
        has_table_privilege(v_database_username, 'public.places', 'SELECT')
        AND has_table_privilege(v_database_username, 'public.places', 'INSERT')
        AND has_table_privilege(v_database_username, 'public.places', 'UPDATE')
        AND has_table_privilege(v_database_username, 'public.places', 'DELETE')
    )
    THEN
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

    IF jsonb_typeof(v_structures) <> 'array'
        OR jsonb_array_length(v_structures) = 0
    THEN
        RAISE EXCEPTION 'v_structures must contain at least one structure.';
    END IF;

    INSERT INTO public.referal_program (marketing_addr)
    VALUES (v_marketing_addr);

    FOR v_structure IN
        SELECT *
        FROM jsonb_to_recordset(v_structures) AS structure
        (
            structure_number       smallint,
            max_places_per_profile integer,
            width                  smallint,
            height                 smallint,
            display_height         smallint,
            prev_required          boolean,
            pos_algo               jsonb
        )
    LOOP
        IF v_structure.structure_number IS NULL
            OR v_structure.max_places_per_profile IS NULL
            OR v_structure.width IS NULL
            OR v_structure.height IS NULL
            OR v_structure.display_height IS NULL
            OR v_structure.prev_required IS NULL
            OR v_structure.pos_algo IS NULL
            OR v_structure.pos_algo = 'null'::jsonb
        THEN
            RAISE EXCEPTION 'Every structure configuration field must be filled.';
        END IF;

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
            v_structure.structure_number,
            v_structure.max_places_per_profile,
            v_structure.width,
            v_structure.height,
            v_structure.display_height,
            v_structure.prev_required,
            v_structure.pos_algo
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
            NULL,                    -- parent_id
            '00000000',              -- mp: fixed-width hexadecimal pos 0
            0,                       -- pos_group
            v_marketing_addr,
            v_structure.structure_number,
            v_owner_profile_addr,
            1,                       -- place_number
            v_owner_profile_login,
            v_owner_profile_login || '1',
            NULL,                    -- parent_profile_addr
            NULL,                    -- parent_profile_login
            NULL,                    -- parent_place_number
            v_created_at,
            v_created_at,
            true,                    -- is_active
            0,                       -- kind
            0,                       -- pos
            0,                       -- filling
            1                        -- deep
        );
    END LOOP;
END;
$$;

COMMIT;
