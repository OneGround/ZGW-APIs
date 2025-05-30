using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Roxit.ZGW.Autorisaties.DataModel.Migrations;

public partial class Initial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "applicaties",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                heeft_alle_autorisaties = table.Column<bool>(type: "boolean", nullable: false),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_applicaties", x => x.id);
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
            name: "applicatie_clients",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                client_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                applicatie_id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_applicatie_clients", x => x.id);
                table.ForeignKey(
                    name: "FK_applicatie_clients_applicaties_applicatie_id",
                    column: x => x.applicatie_id,
                    principalTable: "applicaties",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "autorisaties",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                component = table.Column<short>(type: "smallint", nullable: false),
                max_vertrouwelijkheidaanduiding = table.Column<short>(type: "smallint", nullable: true),
                scopes = table.Column<string[]>(type: "text[]", nullable: false),
                applicatie_id = table.Column<Guid>(type: "uuid", nullable: false),
                zaak_type = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                besluit_type = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                informatie_object_type = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_autorisaties", x => x.id);
                table.ForeignKey(
                    name: "FK_autorisaties_applicaties_applicatie_id",
                    column: x => x.applicatie_id,
                    principalTable: "applicaties",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "future_autorisaties",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                component = table.Column<short>(type: "smallint", nullable: false),
                scopes = table.Column<string[]>(type: "text[]", nullable: false),
                max_vertrouwelijkheidaanduiding = table.Column<short>(type: "smallint", nullable: true),
                applicatie_id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_future_autorisaties", x => x.id);
                table.ForeignKey(
                    name: "FK_future_autorisaties_applicaties_applicatie_id",
                    column: x => x.applicatie_id,
                    principalTable: "applicaties",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateIndex(name: "IX_applicatie_clients_applicatie_id", table: "applicatie_clients", column: "applicatie_id");

        migrationBuilder.CreateIndex(name: "IX_autorisaties_applicatie_id", table: "autorisaties", column: "applicatie_id");

        migrationBuilder.CreateIndex(name: "IX_future_autorisaties_applicatie_id", table: "future_autorisaties", column: "applicatie_id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "applicatie_clients");

        migrationBuilder.DropTable(name: "autorisaties");

        migrationBuilder.DropTable(name: "finished_data_migrations");

        migrationBuilder.DropTable(name: "future_autorisaties");

        migrationBuilder.DropTable(name: "applicaties");
    }
}
