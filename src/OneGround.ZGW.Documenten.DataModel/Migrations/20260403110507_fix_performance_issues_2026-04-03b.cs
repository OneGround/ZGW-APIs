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
            migrationBuilder.DropIndex(name: "idx_e0_light_covering", table: "enkelvoudiginformatieobjectversies");

            migrationBuilder
                .CreateIndex(name: "t3b_idx_e0_light_covering", table: "enkelvoudiginformatieobjectversies", column: "id")
                .Annotation(
                    "Npgsql:IndexInclude",
                    new[] { "owner", "vertrouwelijkheidaanduiding", "enkelvoudiginformatieobject_id", "bronorganisatie", "identificatie" }
                );

            migrationBuilder
                .CreateIndex(
                    name: "t3b_IX_enkelvoudiginformatieobjectversies_trefwoorden",
                    table: "enkelvoudiginformatieobjectversies",
                    column: "trefwoorden"
                )
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder
                .CreateIndex(name: "t3b_IX_eio_owner_id_incl_type_latest", table: "enkelvoudiginformatieobjecten", columns: new[] { "owner", "id" })
                .Annotation("Npgsql:IndexInclude", new[] { "informatieobjecttype", "latest_enkelvoudiginformatieobjectversie_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "t3b_idx_e0_light_covering", table: "enkelvoudiginformatieobjectversies");

            migrationBuilder.DropIndex(name: "t3b_IX_enkelvoudiginformatieobjectversies_trefwoorden", table: "enkelvoudiginformatieobjectversies");

            migrationBuilder.DropIndex(name: "IX_eio_owner_id_incl_type_latest", table: "enkelvoudiginformatieobjecten");

            migrationBuilder
                .CreateIndex(name: "t3b_idx_e0_light_covering", table: "enkelvoudiginformatieobjectversies", column: "id")
                .Annotation("Npgsql:IndexInclude", new[] { "owner", "vertrouwelijkheidaanduiding", "enkelvoudiginformatieobject_id" });
        }
    }
}
