using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneGround.ZGW.Autorisaties.DataModel.Migrations
{
    /// <inheritdoc />
    public partial class Add_AuthorizationUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_autorisaties_component_max_vertrouwelijkheidaanduiding_scop~",
                table: "autorisaties",
                columns: new[]
                {
                    "component",
                    "max_vertrouwelijkheidaanduiding",
                    "scopes",
                    "applicatie_id",
                    "zaak_type",
                    "besluit_type",
                    "informatie_object_type",
                    "owner",
                },
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_autorisaties_component_max_vertrouwelijkheidaanduiding_scop~", table: "autorisaties");
        }
    }
}
