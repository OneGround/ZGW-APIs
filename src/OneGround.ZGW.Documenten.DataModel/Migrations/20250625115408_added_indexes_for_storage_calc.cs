using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneGround.ZGW.Documenten.DataModel.Migrations
{
    /// <inheritdoc />
    public partial class added_indexes_for_storage_calc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_enkelvoudiginformatieobjectversies_owner_inhoud_vertrouwel~1",
                table: "enkelvoudiginformatieobjectversies",
                columns: new[] { "owner", "inhoud", "vertrouwelijkheidaanduiding", "enkelvoudiginformatieobject_id" },
                descending: new[] { false, false, true, false },
                filter: "Bestandsomvang IS NOT NULL")
                .Annotation("Npgsql:IndexInclude", new[] { "bestandsomvang" });

            migrationBuilder.CreateIndex(
                name: "IX_enkelvoudiginformatieobjectversies_owner_inhoud_vertrouweli~",
                table: "enkelvoudiginformatieobjectversies",
                columns: new[] { "owner", "inhoud", "vertrouwelijkheidaanduiding" },
                descending: new[] { false, false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_enkelvoudiginformatieobjectversies_owner_inhoud_vertrouwel~1",
                table: "enkelvoudiginformatieobjectversies");

            migrationBuilder.DropIndex(
                name: "IX_enkelvoudiginformatieobjectversies_owner_inhoud_vertrouweli~",
                table: "enkelvoudiginformatieobjectversies");
        }
    }
}
