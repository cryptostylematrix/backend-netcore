-- Creates a referral program, all of its structures, and the first place in
-- every structure as one atomic operation.
--
-- Run while connected to cs_programs as the table owner.
-- Fill v_marketing_addr and add one object to v_structures for every structure.
-- For a system/non-profile top place, keep both profile_addr and profile_login null.

BEGIN;

DO $$
DECLARE
    v_marketing_addr text := '';

    v_structures jsonb :=
    '[
        {
            "structure_number": null,
            "max_places_per_profile": null,
            "width": null,
            "height": null,
            "display_height": null,
            "prev_required": null,
            "pos_algo": null,
            "profile_addr": null,
            "profile_login": null
        }
    ]'::jsonb;

    v_structure record;
    v_created_at bigint := EXTRACT(EPOCH FROM CURRENT_TIMESTAMP)::bigint;
    v_profile_addr text;
    v_profile_login text;
    v_place_index text;
BEGIN
    IF NULLIF(BTRIM(v_marketing_addr), '') IS NULL THEN
        RAISE EXCEPTION 'v_marketing_addr must be filled.';
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
            pos_algo               jsonb,
            profile_addr           text,
            profile_login          text
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

        v_profile_addr := NULLIF(BTRIM(v_structure.profile_addr), '');
        v_profile_login := NULLIF(BTRIM(v_structure.profile_login), '');

        IF (v_profile_addr IS NULL) <> (v_profile_login IS NULL) THEN
            RAISE EXCEPTION
                'profile_addr and profile_login must either both be set or both be null for structure %.',
                v_structure.structure_number;
        END IF;

        v_place_index := COALESCE(v_profile_login, 'system') || '1';

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
            deep,
            personal_volume,
            group_volume
        )
        VALUES
        (
            NULL,                    -- parent_id
            '00000000',              -- mp: fixed-width hexadecimal pos 0
            0,                       -- pos_group
            v_marketing_addr,
            v_structure.structure_number,
            v_profile_addr,
            1,                       -- place_number
            v_profile_login,
            v_place_index,
            NULL,                    -- parent_profile_addr
            NULL,                    -- parent_profile_login
            NULL,                    -- parent_place_number
            v_created_at,
            v_created_at,
            true,                    -- is_active
            0,                       -- kind
            0,                       -- pos
            0,                       -- filling
            1,                       -- deep
            0,                       -- personal_volume
            0                       -- group_volume
        );
    END LOOP;
END;
$$;

COMMIT;
