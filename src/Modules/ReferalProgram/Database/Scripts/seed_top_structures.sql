-- Creates a reusable procedure for inserting the top place of each structure.
-- Run this script while connected to the cs_programs database.

CREATE OR REPLACE PROCEDURE public.seed_top_structures
(
    p_marketing_addr text,
    p_profile_addr text,
    p_profile_login text,
    p_from_structure smallint DEFAULT 0,
    p_to_structure smallint DEFAULT 4,
    p_pos bigint DEFAULT 0
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_structure_number integer;
    v_timestamp bigint :=
        EXTRACT(EPOCH FROM CURRENT_TIMESTAMP)::bigint;
    v_mp text;
BEGIN
    IF p_from_structure < 0
        OR p_to_structure > 255
        OR p_from_structure > p_to_structure
    THEN
        RAISE EXCEPTION 'Invalid structure range: % to %',
            p_from_structure,
            p_to_structure;
    END IF;

    IF p_pos < 0 OR p_pos > 4294967295 THEN
        RAISE EXCEPTION 'Position must be between 0 and 4294967295';
    END IF;

    -- Lowercase, fixed-width 32-bit hexadecimal value.
    v_mp := lpad(to_hex(p_pos), 8, '0');

    FOR v_structure_number
        IN p_from_structure..p_to_structure
    LOOP
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
            v_mp,
            0,
            p_marketing_addr,
            v_structure_number::smallint,
            p_profile_addr,
            1,
            p_profile_login,
            p_profile_login || '1',
            NULL,
            NULL,
            NULL,
            v_timestamp,
            v_timestamp,
            true,
            0,
            p_pos,
            0,
            1
        )
        ON CONFLICT ON CONSTRAINT places_place_unique
        DO NOTHING;
    END LOOP;
END;
$$;

-- Seed structures 0 through 4 for the initial profile.
CALL public.seed_top_structures
(
    p_marketing_addr =>
        'EQCFZmVrYR-tLGIWDHjBb-Oyk1tcePk2_ThcytEZA08dNLbO',

    p_profile_addr =>
        'EQCtDUCESpq8P0J2qRrrcobr458y0C_Jd6hyGetvIVoB52zN',

    p_profile_login => 'cryptocash',
    p_from_structure => 0::smallint,
    p_to_structure   => 4::smallint,
    p_pos            => 0::bigint
);
