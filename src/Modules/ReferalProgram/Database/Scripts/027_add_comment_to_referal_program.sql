-- Adds an optional human-readable label for identifying a program contract.
-- Run against the Programs database as the table owner.

BEGIN;

ALTER TABLE public.referal_program
    ADD COLUMN IF NOT EXISTS comment text;

COMMENT ON COLUMN public.referal_program.comment IS
    'Optional administrative label or notes for identifying the program contract.';

COMMIT;
