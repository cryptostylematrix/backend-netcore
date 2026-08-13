BEGIN;

ALTER TABLE public.locks
    DROP CONSTRAINT IF EXISTS marketing_locks_structure_number_check;

ALTER TABLE public.locks
    ADD CONSTRAINT marketing_locks_structure_number_check
        CHECK (structure_number BETWEEN 0 AND 255);

COMMIT;
