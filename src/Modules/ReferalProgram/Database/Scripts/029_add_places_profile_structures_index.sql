-- Speeds up structure membership lookups for referral profiles across all programs.
-- Run against the Programs database as a standalone statement with autocommit.
-- CREATE INDEX CONCURRENTLY must not run inside a transaction (BEGIN/COMMIT).
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_places_profile_structures
    ON public.places (marketing_addr, profile_addr, structure_number)
    WHERE structure_number <> 0;
