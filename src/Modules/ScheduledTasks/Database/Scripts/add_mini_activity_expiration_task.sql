-- Tasks database only. Deploy the enabled expiry handler and configure Mini first.
-- Fill both variables. Requires SELECT/INSERT on tasks and permission to lock it.
-- Expires first places in structure 1 (Mini 10 group root), not invites.
-- Runs daily at the chosen UTC time. Rejects duplicates, including disabled tasks.
BEGIN;
DO $$
DECLARE
    v_marketing_address text := '';
    v_first_execution_at_utc timestamptz := NULL;
    v_commands jsonb;
    v_inserted_rows integer;
BEGIN
    v_marketing_address := NULLIF(btrim(v_marketing_address), '');
    IF v_marketing_address IS NULL OR v_first_execution_at_utc IS NULL THEN
        RAISE EXCEPTION 'Set v_marketing_address and v_first_execution_at_utc (with UTC offset).';
    END IF;
    IF NOT (has_table_privilege(current_user, 'public.tasks', 'SELECT')
        AND has_table_privilege(current_user, 'public.tasks', 'INSERT')) THEN
        RAISE EXCEPTION 'SELECT/INSERT on tasks is required.';
    END IF;
    LOCK TABLE public.tasks IN SHARE ROW EXCLUSIVE MODE;
    IF EXISTS (SELECT 1 FROM public.tasks WHERE commands @> jsonb_build_array(jsonb_build_object(
        'module','program','type','program.structure.deactivate-expired-first-places',
        'target',jsonb_build_object('marketingAddress',v_marketing_address),
        'arguments',jsonb_build_object('structureNumber',1)))) THEN
        RAISE EXCEPTION 'An group-root expiration task already exists for this program. Review it instead of adding a duplicate.';
    END IF;
    IF EXISTS (SELECT 1 FROM public.tasks WHERE commands @> jsonb_build_array(jsonb_build_object(
        'module','program','type','program.structure.deactivate-expired-first-places',
        'target',jsonb_build_object('marketingAddress',v_marketing_address),
        'arguments',jsonb_build_object('structureNumber',0)))) THEN
        RAISE EXCEPTION 'An old Mini invite-expiration task exists. Replace that schedule rather than running both policies.';
    END IF;
    v_commands := jsonb_build_array(jsonb_build_object(
        'module','program','type','program.structure.deactivate-expired-first-places','version',1,
        'target',jsonb_build_object('marketingAddress',v_marketing_address),
        'arguments',jsonb_build_object('structureNumber',1,'period',jsonb_build_object('unit','months','value',1))));
    INSERT INTO public.tasks(id,execute_at_utc,schedule,commands)
    VALUES(gen_random_uuid(),v_first_execution_at_utc,'{"type":"interval","unit":"days","value":1}'::jsonb,v_commands);
    GET DIAGNOSTICS v_inserted_rows = ROW_COUNT;
    IF v_inserted_rows <> 1 THEN RAISE EXCEPTION 'Expected one new Mini task.'; END IF;
END;
$$;
COMMIT;
