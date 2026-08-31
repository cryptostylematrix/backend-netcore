-- Configures bounded profiled-frontier placement for one existing structure.
--
-- Run the whole file in pgAdmin Query Tool while connected to the Programs
-- database as the table owner (or a role with UPDATE on public.structures).
-- Edit only the three variables immediately below.
--
-- The structure's complete pos_algo value is replaced. Profile purchases use
-- owner-root profile_frontier positioning, while system purchases use
-- owner-root system_gap positioning.

BEGIN;

DO $$
DECLARE
    -- Required inputs.
    v_marketing_addr text := '';
    v_structure_number integer := 0;
    v_profiled_width_limit integer := 35;

    v_new_pos_algo jsonb;
    v_updated_rows integer;
BEGIN
    IF NULLIF(BTRIM(v_marketing_addr), '') IS NULL THEN
        RAISE EXCEPTION 'v_marketing_addr must be filled.';
    END IF;

    IF v_structure_number NOT BETWEEN 0 AND 255 THEN
        RAISE EXCEPTION
            'v_structure_number must be between 0 and 255; received %.',
            v_structure_number;
    END IF;

    IF v_profiled_width_limit <= 0 THEN
        RAISE EXCEPTION
            'v_profiled_width_limit must be positive; received %.',
            v_profiled_width_limit;
    END IF;

    PERFORM 1
    FROM public.structures structure
    WHERE structure.marketing_addr = v_marketing_addr
      AND structure.structure_number = v_structure_number
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION
            'Structure % for marketing % does not exist.',
            v_structure_number,
            v_marketing_addr;
    END IF;

    v_new_pos_algo := jsonb_build_object(
        'v', 2,
        'default', jsonb_build_object(
            'root', 'owner',
            'relation', 'relative',
            'groups', jsonb_build_array(
                jsonb_build_object(
                    'id', 0,
                    'algo', 'profile_frontier',
                    'weight', 1,
                    'profiled_frontier_limit', v_profiled_width_limit))),
        'operations', jsonb_build_object(
            'buy_system_place', jsonb_build_object(
                'root', 'owner',
                'relation', 'relative',
                'groups', jsonb_build_array(
                    jsonb_build_object(
                        'id', 0,
                        'algo', 'system_gap',
                        'weight', 1)))));

    UPDATE public.structures
    SET pos_algo = v_new_pos_algo
    WHERE marketing_addr = v_marketing_addr
      AND structure_number = v_structure_number;

    GET DIAGNOSTICS v_updated_rows = ROW_COUNT;
    IF v_updated_rows <> 1 THEN
        RAISE EXCEPTION
            'Expected to update one structure, updated %.',
            v_updated_rows;
    END IF;

    RAISE NOTICE
        'Configured profile frontier limit % for marketing %, structure %. New pos_algo: %',
        v_profiled_width_limit,
        v_marketing_addr,
        v_structure_number,
        jsonb_pretty(v_new_pos_algo);
END;
$$;

COMMIT;
