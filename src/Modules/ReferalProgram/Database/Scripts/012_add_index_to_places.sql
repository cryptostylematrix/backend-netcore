BEGIN;

ALTER TABLE public.places
    ADD COLUMN "index" text;

-- Backfill existing rows only. New values are supplied by application code.
UPDATE public.places
SET "index" = COALESCE(profile_login, '') || place_number::text;

ALTER TABLE public.places
    ALTER COLUMN "index" SET NOT NULL;

CREATE INDEX idx_places_search_index
    ON public.places
        (marketing_addr, structure_number, lower("index") text_pattern_ops);

COMMIT;
