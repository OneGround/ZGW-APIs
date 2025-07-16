using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneGround.ZGW.Catalogi.DataModel.Migrations
{
    /// <inheritdoc />
    public partial class add_owner_fields_to_some_ztc_tables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "owner",
                table: "zaaktypedeelzaaktypen",
                type: "character varying(9)",
                maxLength: 9,
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<string>(
                name: "owner",
                table: "referentieprocessen",
                type: "character varying(9)",
                maxLength: 9,
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<string>(
                name: "owner",
                table: "brondatumarchiefproceduren",
                type: "character varying(9)",
                maxLength: 9,
                nullable: false,
                defaultValue: ""
            );
        }
    }
}
