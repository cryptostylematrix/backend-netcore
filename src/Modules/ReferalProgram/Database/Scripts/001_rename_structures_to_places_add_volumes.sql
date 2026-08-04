-- Migrates the existing structures table to the places schema.
-- Run once while connected to the cs_programs database as the table owner.

BEGIN;

ALTER TABLE public.structures
    RENAME TO places;

ALTER TABLE public.places
    RENAME CONSTRAINT structures_pkey TO places_pkey;

ALTER TABLE public.places
    RENAME CONSTRAINT structures_parent_id_fkey TO places_parent_id_fkey;

ALTER TABLE public.places
    RENAME CONSTRAINT structures_place_unique TO places_place_unique;

ALTER TABLE public.places
    RENAME CONSTRAINT structures_structure_number_check TO places_structure_number_check;

ALTER TABLE public.places
    RENAME CONSTRAINT structures_pos_group_check TO places_pos_group_check;

ALTER TABLE public.places
    RENAME CONSTRAINT structures_kind_check TO places_kind_check;

ALTER TABLE public.places
    RENAME CONSTRAINT structures_place_number_check TO places_place_number_check;

ALTER TABLE public.places
    RENAME CONSTRAINT structures_parent_place_number_check TO places_parent_place_number_check;

ALTER TABLE public.places
    RENAME CONSTRAINT structures_pos_check TO places_pos_check;

ALTER TABLE public.places
    RENAME CONSTRAINT structures_filling_check TO places_filling_check;

ALTER TABLE public.places
    RENAME CONSTRAINT structures_deep_check TO places_deep_check;

ALTER INDEX public.idx_structures_parent_id
    RENAME TO idx_places_parent_id;

ALTER INDEX public.idx_structures_active_mp
    RENAME TO idx_places_active_mp;

ALTER TABLE public.places
    ADD COLUMN parent_profile_login varchar(50),
    ADD COLUMN personal_volume bigint NOT NULL DEFAULT 0,
    ADD COLUMN group_volume bigint NOT NULL DEFAULT 0,
    ADD CONSTRAINT places_personal_volume_check
        CHECK (personal_volume BETWEEN 0 AND 4294967295),
    ADD CONSTRAINT places_group_volume_check
        CHECK (group_volume BETWEEN 0 AND 4294967295);

COMMIT;
