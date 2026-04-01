using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneGround.ZGW.Documenten.DataModel.Migrations
{
    /// <inheritdoc />
    public partial class add_table_audittrail_deltas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "legacy_audittrail",
                table: "enkelvoudiginformatieobjecten",
                type: "boolean",
                nullable: false,
                defaultValue: true
            );

            migrationBuilder.CreateTable(
                name: "audittrail_deltas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bron = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    applicatie_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    applicatieweergave = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    gebruikers_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    gebruikersweergave = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    actie = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    actieweergave = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    resultaat = table.Column<int>(type: "integer", nullable: false),
                    hoofdobject = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    resource = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    resourceurl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    toelichting = table.Column<string>(type: "text", nullable: false),
                    resourceweergave = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    aanmaakdatum = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    delta_json = table.Column<string>(type: "jsonb", nullable: true),
                    snapshot_json = table.Column<string>(type: "jsonb", nullable: true),
                    versie = table.Column<int>(type: "integer", nullable: false),
                    request_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    hoofdobject_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audittrail_deltas", x => x.id);
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "audittrail_deltas");

            migrationBuilder.DropColumn(name: "legacy_audittrail", table: "enkelvoudiginformatieobjecten");
        }
    }
}
