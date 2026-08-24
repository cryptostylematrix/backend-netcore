-- Adds the persisted count of places contained in the matrix rooted at each
-- place. Existing rows start at 1 and must be recalculated with
-- ProgramMatrixFillingRecalculator before filtered reads are enabled.

BEGIN;

ALTER TABLE public.places
    ADD COLUMN matrix_filling bigint NOT NULL DEFAULT 1,
    ADD CONSTRAINT places_matrix_filling_check
        CHECK (matrix_filling >= 1);

CREATE INDEX idx_places_profile_matrix_filling
    ON public.places
        (marketing_addr, structure_number, profile_addr, matrix_filling, place_number);

COMMIT;
