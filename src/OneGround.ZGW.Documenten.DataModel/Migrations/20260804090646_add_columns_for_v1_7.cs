using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneGround.ZGW.Documenten.DataModel.Migrations
{
    /// <inheritdoc />
    public partial class add_columns_for_v1_7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(name: "isgereedvoorpublicatie", table: "enkelvoudiginformatieobjecten", type: "boolean", nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "tonenaaninitiator",
                table: "enkelvoudiginformatieobjecten",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "isgereedvoorpublicatie", table: "enkelvoudiginformatieobjecten");

            migrationBuilder.DropColumn(name: "tonenaaninitiator", table: "enkelvoudiginformatieobjecten");
        }
    }
}
