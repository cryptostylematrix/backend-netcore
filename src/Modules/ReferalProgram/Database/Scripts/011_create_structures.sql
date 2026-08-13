BEGIN;

CREATE TABLE public.structures
(
    marketing_addr         text     NOT NULL,
    structure_number       smallint NOT NULL,
    max_places_per_profile integer  NOT NULL,
    width                  smallint NOT NULL,
    height                 smallint NOT NULL,
    prev_required          boolean  NOT NULL,
    pos_algo               jsonb    NOT NULL,

    CONSTRAINT structures_pkey
        PRIMARY KEY (marketing_addr, structure_number),
    CONSTRAINT structures_marketing_addr_fkey
        FOREIGN KEY (marketing_addr)
        REFERENCES public.referal_program (marketing_addr),
    CONSTRAINT structures_structure_number_check
        CHECK (structure_number BETWEEN 0 AND 255),
    CONSTRAINT structures_max_places_per_profile_check
        CHECK (max_places_per_profile >= 0),
    CONSTRAINT structures_width_check
        CHECK (width BETWEEN 0 AND 255),
    CONSTRAINT structures_height_check
        CHECK (height BETWEEN 0 AND 255)
);

GRANT SELECT, INSERT, UPDATE
    ON TABLE public.structures
    TO cs_programs_user;

COMMIT;
