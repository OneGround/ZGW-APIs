using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneGround.ZGW.Documenten.DataModel.Migrations.DrcDbContext2Migrations
{
    /// <inheritdoc />
    public partial class use_org_table_bestandsdelen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "bestandsdelen2");

            migrationBuilder.CreateTable(
                name: "bestandsdelen",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    volgnummer = table.Column<int>(type: "integer", nullable: false),
                    omvang = table.Column<int>(type: "integer", nullable: false),
                    voltooid = table.Column<bool>(type: "boolean", nullable: false),
                    enkelvoudiginformatieobject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    uploadpart_id = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bestandsdelen", x => x.id);
                    table.ForeignKey(
                        name: "FK_bestandsdelen_enkelvoudiginformatieobjecten_2_enkelvoudigin~",
                        column: x => x.enkelvoudiginformatieobject_id,
                        principalTable: "enkelvoudiginformatieobjecten_2",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_bestandsdelen_enkelvoudiginformatieobject_id",
                table: "bestandsdelen",
                column: "enkelvoudiginformatieobject_id"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "bestandsdelen");

            migrationBuilder.CreateTable(
                name: "bestandsdelen2",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    enkelvoudiginformatieobject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    omvang = table.Column<int>(type: "integer", nullable: false),
                    uploadpart_id = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    volgnummer = table.Column<int>(type: "integer", nullable: false),
                    voltooid = table.Column<bool>(type: "boolean", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bestandsdelen2", x => x.id);
                    table.ForeignKey(
                        name: "FK_bestandsdelen2_enkelvoudiginformatieobjecten_2_enkelvoudigi~",
                        column: x => x.enkelvoudiginformatieobject_id,
                        principalTable: "enkelvoudiginformatieobjecten_2",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_bestandsdelen2_enkelvoudiginformatieobject_id",
                table: "bestandsdelen2",
                column: "enkelvoudiginformatieobject_id"
            );
        }
    }
}
