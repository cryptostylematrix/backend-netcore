-- Allows 0 to represent an unlimited number of places per profile.
-- Run while connected to the programs database as the table owner.

BEGIN;

ALTER TABLE public.structures
    DROP CONSTRAINT IF EXISTS structures_max_places_per_profile_check;

ALTER TABLE public.structures
    ADD CONSTRAINT structures_max_places_per_profile_check
        CHECK (max_places_per_profile >= 0);

COMMIT;
