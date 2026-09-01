-- Creates the test CryptoCash referral program, structures 0 through 4, and
-- the first place in every structure as one atomic operation.
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
        'EQCFZmVrYR-tLGIWDHjBb-Oyk1tcePk2_ThcytEZA08dNLbO';
    v_owner_profile_addr text :=
        'EQCtDUCESpq8P0J2qRrrcobr458y0C_Jd6hyGetvIVoB52zN';
    v_owner_profile_login text := 'cryptocash';

    v_structures jsonb :=
    '[
        {
            "structure_number": 0,
            "max_places_per_profile": 1,
            "width": 0,
            "height": 1,
            "display_height": 1,
            "prev_required": false,
            "pos_algo": {
                "v": 1,
                "root": "profile",
                "relation": "relative",
                "groups": [
                    { "id": 0, "algo": "classic", "weight": 1 }
                ]
            }
        },
        {
            "structure_number": 1,
            "max_places_per_profile": 1,
            "width": 2,
            "height": 0,
            "display_height": 2,
            "prev_required": false,
            "activity": { "set_active_on_activation": true },
            "pos_algo": {
                "v": 1,
                "root": "owner",
                "relation": "relative",
                "groups": [
                    {
                        "id": 1,
                        "algo": "chess",
                        "weight": 5,
                        "profiled_places_prioritized": true,
                        "depth_spread": 1
                    },
                    {
                        "id": 2,
                        "algo": "radar",
                        "weight": 5,
                        "profiled_places_prioritized": true,
                        "depth_spread": 3
                    }
                ]
            }
        },
        {
            "structure_number": 2,
            "max_places_per_profile": 1,
            "width": 2,
            "height": 0,
            "display_height": 2,
            "prev_required": false,
            "activity": { "set_active_on_activation": true },
            "pos_algo": {
                "v": 1,
                "root": "owner",
                "relation": "relative",
                "groups": [
                    {
                        "id": 1,
                        "algo": "chess",
                        "weight": 5,
                        "profiled_places_prioritized": true,
                        "depth_spread": 1
                    },
                    {
                        "id": 2,
                        "algo": "radar",
                        "weight": 5,
                        "profiled_places_prioritized": true,
                        "depth_spread": 3
                    }
                ]
            }
        },
        {
            "structure_number": 3,
            "max_places_per_profile": 1,
            "width": 2,
            "height": 0,
            "display_height": 2,
            "prev_required": false,
            "activity": { "set_active_on_activation": true },
            "pos_algo": {
                "v": 1,
                "root": "owner",
                "relation": "relative",
                "groups": [
                    {
                        "id": 1,
                        "algo": "chess",
                        "weight": 5,
                        "profiled_places_prioritized": true,
                        "depth_spread": 1
                    },
                    {
                        "id": 2,
                        "algo": "radar",
                        "weight": 5,
                        "profiled_places_prioritized": true,
                        "depth_spread": 3
                    }
                ]
            }
        },
        {
            "structure_number": 4,
            "max_places_per_profile": 1,
            "width": 2,
            "height": 0,
            "display_height": 2,
            "prev_required": false,
            "activity": { "set_active_on_activation": true },
            "pos_algo": {
                "v": 1,
                "root": "owner",
                "relation": "relative",
                "groups": [
                    {
                        "id": 1,
                        "algo": "chess",
                        "weight": 5,
                        "profiled_places_prioritized": true,
                        "depth_spread": 1
                    },
                    {
                        "id": 2,
                        "algo": "radar",
                        "weight": 5,
                        "profiled_places_prioritized": true,
                        "depth_spread": 3
                    }
                ]
            }
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
            pos_algo               jsonb,
            activity               jsonb
        )
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
            pos_algo,
            activity
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
            v_structure.pos_algo,
            v_structure.activity
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
            NULL,
            '00000000',
            0,
            v_marketing_addr,
            v_structure.structure_number,
            v_owner_profile_addr,
            1,
            v_owner_profile_login,
            v_owner_profile_login || '1',
            NULL,
            NULL,
            NULL,
            v_created_at,
            v_created_at,
            true,
            0,
            0,
            0,
            1
        );
    END LOOP;
END;
$$;

COMMIT;
