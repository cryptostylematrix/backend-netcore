-- Enables blockchain task processing for Test CryptoCash.
--
-- Run the whole file in pgAdmin Query Tool while connected to the Programs
-- database as postgres (or another table owner). This enables the database
-- switch; the API task-processor service must also be running. The processor
-- is intentionally disabled by application configuration in Development.
--
-- This mirrors EnableProgramTaskProcessingRequestConsumer: if a previous
-- delivery failure disabled the program, only the latest failed receipt has
-- its delivery metadata cleared so its stored response can be retried safely.

BEGIN;

DO $$
DECLARE
    v_marketing_addr text :=
        'EQCFZmVrYR-tLGIWDHjBb-Oyk1tcePk2_ThcytEZA08dNLbO';
    v_reset_failed_receipts integer;
BEGIN
    IF NULLIF(BTRIM(v_marketing_addr), '') IS NULL THEN
        RAISE EXCEPTION 'Test CryptoCash marketing address must be filled.';
    END IF;

    PERFORM 1
    FROM public.referal_program
    WHERE marketing_addr = v_marketing_addr
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION
            'Test CryptoCash program % does not exist.',
            v_marketing_addr;
    END IF;

    WITH latest_failed_task AS
    (
        SELECT marketing_addr, task_key
        FROM public.marketing_tasks
        WHERE marketing_addr = v_marketing_addr
          AND error_at IS NOT NULL
        ORDER BY error_at DESC
        LIMIT 1
        FOR UPDATE
    )
    UPDATE public.marketing_tasks task
    SET
        response_attempted_at = NULL,
        error_at = NULL,
        error_reason = NULL
    FROM latest_failed_task failed
    WHERE task.marketing_addr = failed.marketing_addr
      AND task.task_key = failed.task_key;

    GET DIAGNOSTICS v_reset_failed_receipts = ROW_COUNT;

    UPDATE public.referal_program
    SET is_task_processing_enabled = true
    WHERE marketing_addr = v_marketing_addr;

    RAISE NOTICE
        'Test CryptoCash task processing enabled. Failed receipts reset: %.',
        v_reset_failed_receipts;
END;
$$;

COMMIT;

-- pgAdmin displays the enabled state and any remaining delivery failures.
SELECT
    rp.marketing_addr,
    rp.is_task_processing_enabled,
    (
        SELECT COUNT(*)
        FROM public.marketing_tasks task
        WHERE task.marketing_addr = rp.marketing_addr
          AND task.error_at IS NOT NULL
    ) AS remaining_failed_tasks
FROM public.referal_program rp
WHERE rp.marketing_addr =
    'EQCFZmVrYR-tLGIWDHjBb-Oyk1tcePk2_ThcytEZA08dNLbO';
