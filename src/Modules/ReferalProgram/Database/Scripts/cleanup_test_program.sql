-- Removes all database entities that can be safely attributed to one test
-- referral program. Set the marketing address once below. The same value is
-- used for deletion and verification.
--
-- Run while connected to the correct programs database as the table owner
-- or as a role with DELETE permission on all affected tables.
--
-- Requires migration 018_scope_processed_tasks_by_marketing.sql so processed
-- tasks can be scoped safely by marketing_addr.

BEGIN;

CREATE TEMPORARY TABLE cleanup_test_program_target
(
    marketing_addr text PRIMARY KEY
)
ON COMMIT DROP;

-- Fill with the address of Test CryptoCash, Test CEO, or another test program.
INSERT INTO cleanup_test_program_target (marketing_addr)
VALUES ('');

DO $$
DECLARE
    v_marketing_addr text;
    v_deleted_tasks bigint;
    v_deleted_locks bigint;
    v_deleted_places bigint;
    v_deleted_structures bigint;
    v_deleted_programs bigint;
BEGIN
    SELECT marketing_addr
    INTO STRICT v_marketing_addr
    FROM cleanup_test_program_target;

    IF NULLIF(BTRIM(v_marketing_addr), '') IS NULL THEN
        RAISE EXCEPTION 'The test program marketing address must be filled.';
    END IF;

    IF NOT EXISTS
    (
        SELECT 1
        FROM public.referal_program
        WHERE marketing_addr = v_marketing_addr
    )
    THEN
        RAISE EXCEPTION
            'Referral program % was not found. Nothing was deleted.',
            v_marketing_addr;
    END IF;

    DELETE FROM public.marketing_tasks
    WHERE marketing_addr = v_marketing_addr;
    GET DIAGNOSTICS v_deleted_tasks = ROW_COUNT;

    DELETE FROM public.locks
    WHERE marketing_addr = v_marketing_addr;
    GET DIAGNOSTICS v_deleted_locks = ROW_COUNT;

    -- A single statement removes the complete self-referencing place tree.
    DELETE FROM public.places
    WHERE marketing_addr = v_marketing_addr;
    GET DIAGNOSTICS v_deleted_places = ROW_COUNT;

    DELETE FROM public.structures
    WHERE marketing_addr = v_marketing_addr;
    GET DIAGNOSTICS v_deleted_structures = ROW_COUNT;

    -- Removing this row allows the corresponding setup script to run again.
    DELETE FROM public.referal_program
    WHERE marketing_addr = v_marketing_addr;
    GET DIAGNOSTICS v_deleted_programs = ROW_COUNT;

    RAISE NOTICE
        'Deleted marketing=%, programs=%, structures=%, places=%, locks=%, processed_tasks=%',
        v_marketing_addr,
        v_deleted_programs,
        v_deleted_structures,
        v_deleted_places,
        v_deleted_locks,
        v_deleted_tasks;
END;
$$;

-- Verification uses exactly the same address as the deletion block.
-- Every remaining count must be zero.
SELECT
    target.marketing_addr,
    result.entity,
    result.remaining
FROM cleanup_test_program_target AS target
CROSS JOIN LATERAL
(
    SELECT 'referal_program' AS entity, COUNT(*) AS remaining
    FROM public.referal_program
    WHERE marketing_addr = target.marketing_addr

    UNION ALL

    SELECT 'structures', COUNT(*)
    FROM public.structures
    WHERE marketing_addr = target.marketing_addr

    UNION ALL

    SELECT 'places', COUNT(*)
    FROM public.places
    WHERE marketing_addr = target.marketing_addr

    UNION ALL

    SELECT 'locks', COUNT(*)
    FROM public.locks
    WHERE marketing_addr = target.marketing_addr

    UNION ALL

    SELECT 'marketing_tasks', COUNT(*)
    FROM public.marketing_tasks
    WHERE marketing_addr = target.marketing_addr
) AS result;

COMMIT;
