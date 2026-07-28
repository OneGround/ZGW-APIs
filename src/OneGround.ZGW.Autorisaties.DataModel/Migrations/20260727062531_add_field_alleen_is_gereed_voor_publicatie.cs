using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneGround.ZGW.Autorisaties.DataModel.Migrations
{
    /// <inheritdoc />
    public partial class add_field_alleen_is_gereed_voor_publicatie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "alleen_is_gereed_voor_publicatie",
                table: "applicaties",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "alleen_is_gereed_voor_publicatie", table: "applicaties");
        }
    }
}
