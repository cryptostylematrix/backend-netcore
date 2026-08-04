CREATE TABLE IF NOT EXISTS public.marketing_tasks
(
    task_key integer NOT NULL,
    task_query_id bigint NOT NULL,
    status character varying(50) COLLATE pg_catalog."default" NOT NULL DEFAULT 'pending',
    created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT marketing_tasks_pkey PRIMARY KEY (task_key, task_query_id)
);

CREATE INDEX IF NOT EXISTS idx_marketing_tasks_status
    ON public.marketing_tasks (status);
