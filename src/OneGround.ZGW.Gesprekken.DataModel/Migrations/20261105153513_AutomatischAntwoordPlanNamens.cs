using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneGround.ZGW.Gesprekken.DataModel.Migrations
{
    /// <inheritdoc />
    public partial class AutomatischAntwoordPlanNamens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "namens",
                table: "gespreksdeelnames",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true
            );

            migrationBuilder.AddColumn<bool>(name: "automatisch", table: "gespreksbijdragen", type: "boolean", nullable: false, defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "aanleidingplan",
                table: "gesprekken",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true
            );

            migrationBuilder.CreateIndex(name: "IX_gesprekken_aanleidingplan", table: "gesprekken", column: "aanleidingplan");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_gesprekken_aanleidingplan", table: "gesprekken");

            migrationBuilder.DropColumn(name: "namens", table: "gespreksdeelnames");

            migrationBuilder.DropColumn(name: "automatisch", table: "gespreksbijdragen");

            migrationBuilder.DropColumn(name: "aanleidingplan", table: "gesprekken");
        }
    }
}
