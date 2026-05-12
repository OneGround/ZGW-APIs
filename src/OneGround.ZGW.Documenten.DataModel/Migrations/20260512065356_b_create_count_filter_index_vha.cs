using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneGround.ZGW.Documenten.DataModel.Migrations
{
    /// <inheritdoc />
    public partial class b_create_count_filter_index_vha : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
            migrationBuilder.Sql(@"DROP INDEX CONCURRENTLY IF EXISTS public.""t3b_IX_eio_owner_iot_latest_vha"";", suppressTransaction: true);
        }
    }
}
