using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneGround.ZGW.Documenten.DataModel.Migrations
{
    /// <inheritdoc />
    public partial class fix_performance_issues_20260403b : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX CONCURRENTLY IF EXISTS public.""idx_e0_light_covering""");

            migrationBuilder.Sql(
                @"
CREATE INDEX CONCURRENTLY IF NOT EXISTS ""t3b_IX_eio_owner_id_incl_type_latest""
    ON public.enkelvoudiginformatieobjecten USING btree
    (owner COLLATE pg_catalog.""default"" ASC NULLS LAST, id ASC NULLS LAST)
    INCLUDE(informatieobjecttype, latest_enkelvoudiginformatieobjectversie_id)
    TABLESPACE pg_default
",
                suppressTransaction: true
            );

            migrationBuilder.Sql(
                @"
CREATE INDEX CONCURRENTLY IF NOT EXISTS ""t3b_IX_enkelvoudiginformatieobjectversies_trefwoorden""
    ON public.enkelvoudiginformatieobjectversies USING gin
    (trefwoorden COLLATE pg_catalog.""default"")
    TABLESPACE pg_default;
",
                suppressTransaction: true
            );

            migrationBuilder.Sql(
                @"
CREATE INDEX CONCURRENTLY IF NOT EXISTS t3b_idx_e0_light_covering
    ON public.enkelvoudiginformatieobjectversies USING btree
    (id ASC NULLS LAST)
    INCLUDE(owner, vertrouwelijkheidaanduiding, enkelvoudiginformatieobject_id, bronorganisatie, identificatie)
    TABLESPACE pg_default
",
                suppressTransaction: true
            );
        }
    }
}
