using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneGround.ZGW.Documenten.DataModel.Migrations
{
    /// <inheritdoc />
    public partial class optimize_drc_cursor_paging_index : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Covering index for cursor-based pagination on (CreationTime DESC, Id ASC).
            // The INCLUDE columns (InformatieObjectType, LatestVertrouwelijkheidAanduiding) allow
            // the planner to use an Index Only Scan with the inline auth predicate — no heap access needed.
            migrationBuilder.Sql(
                @"
CREATE INDEX CONCURRENTLY IF NOT EXISTS ""t3b_IX_eio_owner_creationtime_id_incl_type_vha""
    ON public.enkelvoudiginformatieobjecten USING btree
    (owner COLLATE pg_catalog.""default"" ASC NULLS LAST,
     creationtime DESC NULLS FIRST,
     id ASC NULLS LAST)
    INCLUDE (informatieobjecttype, latest_vertrouwelijkheidaanduiding)
    TABLESPACE pg_default;
",
                suppressTransaction: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"DROP INDEX CONCURRENTLY IF EXISTS public.""t3b_IX_eio_owner_creationtime_id_incl_type_vha"";",
                suppressTransaction: true
            );
        }
    }
}
