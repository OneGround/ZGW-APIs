using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneGround.ZGW.Documenten.DataModel.Migrations
{
    /// <inheritdoc />
    public partial class optimize_drc_index_cleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop obsolete single-column EIO indices — superseded by composite and covering indices.
            migrationBuilder.Sql(
                @"DROP INDEX CONCURRENTLY IF EXISTS public.""IX_enkelvoudiginformatieobjecten_informatieobjecttype"";",
                suppressTransaction: true
            );
            migrationBuilder.Sql(@"DROP INDEX CONCURRENTLY IF EXISTS public.""IX_enkelvoudiginformatieobjecten_owner"";", suppressTransaction: true);

            // Drop old composite EIO index that referenced LatestEnkelvoudigInformatieObjectVersieId.
            // Replaced by t3b_IX_eio_owner_iot_latest_vha which uses the denormalized LatestVertrouwelijkheidAanduiding.
            migrationBuilder.Sql(
                @"DROP INDEX CONCURRENTLY IF EXISTS public.""IX_enkelvoudiginformatieobjecten_owner_informatieobjecttype_la~"";",
                suppressTransaction: true
            );

            // Drop old covering EIO index that included LatestEnkelvoudigInformatieObjectVersieId.
            // Replaced by t3b_IX_eio_owner_id_incl_type_latest_vha which includes LatestVertrouwelijkheidAanduiding.
            migrationBuilder.Sql(@"DROP INDEX CONCURRENTLY IF EXISTS public.""IX_eio_owner_id_incl_type_latest"";", suppressTransaction: true);

            // Drop obsolete Versie indices — no longer needed after LatestVertrouwelijkheidAanduiding was
            // denormalized onto EnkelvoudigInformatieObject. Auth filtering no longer joins through versies.
            migrationBuilder.Sql(
                @"DROP INDEX CONCURRENTLY IF EXISTS public.""IX_enkelvoudiginformatieobjectversies_owner_vertrouwelijkheid~"";",
                suppressTransaction: true
            );
            migrationBuilder.Sql(@"DROP INDEX CONCURRENTLY IF EXISTS public.t3b_idx_eiov_owner_vha_id;", suppressTransaction: true);
            migrationBuilder.Sql(
                @"DROP INDEX CONCURRENTLY IF EXISTS public.""IX_enkelvoudiginformatieobjectversies_vertrouwelijkheidaanduid~"";",
                suppressTransaction: true
            );

            // Ensure new indices exist. These were already created in prior raw-SQL migrations;
            // IF NOT EXISTS makes this idempotent across all environments.
            migrationBuilder.Sql(
                @"
CREATE INDEX CONCURRENTLY IF NOT EXISTS ""t3b_IX_eio_owner_id_incl_type_latest_vha""
    ON public.enkelvoudiginformatieobjecten USING btree
    (owner COLLATE pg_catalog.""default"" ASC NULLS LAST, id ASC NULLS LAST)
    INCLUDE (informatieobjecttype, latest_vertrouwelijkheidaanduiding)
    TABLESPACE pg_default;
",
                suppressTransaction: true
            );
            migrationBuilder.Sql(
                @"
CREATE INDEX CONCURRENTLY IF NOT EXISTS ""t3b_IX_eio_owner_iot_latest_vha""
    ON public.enkelvoudiginformatieobjecten USING btree
    (owner COLLATE pg_catalog.""default"" ASC NULLS LAST,
     informatieobjecttype COLLATE pg_catalog.""default"" ASC NULLS LAST,
     latest_vertrouwelijkheidaanduiding ASC NULLS LAST)
    TABLESPACE pg_default;
",
                suppressTransaction: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"DROP INDEX CONCURRENTLY IF EXISTS public.""t3b_IX_eio_owner_id_incl_type_latest_vha"";",
                suppressTransaction: true
            );
            migrationBuilder.Sql(@"DROP INDEX CONCURRENTLY IF EXISTS public.""t3b_IX_eio_owner_iot_latest_vha"";", suppressTransaction: true);

            migrationBuilder.Sql(
                @"
CREATE INDEX CONCURRENTLY IF NOT EXISTS ""IX_enkelvoudiginformatieobjecten_informatieobjecttype""
    ON public.enkelvoudiginformatieobjecten (informatieobjecttype);
",
                suppressTransaction: true
            );
            migrationBuilder.Sql(
                @"
CREATE INDEX CONCURRENTLY IF NOT EXISTS ""IX_enkelvoudiginformatieobjecten_owner""
    ON public.enkelvoudiginformatieobjecten (owner);
",
                suppressTransaction: true
            );
            migrationBuilder.Sql(
                @"
CREATE INDEX CONCURRENTLY IF NOT EXISTS ""IX_enkelvoudiginformatieobjecten_owner_informatieobjecttype_la~""
    ON public.enkelvoudiginformatieobjecten (owner, informatieobjecttype, latest_enkelvoudiginformatieobjectversie_id);
",
                suppressTransaction: true
            );
            migrationBuilder.Sql(
                @"
CREATE INDEX CONCURRENTLY IF NOT EXISTS ""IX_eio_owner_id_incl_type_latest""
    ON public.enkelvoudiginformatieobjecten USING btree (owner, id)
    INCLUDE (informatieobjecttype, latest_enkelvoudiginformatieobjectversie_id);
",
                suppressTransaction: true
            );
            migrationBuilder.Sql(
                @"
CREATE INDEX CONCURRENTLY IF NOT EXISTS ""IX_enkelvoudiginformatieobjectversies_vertrouwelijkheidaanduid~""
    ON public.enkelvoudiginformatieobjectversies (vertrouwelijkheidaanduiding, id, owner);
",
                suppressTransaction: true
            );
        }
    }
}
