-- Adds the recurring Scheduled Tasks used by a CryptoCash Program.
-- Run as the postgres user in Query Tool while connected to the Tasks database.
--
-- Set the marketing address and all four execution variables before execution.
-- Each execution must be the first day of a month at 00:00:00 UTC and anchors
-- that structure's schedule. The structure-1 execution for the 15th is derived
-- automatically from the structure-1 variable.
-- Each execution inserts a new set of tasks; do not rerun it for the same
-- marketing address unless another set is intentional.
--
-- Example:
--     v_marketing_address text := 'EQ_REPLACE_WITH_MARKETING_ADDRESS';
--     v_structure_1_first_execution_at_utc timestamp with time zone :=
--         '2026-09-01 00:00:00+00';
--     v_structure_2_first_execution_at_utc timestamp with time zone :=
--         '2026-10-01 00:00:00+00';
--     v_structure_3_first_execution_at_utc timestamp with time zone :=
--         '2026-12-01 00:00:00+00';
--     v_structure_4_first_execution_at_utc timestamp with time zone :=
--         '2027-03-01 00:00:00+00';

DO $$
DECLARE
    v_marketing_address text := '';
    v_structure_1_first_execution_at_utc timestamp with time zone := NULL;
    v_structure_2_first_execution_at_utc timestamp with time zone := NULL;
    v_structure_3_first_execution_at_utc timestamp with time zone := NULL;
    v_structure_4_first_execution_at_utc timestamp with time zone := NULL;
    v_structure_number integer;
    v_structure_first_execution_at_utc timestamp with time zone;
    v_structure_first_execution_utc_without_zone timestamp;
BEGIN
    IF btrim(v_marketing_address) = '' THEN
        RAISE EXCEPTION
            'Set v_marketing_address before running this script.';
    END IF;

    FOR v_structure_number, v_structure_first_execution_at_utc IN
        SELECT *
        FROM
        (
            VALUES
                (1, v_structure_1_first_execution_at_utc),
                (2, v_structure_2_first_execution_at_utc),
                (3, v_structure_3_first_execution_at_utc),
                (4, v_structure_4_first_execution_at_utc)
        ) AS structure_execution(structure_number, first_execution_at_utc)
    LOOP
        IF v_structure_first_execution_at_utc IS NULL THEN
            RAISE EXCEPTION
                'Set the first execution date for structure % before running this script.',
                v_structure_number;
        END IF;

        v_structure_first_execution_utc_without_zone :=
            v_structure_first_execution_at_utc AT TIME ZONE 'UTC';

        IF extract(day FROM v_structure_first_execution_utc_without_zone) <> 1
           OR v_structure_first_execution_utc_without_zone::time <> time '00:00:00' THEN
            RAISE EXCEPTION
                'The first execution for structure % must be the first day of a month at 00:00:00 UTC. Received: %',
                v_structure_number,
                v_structure_first_execution_at_utc;
        END IF;
    END LOOP;

    INSERT INTO public.tasks
    (
        id,
        execute_at_utc,
        schedule,
        commands
    )
    SELECT
        gen_random_uuid(),
        task_configuration.first_execution_at_utc,
        jsonb_build_object(
            'type', 'calendar',
            'unit', 'months',
            'interval', task_configuration.month_interval,
            'dayOfMonth', task_configuration.day_of_month,
            'timeUtc', '00:00:00'),
        jsonb_build_array(
            jsonb_build_object(
                'module', 'program',
                'type', 'program.task-processing.disable',
                'version', 1,
                'target', jsonb_build_object(
                    'marketingAddress', v_marketing_address),
                'arguments', '{}'::jsonb),
            jsonb_build_object(
                'module', 'program',
                'type', 'program.structure.update-activity',
                'version', 1,
                'target', jsonb_build_object(
                    'marketingAddress', v_marketing_address),
                'arguments', jsonb_build_object(
                    'structureNumber', task_configuration.structure_number)),
            jsonb_build_object(
                'module', 'program',
                'type', 'program.structure.compress',
                'version', 1,
                'target', jsonb_build_object(
                    'marketingAddress', v_marketing_address),
                'arguments', jsonb_build_object(
                    'structureNumber', task_configuration.structure_number)),
            jsonb_build_object(
                'module', 'program',
                'type', 'program.structure.reset-personal-volume',
                'version', 1,
                'target', jsonb_build_object(
                    'marketingAddress', v_marketing_address),
                'arguments', jsonb_build_object(
                    'structureNumber', task_configuration.structure_number)),
            jsonb_build_object(
                'module', 'program',
                'type', 'program.task-processing.enable',
                'version', 1,
                'target', jsonb_build_object(
                    'marketingAddress', v_marketing_address),
                'arguments', '{}'::jsonb))
    FROM
    (
        VALUES
            (1, 1, 1, v_structure_1_first_execution_at_utc),
            (1, 15, 1,
                ((v_structure_1_first_execution_at_utc AT TIME ZONE 'UTC')
                    + interval '14 days')
                    AT TIME ZONE 'UTC'),
            (2, 1, 1, v_structure_2_first_execution_at_utc),
            (3, 1, 3, v_structure_3_first_execution_at_utc),
            (4, 1, 6, v_structure_4_first_execution_at_utc)
    ) AS task_configuration
    (
        structure_number,
        day_of_month,
        month_interval,
        first_execution_at_utc
    );

    RAISE NOTICE
        'Created 5 CryptoCash tasks for marketing address %.',
        v_marketing_address;
END;
$$;
