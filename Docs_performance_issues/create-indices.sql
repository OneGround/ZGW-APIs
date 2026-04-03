-- Index: t3b_IX_eio_owner_id_incl_type_latest
-- DROP INDEX IF EXISTS public."t3b_IX_eio_owner_id_incl_type_latest";
CREATE INDEX IF NOT EXISTS "t3b_IX_eio_owner_id_incl_type_latest"
    ON public.enkelvoudiginformatieobjecten USING btree
    (owner COLLATE pg_catalog."default" ASC NULLS LAST, id ASC NULLS LAST)
    INCLUDE(informatieobjecttype, latest_enkelvoudiginformatieobjectversie_id)
    TABLESPACE pg_default;


-- Index: t3b_IX_enkelvoudiginformatieobjectversies_trefwoorden
-- DROP INDEX IF EXISTS public."t3b_IX_enkelvoudiginformatieobjectversies_trefwoorden";
CREATE INDEX IF NOT EXISTS "t3b_IX_enkelvoudiginformatieobjectversies_trefwoorden"
    ON public.enkelvoudiginformatieobjectversies USING gin
    (trefwoorden COLLATE pg_catalog."default")
    TABLESPACE pg_default;


-- Index: t3b_idx_e0_light_covering
-- DROP INDEX IF EXISTS public.t3b_idx_e0_light_covering;
CREATE INDEX IF NOT EXISTS t3b_idx_e0_light_covering
    ON public.enkelvoudiginformatieobjectversies USING btree
    (id ASC NULLS LAST)
    INCLUDE(owner, vertrouwelijkheidaanduiding, enkelvoudiginformatieobject_id, bronorganisatie, identificatie)
    TABLESPACE pg_default;
