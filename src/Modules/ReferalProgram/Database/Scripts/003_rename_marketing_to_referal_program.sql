ALTER TABLE public.marketing
    RENAME TO referal_program;

ALTER TABLE public.referal_program
    RENAME COLUMN addr TO marketing_addr;
