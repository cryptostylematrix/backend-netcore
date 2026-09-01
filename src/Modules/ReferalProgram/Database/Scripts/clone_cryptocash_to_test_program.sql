-- Clones the complete off-chain CryptoCash tree to the Test CryptoCash
-- marketing address. Run the whole file in pgAdmin Query Tool while connected
-- to the Programs database as postgres (or another table owner).
--
-- Stop the Programs task processor and other writers before running this file.
-- The target program must not already exist. Locks and marketing_tasks are
-- intentionally not copied because they describe in-flight/on-chain work for
-- the source contract, not reusable tree state.

BEGIN;

DO $$
DECLARE
    v_source_marketing_addr text :=
        'EQAba1dNyAbxm4t_dv5T1ARQXaQAAYcfJ4jcAWcw1PQ7q10b';
    v_target_marketing_addr text :=
        'EQCFZmVrYR-tLGIWDHjBb-Oyk1tcePk2_ThcytEZA08dNLbO';

    -- Keep disabled until the copied data and Test CryptoCash contract have
    -- been checked. Enable it explicitly after verification if required.
    v_enable_task_processing boolean := false;

    v_places_id_sequence text;
    v_source_structure_count integer;
    v_source_place_count integer;
    v_source_rank_count integer;
    v_source_volume_count integer;
    v_target_structure_count integer;
    v_target_place_count integer;
    v_target_rank_count integer;
    v_target_volume_count integer;
BEGIN
    IF NULLIF(BTRIM(v_source_marketing_addr), '') IS NULL
        OR NULLIF(BTRIM(v_target_marketing_addr), '') IS NULL
    THEN
        RAISE EXCEPTION 'Source and target marketing addresses must be filled.';
    END IF;

    IF v_source_marketing_addr = v_target_marketing_addr THEN
        RAISE EXCEPTION 'Source and target marketing addresses must differ.';
    END IF;

    IF NOT EXISTS
    (
        SELECT 1
        FROM public.referal_program
        WHERE marketing_addr = v_source_marketing_addr
    )
    THEN
        RAISE EXCEPTION
            'Source CryptoCash program % does not exist.',
            v_source_marketing_addr;
    END IF;

    IF EXISTS
    (
        SELECT 1
        FROM public.referal_program
        WHERE marketing_addr = v_target_marketing_addr
    )
    THEN
        RAISE EXCEPTION
            'Target CryptoCash Test program % already exists. No rows were copied.',
            v_target_marketing_addr;
    END IF;

    v_places_id_sequence := pg_get_serial_sequence('public.places', 'id');
    IF v_places_id_sequence IS NULL THEN
        RAISE EXCEPTION 'Could not resolve the public.places id sequence.';
    END IF;

    -- Keep the source snapshot internally consistent while IDs and parent IDs
    -- are remapped. These locks are released automatically at COMMIT/ROLLBACK.
    LOCK TABLE public.referal_program IN SHARE MODE;
    LOCK TABLE public.structures IN SHARE MODE;
    LOCK TABLE public.structure_ranks IN SHARE MODE;
    LOCK TABLE public.profile_volumes IN SHARE MODE;
    LOCK TABLE public.places IN SHARE MODE;

    SELECT COUNT(*)
    INTO v_source_structure_count
    FROM public.structures
    WHERE marketing_addr = v_source_marketing_addr;

    SELECT COUNT(*)
    INTO v_source_place_count
    FROM public.places
    WHERE marketing_addr = v_source_marketing_addr;

    SELECT COUNT(*)
    INTO v_source_rank_count
    FROM public.structure_ranks
    WHERE marketing_addr = v_source_marketing_addr;

    SELECT COUNT(*)
    INTO v_source_volume_count
    FROM public.profile_volumes
    WHERE marketing_addr = v_source_marketing_addr;

    IF v_source_structure_count = 0 OR v_source_place_count = 0 THEN
        RAISE EXCEPTION
            'Source CryptoCash is incomplete: % structures and % places.',
            v_source_structure_count,
            v_source_place_count;
    END IF;

    INSERT INTO public.referal_program
    (
        marketing_addr,
        is_task_processing_enabled
    )
    VALUES
    (
        v_target_marketing_addr,
        v_enable_task_processing
    );

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
    SELECT
        v_target_marketing_addr,
        structure_number,
        max_places_per_profile,
        width,
        height,
        display_height,
        prev_required,
        pos_algo,
        activity
    FROM public.structures
    WHERE marketing_addr = v_source_marketing_addr;

    INSERT INTO public.profile_volumes
    (
        marketing_addr,
        structure_number,
        profile_addr,
        personal_volume,
        referral_volume,
        group_volume
    )
    SELECT
        v_target_marketing_addr,
        structure_number,
        profile_addr,
        personal_volume,
        referral_volume,
        group_volume
    FROM public.profile_volumes
    WHERE marketing_addr = v_source_marketing_addr;

    INSERT INTO public.structure_ranks
    (
        marketing_addr,
        structure_number,
        required_active_referral_places,
        name
    )
    SELECT
        v_target_marketing_addr,
        structure_number,
        required_active_referral_places,
        name
    FROM public.structure_ranks
    WHERE marketing_addr = v_source_marketing_addr;

    CREATE TEMP TABLE cryptocash_place_id_map
    (
        source_id integer PRIMARY KEY,
        target_id integer UNIQUE NOT NULL
    ) ON COMMIT DROP;

    INSERT INTO cryptocash_place_id_map (source_id, target_id)
    SELECT
        id,
        nextval(v_places_id_sequence::regclass)::integer
    FROM public.places
    WHERE marketing_addr = v_source_marketing_addr
    ORDER BY id;

    IF EXISTS
    (
        SELECT 1
        FROM public.places source_place
        LEFT JOIN cryptocash_place_id_map parent_map
            ON parent_map.source_id = source_place.parent_id
        WHERE source_place.marketing_addr = v_source_marketing_addr
          AND source_place.parent_id IS NOT NULL
          AND parent_map.source_id IS NULL
    )
    THEN
        RAISE EXCEPTION
            'A source CryptoCash place references a parent outside the source program.';
    END IF;

    INSERT INTO public.places
    (
        id,
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
        matrix_filling
    )
    SELECT
        place_map.target_id,
        parent_map.target_id,
        source_place.mp,
        source_place.pos_group,
        v_target_marketing_addr,
        source_place.structure_number,
        source_place.profile_addr,
        source_place.place_number,
        source_place.profile_login,
        source_place."index",
        source_place.parent_profile_addr,
        source_place.parent_profile_login,
        source_place.parent_place_number,
        source_place.created_at,
        source_place.activated_at,
        source_place.is_active,
        source_place.kind,
        source_place.pos,
        source_place.filling,
        source_place.deep,
        source_place.matrix_filling
    FROM public.places source_place
    JOIN cryptocash_place_id_map place_map
        ON place_map.source_id = source_place.id
    LEFT JOIN cryptocash_place_id_map parent_map
        ON parent_map.source_id = source_place.parent_id
    WHERE source_place.marketing_addr = v_source_marketing_addr
    ORDER BY source_place.deep, source_place.id;

    SELECT COUNT(*)
    INTO v_target_structure_count
    FROM public.structures
    WHERE marketing_addr = v_target_marketing_addr;

    SELECT COUNT(*)
    INTO v_target_place_count
    FROM public.places
    WHERE marketing_addr = v_target_marketing_addr;

    SELECT COUNT(*)
    INTO v_target_rank_count
    FROM public.structure_ranks
    WHERE marketing_addr = v_target_marketing_addr;

    SELECT COUNT(*)
    INTO v_target_volume_count
    FROM public.profile_volumes
    WHERE marketing_addr = v_target_marketing_addr;

    IF v_target_structure_count <> v_source_structure_count
        OR v_target_place_count <> v_source_place_count
        OR v_target_rank_count <> v_source_rank_count
        OR v_target_volume_count <> v_source_volume_count
    THEN
        RAISE EXCEPTION
            'Clone verification failed. Source/target counts: structures %/%, places %/%, ranks %/%, volumes %/%.',
            v_source_structure_count,
            v_target_structure_count,
            v_source_place_count,
            v_target_place_count,
            v_source_rank_count,
            v_target_rank_count,
            v_source_volume_count,
            v_target_volume_count;
    END IF;

    RAISE NOTICE
        'CryptoCash clone complete: % structures, % places, % ranks, % profile volumes. Task processing enabled: %.',
        v_target_structure_count,
        v_target_place_count,
        v_target_rank_count,
        v_target_volume_count,
        v_enable_task_processing;
END;
$$;

COMMIT;

-- pgAdmin displays this verification result after the transaction commits.
SELECT
    rp.marketing_addr,
    rp.is_task_processing_enabled,
    (SELECT COUNT(*) FROM public.structures s
        WHERE s.marketing_addr = rp.marketing_addr) AS structures,
    (SELECT COUNT(*) FROM public.places p
        WHERE p.marketing_addr = rp.marketing_addr) AS places,
    (SELECT COUNT(*) FROM public.structure_ranks sr
        WHERE sr.marketing_addr = rp.marketing_addr) AS ranks,
    (SELECT COUNT(*) FROM public.profile_volumes pv
        WHERE pv.marketing_addr = rp.marketing_addr) AS profile_volumes,
    (SELECT COUNT(*) FROM public.locks l
        WHERE l.marketing_addr = rp.marketing_addr) AS locks,
    (SELECT COUNT(*) FROM public.marketing_tasks mt
        WHERE mt.marketing_addr = rp.marketing_addr) AS marketing_tasks
FROM public.referal_program rp
WHERE rp.marketing_addr =
    'EQCFZmVrYR-tLGIWDHjBb-Oyk1tcePk2_ThcytEZA08dNLbO';
