ALTER TABLE public.places
    ADD COLUMN task_key integer NOT NULL DEFAULT 0,
    ADD COLUMN task_query_id bigint NOT NULL DEFAULT 0,
    ADD COLUMN task_source_addr character varying(600) COLLATE pg_catalog."default";

CREATE INDEX IF NOT EXISTS idx_places_marketing_task_key
    ON public.places (marketing_addr, task_key);
