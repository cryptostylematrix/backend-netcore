-- Run against the Programs database as the table owner before deploying the API.
BEGIN;

ALTER TABLE public.structures ADD COLUMN "group" text NULL;

COMMENT ON COLUMN public.structures."group" IS
    'Case-sensitive structural group within a program; NULL means no group.';

-- Normalize direct SQL writes as well as application reads.
CREATE FUNCTION public.normalize_structure_group() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
    NEW."group" := NULLIF(regexp_replace(NEW."group", '^\s+|\s+$', '', 'g'), '');
    RETURN NEW;
END;
$$;

CREATE TRIGGER normalize_structure_group
BEFORE INSERT OR UPDATE OF "group" ON public.structures
FOR EACH ROW EXECUTE FUNCTION public.normalize_structure_group();

COMMIT;
