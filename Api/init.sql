-- Table: public.audit_log

-- DROP TABLE IF EXISTS public.audit_log;

CREATE TABLE IF NOT EXISTS public.audit_log
(
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    entity_id text COLLATE pg_catalog."default" NOT NULL,
    event_type text COLLATE pg_catalog."default" NOT NULL,
    payload jsonb NOT NULL,
    destination text COLLATE pg_catalog."default",
    created_at timestamp without time zone DEFAULT now(),
    CONSTRAINT audit_log_pkey PRIMARY KEY (id)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.audit_log
    OWNER to postgres;
-- Index: idx_audit_entity

-- DROP INDEX IF EXISTS public.idx_audit_entity;

CREATE INDEX IF NOT EXISTS idx_audit_entity
    ON public.audit_log USING btree
    (entity_id COLLATE pg_catalog."default" ASC NULLS LAST)
    WITH (fillfactor=100, deduplicate_items=True)
    TABLESPACE pg_default;
-- Index: idx_audit_event

-- DROP INDEX IF EXISTS public.idx_audit_event;

CREATE INDEX IF NOT EXISTS idx_audit_event
    ON public.audit_log USING btree
    (event_type COLLATE pg_catalog."default" ASC NULLS LAST)
    WITH (fillfactor=100, deduplicate_items=True)
    TABLESPACE pg_default;