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
            migrationBuilder.AddColumn<bool>(
                name: "is_gereed_voor_publicatie",
                table: "enkelvoudiginformatieobjectversies",
                type: "boolean",
                nullable: true
            );

            migrationBuilder.AddColumn<bool>(
                name: "tonen_aan_initiator",
                table: "enkelvoudiginformatieobjectversies",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "is_gereed_voor_publicatie", table: "enkelvoudiginformatieobjectversies");

            migrationBuilder.DropColumn(name: "tonen_aan_initiator", table: "enkelvoudiginformatieobjectversies");
        }
    }
}
