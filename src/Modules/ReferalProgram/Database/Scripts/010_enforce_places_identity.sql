-- A place is uniquely identified within a marketing contract and structure.
-- NULL profile addresses represent system places and must participate in uniqueness.
CREATE UNIQUE INDEX IF NOT EXISTS ux_places_identity_nulls_not_distinct
    ON public.places
        (marketing_addr, structure_number, profile_addr, place_number)
    NULLS NOT DISTINCT;
