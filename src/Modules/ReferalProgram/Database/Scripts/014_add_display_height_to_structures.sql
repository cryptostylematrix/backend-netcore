BEGIN;

ALTER TABLE public.structures
    ADD COLUMN display_height smallint;

UPDATE public.structures
SET display_height = height;

ALTER TABLE public.structures
    ALTER COLUMN display_height SET NOT NULL,
    ADD CONSTRAINT structures_display_height_check
        CHECK (display_height BETWEEN 0 AND 255);

COMMIT;
