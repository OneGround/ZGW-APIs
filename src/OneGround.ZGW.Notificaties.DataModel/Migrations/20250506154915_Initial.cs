using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneGround.ZGW.Notificaties.DataModel.Migrations;

public partial class Initial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "abonnementen",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                callbackurl = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                auth = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_abonnementen", x => x.id);
            }
        );

        migrationBuilder.CreateTable(
            name: "finished_data_migrations",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "text", nullable: true),
                executed = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                application_version = table.Column<string>(type: "text", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_finished_data_migrations", x => x.id);
            }
        );

        migrationBuilder.CreateTable(
            name: "kanalen",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                naam = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                documentatielink = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                filters = table.Column<string[]>(type: "text[]", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_kanalen", x => x.id);
            }
        );

        migrationBuilder.CreateTable(
            name: "abonnementkanalen",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                kanaal_id = table.Column<Guid>(type: "uuid", nullable: false),
                abonnement_id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_abonnementkanalen", x => x.id);
                table.ForeignKey(
                    name: "FK_abonnementkanalen_abonnementen_abonnement_id",
                    column: x => x.abonnement_id,
                    principalTable: "abonnementen",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
                table.ForeignKey(
                    name: "FK_abonnementkanalen_kanalen_kanaal_id",
                    column: x => x.kanaal_id,
                    principalTable: "kanalen",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "filtervalues",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                abonnement_kanaal_id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_filtervalues", x => x.id);
                table.ForeignKey(
                    name: "FK_filtervalues_abonnementkanalen_abonnement_kanaal_id",
                    column: x => x.abonnement_kanaal_id,
                    principalTable: "abonnementkanalen",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateIndex(name: "IX_abonnementkanalen_abonnement_id", table: "abonnementkanalen", column: "abonnement_id");

        migrationBuilder.CreateIndex(name: "IX_abonnementkanalen_kanaal_id", table: "abonnementkanalen", column: "kanaal_id");

        migrationBuilder.CreateIndex(name: "IX_filtervalues_abonnement_kanaal_id", table: "filtervalues", column: "abonnement_kanaal_id");

        migrationBuilder.CreateIndex(name: "IX_kanalen_naam", table: "kanalen", column: "naam", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "filtervalues");

        migrationBuilder.DropTable(name: "finished_data_migrations");

        migrationBuilder.DropTable(name: "abonnementkanalen");

        migrationBuilder.DropTable(name: "abonnementen");

        migrationBuilder.DropTable(name: "kanalen");
    }
}
