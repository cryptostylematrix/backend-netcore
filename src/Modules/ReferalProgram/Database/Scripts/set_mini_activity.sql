-- Configure an EXISTING Mini program after deploying the activity-expiration code.
-- Programs database, table owner or SELECT/UPDATE on structures and SELECT on referal_program.
-- Replaces activity JSON for structures 0-3 with the confirmed Mini policy.
-- Other structures, places, dates, volumes and groups are untouched. Safe to repeat.
BEGIN;
DO $$
DECLARE
    v_marketing_addr text := '';
    v_updated_rows integer;
BEGIN
    v_marketing_addr := NULLIF(btrim(v_marketing_addr), '');
    IF v_marketing_addr IS NULL THEN RAISE EXCEPTION 'Set v_marketing_addr to the Mini address.'; END IF;
    IF NOT (has_table_privilege(current_user, 'public.structures', 'SELECT')
        AND has_table_privilege(current_user, 'public.structures', 'UPDATE')) THEN
        RAISE EXCEPTION 'SELECT/UPDATE on structures is required.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM public.referal_program WHERE marketing_addr=v_marketing_addr) THEN
        RAISE EXCEPTION 'Program % does not exist.', v_marketing_addr;
    END IF;
    IF (SELECT count(*) FROM public.structures
        WHERE marketing_addr=v_marketing_addr AND structure_number BETWEEN 1 AND 3
            AND NULLIF(btrim("group"),'') IS NOT NULL) <> 3
        OR (SELECT count(DISTINCT btrim("group")) FROM public.structures
            WHERE marketing_addr=v_marketing_addr AND structure_number BETWEEN 1 AND 3) <> 1
        OR EXISTS (SELECT 1 FROM public.structures root_structure JOIN public.structures first_structure
            ON root_structure.marketing_addr=first_structure.marketing_addr
                AND btrim(root_structure."group")=btrim(first_structure."group")
            WHERE first_structure.marketing_addr=v_marketing_addr AND first_structure.structure_number=1
                AND root_structure.structure_number<1) THEN
        RAISE EXCEPTION 'Mini structures 1-3 must share a nonempty group whose smallest structure is 1. Configure groups first.';
    END IF;
    UPDATE public.structures SET activity=CASE WHEN structure_number=0
        THEN '{"type":"invite","require_marketing_place_to_invite":true,"when_inactive":{"allow_inviting_without_places":false,"allow_inviting_with_places":true,"allow_as_fallback_root":true,"allow_as_bonus_recipient":true,"allow_as_clone_recipient":true,"keep_on_compression":true}}'::jsonb
        ELSE '{"type":"marketing","activity_source":"group_root","when_inactive":{"allow_own_children":true,"allow_spillover_children":false,"check_manual_placement":false,"allow_as_bonus_recipient":true,"allow_as_clone_recipient":true,"keep_on_compression":true}}'::jsonb END
    WHERE marketing_addr=v_marketing_addr AND structure_number BETWEEN 0 AND 3;
    GET DIAGNOSTICS v_updated_rows = ROW_COUNT;
    IF v_updated_rows <> 4 THEN RAISE EXCEPTION 'Expected 4 Mini structures, found %.', v_updated_rows; END IF;
END;
$$;
COMMIT;
