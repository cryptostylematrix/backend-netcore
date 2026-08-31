-- Makes marketing_tasks the authoritative, immutable command receipt.
--
-- Existing rows cannot be migrated safely because they do not contain the
-- exact response source and code originally returned. Run this migration only
-- with task processing stopped and no old on-chain command awaiting a reply.

BEGIN;

DROP INDEX IF EXISTS public.idx_places_marketing_task_key;
DROP INDEX IF EXISTS public.idx_marketing_tasks_status;

TRUNCATE TABLE public.marketing_tasks;

ALTER TABLE public.marketing_tasks
    DROP CONSTRAINT marketing_tasks_status_check,
    DROP COLUMN status,
    DROP COLUMN updated_at,
    ADD COLUMN place_id integer NOT NULL,
    ADD COLUMN task_source_addr varchar(600),
    ADD COLUMN response_source_place_id integer NOT NULL,
    ADD COLUMN response_code bigint NOT NULL,
    ADD CONSTRAINT marketing_tasks_place_id_fkey
        FOREIGN KEY (place_id)
        REFERENCES public.places (id),
    ADD CONSTRAINT marketing_tasks_response_source_place_id_fkey
        FOREIGN KEY (response_source_place_id)
        REFERENCES public.places (id),
    ADD CONSTRAINT marketing_tasks_response_code_check
        CHECK (response_code BETWEEN 0 AND 4294967295);

ALTER TABLE public.places
    DROP COLUMN task_key,
    DROP COLUMN task_query_id,
    DROP COLUMN task_source_addr;

COMMIT;
