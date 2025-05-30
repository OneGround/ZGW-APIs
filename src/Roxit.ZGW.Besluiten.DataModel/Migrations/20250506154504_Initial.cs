using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Roxit.ZGW.Besluiten.DataModel.Migrations;

public partial class Initial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder
            .AlterDatabase()
            .Annotation("Npgsql:CollationDefinition:ci_collation", "@colStrength=secondary,@colStrength=secondary,icu,False")
            .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,")
            .Annotation("Npgsql:PostgresExtension:postgis", ",,");

        migrationBuilder.CreateTable(
            name: "audittrail",
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
                oud = table.Column<string>(type: "jsonb", nullable: true),
                nieuw = table.Column<string>(type: "jsonb", nullable: true),
                request_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                hoofdobject_id = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_audittrail", x => x.id);
            }
        );

        migrationBuilder.CreateTable(
            name: "besluiten",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                identificatie = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                verantwoordelijkeorganisatie = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                besluittype = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, collation: "ci_collation"),
                zaak = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, collation: "ci_collation"),
                zaakbesluit = table.Column<string>(type: "text", nullable: true),
                datum = table.Column<DateOnly>(type: "date", nullable: false),
                toelichting = table.Column<string>(type: "text", nullable: true),
                bestuursorgaan = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                ingangsdatum = table.Column<DateOnly>(type: "date", nullable: false),
                vervaldatum = table.Column<DateOnly>(type: "date", nullable: true),
                vervalreden = table.Column<int>(type: "integer", nullable: true),
                publicatiedatum = table.Column<DateOnly>(type: "date", nullable: true),
                verzenddatum = table.Column<DateOnly>(type: "date", nullable: true),
                uiterlijkeReactiedatum = table.Column<DateOnly>(type: "date", nullable: true),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_besluiten", x => x.id);
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
            name: "organisatie_nummers",
            columns: table => new
            {
                id = table
                    .Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                rsin = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                entiteit = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                formaat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                huidig_nummer = table.Column<long>(type: "bigint", nullable: false),
                huidig_nummer_entiteit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_organisatie_nummers", x => x.id);
            }
        );

        migrationBuilder.CreateTable(
            name: "besluitinformatieobjecten",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                aardrelatie = table.Column<int>(type: "integer", nullable: false),
                registratiedatum = table.Column<DateOnly>(type: "date", nullable: false),
                besluit_id = table.Column<Guid>(type: "uuid", nullable: false),
                informatieobject = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, collation: "ci_collation"),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_besluitinformatieobjecten", x => x.id);
                table.ForeignKey(
                    name: "FK_besluitinformatieobjecten_besluiten_besluit_id",
                    column: x => x.besluit_id,
                    principalTable: "besluiten",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateIndex(name: "IX_audittrail_hoofdobject_id", table: "audittrail", column: "hoofdobject_id");

        migrationBuilder.CreateIndex(name: "IX_besluiten_besluittype", table: "besluiten", column: "besluittype");

        migrationBuilder.CreateIndex(name: "IX_besluiten_identificatie", table: "besluiten", column: "identificatie");

        migrationBuilder.CreateIndex(name: "IX_besluiten_owner_identificatie", table: "besluiten", columns: ["owner", "identificatie"]);

        migrationBuilder.CreateIndex(name: "IX_besluiten_verantwoordelijkeorganisatie", table: "besluiten", column: "verantwoordelijkeorganisatie");

        migrationBuilder.CreateIndex(
            name: "IX_besluiten_verantwoordelijkeorganisatie_identificatie",
            table: "besluiten",
            columns: ["verantwoordelijkeorganisatie", "identificatie"],
            unique: true
        );

        migrationBuilder.CreateIndex(name: "IX_besluiten_zaak", table: "besluiten", column: "zaak");

        migrationBuilder.CreateIndex(name: "IX_besluitinformatieobjecten_besluit_id", table: "besluitinformatieobjecten", column: "besluit_id");

        migrationBuilder.CreateIndex(
            name: "IX_besluitinformatieobjecten_informatieobject",
            table: "besluitinformatieobjecten",
            column: "informatieobject"
        );
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "audittrail");

        migrationBuilder.DropTable(name: "besluitinformatieobjecten");

        migrationBuilder.DropTable(name: "finished_data_migrations");

        migrationBuilder.DropTable(name: "organisatie_nummers");

        migrationBuilder.DropTable(name: "besluiten");
    }
}
