using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneGround.ZGW.Documenten.DataModel.Migrations
{
    /// <inheritdoc />
    public partial class RemovedUniqueFromEnkelvoudigInformatieObjectVersieIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_enkelvoudiginformatieobjectversies_enkelvoudiginformatieobj~",
                table: "enkelvoudiginformatieobjectversies"
            );

            migrationBuilder.CreateIndex(
                name: "IX_enkelvoudiginformatieobjectversies_enkelvoudiginformatieobj~",
                table: "enkelvoudiginformatieobjectversies",
                column: "enkelvoudiginformatieobject_id"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_enkelvoudiginformatieobjectversies_enkelvoudiginformatieobj~",
                table: "enkelvoudiginformatieobjectversies"
            );

            migrationBuilder.CreateIndex(
                name: "IX_enkelvoudiginformatieobjectversies_enkelvoudiginformatieobj~",
                table: "enkelvoudiginformatieobjectversies",
                column: "enkelvoudiginformatieobject_id",
                unique: true
            );
        }
    }
}
