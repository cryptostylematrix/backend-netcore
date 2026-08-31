-- Tracks whether a stored command response was attempted and records a
-- contract-level rejection without changing the immutable response snapshot.

BEGIN;

ALTER TABLE public.marketing_tasks
    ADD COLUMN response_attempted_at timestamp with time zone,
    ADD COLUMN error_at timestamp with time zone,
    ADD COLUMN error_reason text,
    ADD CONSTRAINT marketing_tasks_error_pair_check
        CHECK ((error_at IS NULL) = (error_reason IS NULL)),
    ADD CONSTRAINT marketing_tasks_error_requires_attempt_check
        CHECK (error_at IS NULL OR response_attempted_at IS NOT NULL);

CREATE INDEX idx_marketing_tasks_delivery_errors
    ON public.marketing_tasks (marketing_addr, error_at)
    WHERE error_at IS NOT NULL;

COMMIT;
