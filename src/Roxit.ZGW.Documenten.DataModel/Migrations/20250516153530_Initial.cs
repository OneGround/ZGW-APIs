using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Roxit.ZGW.Documenten.DataModel;

#nullable disable

namespace Roxit.ZGW.Documenten.DataModel.Migrations;

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
            name: "bestandsdelen",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                volgnummer = table.Column<int>(type: "integer", nullable: false),
                omvang = table.Column<int>(type: "integer", nullable: false),
                voltooid = table.Column<bool>(type: "boolean", nullable: false),
                enkelvoudiginformatieobjectversie_id = table.Column<Guid>(type: "uuid", nullable: false),
                uploadpart_id = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_bestandsdelen", x => x.id);
            }
        );

        migrationBuilder.CreateTable(
            name: "enkelvoudiginformatieobjecten",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                informatieobjecttype = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                indicatiegebruiksrecht = table.Column<bool>(type: "boolean", nullable: true),
                locked = table.Column<bool>(type: "boolean", nullable: false),
                @lock = table.Column<string>(name: "lock", type: "text", nullable: true),
                latest_enkelvoudiginformatieobjectversie_id = table.Column<Guid>(type: "uuid", nullable: true),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_enkelvoudiginformatieobjecten", x => x.id);
            }
        );

        migrationBuilder.CreateTable(
            name: "enkelvoudiginformatieobjectversies",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                identificatie = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                bronorganisatie = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                creatiedatum = table.Column<DateOnly>(type: "date", nullable: true),
                titel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                vertrouwelijkheidaanduiding = table.Column<int>(type: "integer", nullable: true),
                auteur = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                status = table.Column<int>(type: "integer", nullable: true),
                formaat = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                taal = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                versie = table.Column<int>(type: "integer", nullable: false),
                beginregistratie = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                bestandsnaam = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                inhoud = table.Column<string>(type: "text", nullable: true),
                bestandsomvang = table.Column<long>(type: "bigint", nullable: false),
                link = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                beschrijving = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                ontvangstdatum = table.Column<DateOnly>(type: "date", nullable: true),
                verzenddatum = table.Column<DateOnly>(type: "date", nullable: true),
                integriteit_algoritme = table.Column<int>(type: "integer", nullable: false),
                ondertekening_soort = table.Column<int>(type: "integer", maxLength: 50, nullable: true),
                ondertekening_datum = table.Column<DateOnly>(type: "date", nullable: true),
                integriteit_waarde = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                integriteit_datum = table.Column<DateOnly>(type: "date", nullable: true),
                verschijningsvorm = table.Column<string>(type: "text", nullable: true),
                trefwoorden = table.Column<List<string>>(type: "text[]", nullable: true),
                inhoud_is_vervallen = table.Column<bool>(type: "boolean", nullable: false),
                enkelvoudiginformatieobject_id = table.Column<Guid>(type: "uuid", nullable: false),
                multipartdocument_id = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_enkelvoudiginformatieobjectversies", x => x.id);
                table.ForeignKey(
                    name: "FK_enkelvoudiginformatieobjectversies_enkelvoudiginformatieobj~",
                    column: x => x.enkelvoudiginformatieobject_id,
                    principalTable: "enkelvoudiginformatieobjecten",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "gebruiksrechten",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                omschrijvingvoorwaarden = table.Column<string>(type: "text", nullable: false),
                startdatum = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                einddatum = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                informatieobject_id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_gebruiksrechten", x => x.id);
                table.ForeignKey(
                    name: "FK_gebruiksrechten_enkelvoudiginformatieobjecten_informatieobj~",
                    column: x => x.informatieobject_id,
                    principalTable: "enkelvoudiginformatieobjecten",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "objectinformatieobjecten",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                @object = table.Column<string>(
                    name: "object",
                    type: "character varying(200)",
                    maxLength: 200,
                    nullable: false,
                    collation: "ci_collation"
                ),
                informatieobject_id = table.Column<Guid>(type: "uuid", nullable: false),
                objecttype = table.Column<int>(type: "integer", nullable: false),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_objectinformatieobjecten", x => x.id);
                table.ForeignKey(
                    name: "FK_objectinformatieobjecten_enkelvoudiginformatieobjecten_info~",
                    column: x => x.informatieobject_id,
                    principalTable: "enkelvoudiginformatieobjecten",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "verzendingen",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                betrokkene = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, collation: "ci_collation"),
                aardrelatie = table.Column<short>(type: "smallint", nullable: false),
                toelichting = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                ontvangstdatum = table.Column<DateOnly>(type: "date", nullable: true),
                verzenddatum = table.Column<DateOnly>(type: "date", nullable: true),
                contactpersoon = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                contactpersoonnaam = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                binnenlandsCorrespondentieadres = table.Column<BinnenlandsCorrespondentieAdres>(type: "jsonb", nullable: true),
                buitenlandsCorrespondentieadres = table.Column<BuitenlandsCorrespondentieAdres>(type: "jsonb", nullable: true),
                correspondentiepostadres = table.Column<CorrespondentiePostadres>(type: "jsonb", nullable: true),
                faxnummer = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                emailadres = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                mijnoverheid = table.Column<bool>(type: "boolean", nullable: false),
                telefoonnummer = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                informatieobject_id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_verzendingen", x => x.id);
                table.ForeignKey(
                    name: "FK_verzendingen_enkelvoudiginformatieobjecten_informatieobject~",
                    column: x => x.informatieobject_id,
                    principalTable: "enkelvoudiginformatieobjecten",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateIndex(name: "IX_audittrail_hoofdobject_id", table: "audittrail", column: "hoofdobject_id");

        migrationBuilder.CreateIndex(
            name: "IX_bestandsdelen_enkelvoudiginformatieobjectversie_id",
            table: "bestandsdelen",
            column: "enkelvoudiginformatieobjectversie_id"
        );

        migrationBuilder.CreateIndex(
            name: "IX_enkelvoudiginformatieobjecten_informatieobjecttype",
            table: "enkelvoudiginformatieobjecten",
            column: "informatieobjecttype"
        );

        migrationBuilder.CreateIndex(
            name: "IX_enkelvoudiginformatieobjecten_latest_enkelvoudiginformatieo~",
            table: "enkelvoudiginformatieobjecten",
            column: "latest_enkelvoudiginformatieobjectversie_id",
            unique: true
        );

        migrationBuilder.CreateIndex(name: "IX_enkelvoudiginformatieobjecten_owner", table: "enkelvoudiginformatieobjecten", column: "owner");

        migrationBuilder.CreateIndex(
            name: "IX_enkelvoudiginformatieobjecten_owner_informatieobjecttype_la~",
            table: "enkelvoudiginformatieobjecten",
            columns: ["owner", "informatieobjecttype", "latest_enkelvoudiginformatieobjectversie_id"]
        );

        migrationBuilder
            .CreateIndex(name: "idx_e0_light_covering", table: "enkelvoudiginformatieobjectversies", column: "id")
            .Annotation("Npgsql:IndexInclude", new[] { "owner", "vertrouwelijkheidaanduiding", "enkelvoudiginformatieobject_id" });

        migrationBuilder.CreateIndex(
            name: "IX_enkelvoudiginformatieobjectversies_bronorganisatie",
            table: "enkelvoudiginformatieobjectversies",
            column: "bronorganisatie"
        );

        migrationBuilder.CreateIndex(
            name: "IX_enkelvoudiginformatieobjectversies_bronorganisatie_identifi~",
            table: "enkelvoudiginformatieobjectversies",
            columns: ["bronorganisatie", "identificatie", "versie"],
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_enkelvoudiginformatieobjectversies_enkelvoudiginformatieobj~",
            table: "enkelvoudiginformatieobjectversies",
            column: "enkelvoudiginformatieobject_id",
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_enkelvoudiginformatieobjectversies_identificatie",
            table: "enkelvoudiginformatieobjectversies",
            column: "identificatie"
        );

        migrationBuilder.CreateIndex(
            name: "IX_enkelvoudiginformatieobjectversies_inhoud",
            table: "enkelvoudiginformatieobjectversies",
            column: "inhoud"
        );

        migrationBuilder.CreateIndex(
            name: "IX_enkelvoudiginformatieobjectversies_owner_enkelvoudiginforma~",
            table: "enkelvoudiginformatieobjectversies",
            columns: ["owner", "enkelvoudiginformatieobject_id", "versie", "vertrouwelijkheidaanduiding"]
        );

        migrationBuilder.CreateIndex(
            name: "IX_enkelvoudiginformatieobjectversies_owner_id_vertrouwelijkhe~",
            table: "enkelvoudiginformatieobjectversies",
            columns: ["owner", "id", "vertrouwelijkheidaanduiding"]
        );

        migrationBuilder.CreateIndex(
            name: "IX_enkelvoudiginformatieobjectversies_vertrouwelijkheidaanduid~",
            table: "enkelvoudiginformatieobjectversies",
            columns: ["vertrouwelijkheidaanduiding", "id", "owner"]
        );

        migrationBuilder.CreateIndex(name: "IX_gebruiksrechten_informatieobject_id", table: "gebruiksrechten", column: "informatieobject_id");

        migrationBuilder.CreateIndex(
            name: "IX_objectinformatieobjecten_informatieobject_id",
            table: "objectinformatieobjecten",
            column: "informatieobject_id"
        );

        migrationBuilder.CreateIndex(name: "IX_objectinformatieobjecten_object", table: "objectinformatieobjecten", column: "object");

        migrationBuilder.CreateIndex(
            name: "IX_objectinformatieobjecten_object_informatieobject_id_objectt~",
            table: "objectinformatieobjecten",
            columns: ["object", "informatieobject_id", "objecttype"],
            unique: true
        );

        migrationBuilder.CreateIndex(name: "IX_verzendingen_aardrelatie", table: "verzendingen", column: "aardrelatie");

        migrationBuilder.CreateIndex(name: "IX_verzendingen_betrokkene", table: "verzendingen", column: "betrokkene");

        migrationBuilder.CreateIndex(name: "IX_verzendingen_informatieobject_id", table: "verzendingen", column: "informatieobject_id");

        migrationBuilder.AddForeignKey(
            name: "FK_bestandsdelen_enkelvoudiginformatieobjectversies_enkelvoudi~",
            table: "bestandsdelen",
            column: "enkelvoudiginformatieobjectversie_id",
            principalTable: "enkelvoudiginformatieobjectversies",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade
        );

        migrationBuilder.AddForeignKey(
            name: "FK_enkelvoudiginformatieobjecten_enkelvoudiginformatieobjectve~",
            table: "enkelvoudiginformatieobjecten",
            column: "latest_enkelvoudiginformatieobjectversie_id",
            principalTable: "enkelvoudiginformatieobjectversies",
            principalColumn: "id"
        );
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_enkelvoudiginformatieobjecten_enkelvoudiginformatieobjectve~",
            table: "enkelvoudiginformatieobjecten"
        );

        migrationBuilder.DropTable(name: "audittrail");

        migrationBuilder.DropTable(name: "bestandsdelen");

        migrationBuilder.DropTable(name: "finished_data_migrations");

        migrationBuilder.DropTable(name: "gebruiksrechten");

        migrationBuilder.DropTable(name: "objectinformatieobjecten");

        migrationBuilder.DropTable(name: "organisatie_nummers");

        migrationBuilder.DropTable(name: "verzendingen");

        migrationBuilder.DropTable(name: "enkelvoudiginformatieobjectversies");

        migrationBuilder.DropTable(name: "enkelvoudiginformatieobjecten");
    }
}
