BEGIN;

CREATE TABLE public.structure_ranks
(
    marketing_addr                   text         NOT NULL,
    structure_number                 smallint     NOT NULL,
    required_active_referral_places  bigint       NOT NULL,
    name                             varchar(100) NOT NULL,

    CONSTRAINT structure_ranks_pkey
        PRIMARY KEY
        (
            marketing_addr,
            structure_number,
            required_active_referral_places
        ),
    CONSTRAINT structure_ranks_name_unique
        UNIQUE (marketing_addr, structure_number, name),
    CONSTRAINT structure_ranks_structure_fkey
        FOREIGN KEY (marketing_addr, structure_number)
        REFERENCES public.structures (marketing_addr, structure_number)
        ON DELETE CASCADE,
    CONSTRAINT structure_ranks_structure_number_check
        CHECK (structure_number BETWEEN 0 AND 255),
    CONSTRAINT structure_ranks_required_places_check
        CHECK (required_active_referral_places BETWEEN 0 AND 4294967295),
    CONSTRAINT structure_ranks_name_check
        CHECK (BTRIM(name) <> '')
);

GRANT SELECT, INSERT, UPDATE, DELETE
    ON TABLE public.structure_ranks
    TO cs_programs_user;

COMMIT;
