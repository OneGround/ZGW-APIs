using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneGround.ZGW.Zaken.DataModel.Migrations
{
    /// <inheritdoc />
    public partial class added_zaaknotitie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "zaaknotities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    zaak_id = table.Column<Guid>(type: "uuid", nullable: false),
                    onderwerp = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    tekst = table.Column<string>(type: "text", nullable: false),
                    aangemaaktdoor = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    notitietype = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_zaaknotities", x => x.id);
                    table.ForeignKey(
                        name: "FK_zaaknotities_zaken_zaak_id",
                        column: x => x.zaak_id,
                        principalTable: "zaken",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(name: "IX_zaaknotities_owner", table: "zaaknotities", column: "owner");

            migrationBuilder.CreateIndex(name: "IX_zaaknotities_zaak_id", table: "zaaknotities", column: "zaak_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "zaaknotities");
        }
    }
}
