-- Moves volume ownership from places to profiles within a Program structure.
-- Existing place volume values are intentionally not migrated.

BEGIN;

CREATE TABLE public.profile_volumes
(
    marketing_addr   text         NOT NULL,
    structure_number smallint     NOT NULL,
    profile_addr     varchar(600) NOT NULL,
    personal_volume  bigint       NOT NULL DEFAULT 0,
    referral_volume  bigint       NOT NULL DEFAULT 0,
    group_volume     bigint       NOT NULL DEFAULT 0,

    CONSTRAINT profile_volumes_pkey
        PRIMARY KEY (marketing_addr, structure_number, profile_addr),
    CONSTRAINT profile_volumes_structure_fkey
        FOREIGN KEY (marketing_addr, structure_number)
        REFERENCES public.structures (marketing_addr, structure_number)
        ON DELETE CASCADE,
    CONSTRAINT profile_volumes_structure_number_check
        CHECK (structure_number BETWEEN 0 AND 255),
    CONSTRAINT profile_volumes_profile_addr_check
        CHECK (BTRIM(profile_addr) <> ''),
    CONSTRAINT profile_volumes_personal_volume_check
        CHECK (personal_volume BETWEEN 0 AND 4294967295),
    CONSTRAINT profile_volumes_referral_volume_check
        CHECK (referral_volume BETWEEN 0 AND 4294967295),
    CONSTRAINT profile_volumes_group_volume_check
        CHECK (group_volume BETWEEN 0 AND 4294967295)
);

GRANT SELECT, INSERT, UPDATE, DELETE
    ON TABLE public.profile_volumes
    TO cs_programs_user;

ALTER TABLE public.places
    DROP COLUMN personal_volume,
    DROP COLUMN group_volume;

COMMIT;
