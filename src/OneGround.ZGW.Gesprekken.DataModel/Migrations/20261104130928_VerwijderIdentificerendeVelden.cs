using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneGround.ZGW.Gesprekken.DataModel.Migrations
{
    /// <inheritdoc />
    public partial class VerwijderIdentificerendeVelden : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "bsn", table: "gespreksdeelnames");

            migrationBuilder.DropColumn(name: "emailadres", table: "gespreksdeelnames");

            migrationBuilder.DropColumn(name: "kvknummer", table: "gespreksdeelnames");

            migrationBuilder.DropColumn(name: "rsin", table: "gespreksdeelnames");

            migrationBuilder.DropColumn(name: "vestigingsnummer", table: "gespreksdeelnames");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "bsn", table: "gespreksdeelnames", type: "character varying(9)", maxLength: 9, nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "emailadres",
                table: "gespreksdeelnames",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "kvknummer",
                table: "gespreksdeelnames",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(name: "rsin", table: "gespreksdeelnames", type: "character varying(9)", maxLength: 9, nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "vestigingsnummer",
                table: "gespreksdeelnames",
                type: "character varying(12)",
                maxLength: 12,
                nullable: true
            );
        }
    }
}
