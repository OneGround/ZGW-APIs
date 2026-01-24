using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using OneGround.ZGW.Documenten.DataModel;

#nullable disable

namespace OneGround.ZGW.Documenten.DataModel.Migrations.DrcDbContext2Migrations
{
    /// <inheritdoc />
    public partial class add_new_drc_datamodel : Migration
    {
        /// <inheritdoc />
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
                name: "enkelvoudiginformatieobject_locks_2",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    locked = table.Column<bool>(type: "boolean", nullable: false),
                    @lock = table.Column<string>(name: "lock", type: "text", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_enkelvoudiginformatieobject_locks_2", x => x.id);
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
                name: "enkelvoudiginformatieobjecten_2",
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
                    enkelvoudiginformatieobject_lock_id = table.Column<Guid>(type: "uuid", nullable: false),
                    enkelvoudiginformatieobject_id = table.Column<Guid>(name: "enkelvoudiginformatieobject_id ", type: "uuid", nullable: false),
                    informatieobjecttype = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    indicatiegebruiksrecht = table.Column<bool>(type: "boolean", nullable: true),
                    multipartdocument_id = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_enkelvoudiginformatieobjecten_2", x => x.id);
                    table.ForeignKey(
                        name: "FK_enkelvoudiginformatieobjecten_2_enkelvoudiginformatieobject~",
                        column: x => x.enkelvoudiginformatieobject_lock_id,
                        principalTable: "enkelvoudiginformatieobject_locks_2",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "bestandsdelen2",
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
                    informatieobject_id2 = table.Column<Guid>(type: "uuid", nullable: false),
                    informatieobject_id21 = table.Column<Guid>(type: "uuid", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gebruiksrechten", x => x.id);
                    table.ForeignKey(
                        name: "FK_gebruiksrechten_enkelvoudiginformatieobjecten_2_informatieo~",
                        column: x => x.informatieobject_id21,
                        principalTable: "enkelvoudiginformatieobjecten_2",
                        principalColumn: "id"
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
                    informatieobject_id2 = table.Column<Guid>(type: "uuid", nullable: false),
                    informatieobject_id21 = table.Column<Guid>(type: "uuid", nullable: true),
                    objecttype = table.Column<int>(type: "integer", nullable: false),
                    owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_objectinformatieobjecten", x => x.id);
                    table.ForeignKey(
                        name: "FK_objectinformatieobjecten_enkelvoudiginformatieobjecten_2_in~",
                        column: x => x.informatieobject_id21,
                        principalTable: "enkelvoudiginformatieobjecten_2",
                        principalColumn: "id"
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
                    informatieobject_id2 = table.Column<Guid>(type: "uuid", nullable: false),
                    informatieobject_id21 = table.Column<Guid>(type: "uuid", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_verzendingen", x => x.id);
                    table.ForeignKey(
                        name: "FK_verzendingen_enkelvoudiginformatieobjecten_2_informatieobje~",
                        column: x => x.informatieobject_id21,
                        principalTable: "enkelvoudiginformatieobjecten_2",
                        principalColumn: "id"
                    );
                }
            );

            migrationBuilder.CreateIndex(name: "IX_audittrail_hoofdobject_id", table: "audittrail", column: "hoofdobject_id");

            migrationBuilder.CreateIndex(
                name: "IX_bestandsdelen2_enkelvoudiginformatieobject_id",
                table: "bestandsdelen2",
                column: "enkelvoudiginformatieobject_id"
            );

            migrationBuilder
                .CreateIndex(name: "idx_e0_light_covering", table: "enkelvoudiginformatieobjecten_2", column: "id")
                .Annotation("Npgsql:IndexInclude", new[] { "owner", "vertrouwelijkheidaanduiding", "enkelvoudiginformatieobject_id " });

            migrationBuilder.CreateIndex(
                name: "IX_enkelvoudiginformatieobjecten_2_bronorganisatie",
                table: "enkelvoudiginformatieobjecten_2",
                column: "bronorganisatie"
            );

            migrationBuilder.CreateIndex(
                name: "IX_enkelvoudiginformatieobjecten_2_enkelvoudiginformatieobjec~1",
                table: "enkelvoudiginformatieobjecten_2",
                column: "enkelvoudiginformatieobject_lock_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_enkelvoudiginformatieobjecten_2_enkelvoudiginformatieobject~",
                table: "enkelvoudiginformatieobjecten_2",
                column: "enkelvoudiginformatieobject_id "
            );

            migrationBuilder.CreateIndex(
                name: "IX_enkelvoudiginformatieobjecten_2_identificatie",
                table: "enkelvoudiginformatieobjecten_2",
                column: "identificatie"
            );

            migrationBuilder.CreateIndex(
                name: "IX_enkelvoudiginformatieobjecten_2_informatieobjecttype",
                table: "enkelvoudiginformatieobjecten_2",
                column: "informatieobjecttype"
            );

            migrationBuilder.CreateIndex(
                name: "IX_enkelvoudiginformatieobjecten_2_inhoud",
                table: "enkelvoudiginformatieobjecten_2",
                column: "inhoud"
            );

            migrationBuilder.CreateIndex(name: "IX_enkelvoudiginformatieobjecten_2_owner", table: "enkelvoudiginformatieobjecten_2", column: "owner");

            migrationBuilder.CreateIndex(
                name: "IX_enkelvoudiginformatieobjecten_2_owner_enkelvoudiginformatie~",
                table: "enkelvoudiginformatieobjecten_2",
                columns: new[] { "owner", "enkelvoudiginformatieobject_id ", "versie", "vertrouwelijkheidaanduiding" },
                descending: new[] { false, false, true, false }
            );

            migrationBuilder.CreateIndex(
                name: "IX_enkelvoudiginformatieobjecten_2_owner_id_vertrouwelijkheida~",
                table: "enkelvoudiginformatieobjecten_2",
                columns: new[] { "owner", "id", "vertrouwelijkheidaanduiding" }
            );

            migrationBuilder
                .CreateIndex(
                    name: "IX_enkelvoudiginformatieobjecten_2_owner_inhoud_vertrouwelijk~1",
                    table: "enkelvoudiginformatieobjecten_2",
                    columns: new[] { "owner", "inhoud", "vertrouwelijkheidaanduiding", "enkelvoudiginformatieobject_id " },
                    descending: new[] { false, false, true, false },
                    filter: "Bestandsomvang IS NOT NULL"
                )
                .Annotation("Npgsql:IndexInclude", new[] { "bestandsomvang" });

            migrationBuilder.CreateIndex(
                name: "IX_enkelvoudiginformatieobjecten_2_owner_inhoud_vertrouwelijkh~",
                table: "enkelvoudiginformatieobjecten_2",
                columns: new[] { "owner", "inhoud", "vertrouwelijkheidaanduiding" },
                descending: new[] { false, false, true }
            );

            migrationBuilder.CreateIndex(
                name: "IX_enkelvoudiginformatieobjecten_2_vertrouwelijkheidaanduiding~",
                table: "enkelvoudiginformatieobjecten_2",
                columns: new[] { "vertrouwelijkheidaanduiding", "id", "owner" }
            );

            migrationBuilder.CreateIndex(name: "IX_gebruiksrechten_informatieobject_id21", table: "gebruiksrechten", column: "informatieobject_id21");

            migrationBuilder.CreateIndex(
                name: "IX_objectinformatieobjecten_informatieobject_id",
                table: "objectinformatieobjecten",
                column: "informatieobject_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_objectinformatieobjecten_informatieobject_id21",
                table: "objectinformatieobjecten",
                column: "informatieobject_id21"
            );

            migrationBuilder.CreateIndex(name: "IX_objectinformatieobjecten_object", table: "objectinformatieobjecten", column: "object");

            migrationBuilder.CreateIndex(
                name: "IX_objectinformatieobjecten_object_informatieobject_id_objectt~",
                table: "objectinformatieobjecten",
                columns: new[] { "object", "informatieobject_id", "objecttype" },
                unique: true
            );

            migrationBuilder.CreateIndex(name: "IX_verzendingen_aardrelatie", table: "verzendingen", column: "aardrelatie");

            migrationBuilder.CreateIndex(name: "IX_verzendingen_betrokkene", table: "verzendingen", column: "betrokkene");

            migrationBuilder.CreateIndex(name: "IX_verzendingen_informatieobject_id21", table: "verzendingen", column: "informatieobject_id21");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "audittrail");

            migrationBuilder.DropTable(name: "bestandsdelen2");

            migrationBuilder.DropTable(name: "finished_data_migrations");

            migrationBuilder.DropTable(name: "gebruiksrechten");

            migrationBuilder.DropTable(name: "objectinformatieobjecten");

            migrationBuilder.DropTable(name: "organisatie_nummers");

            migrationBuilder.DropTable(name: "verzendingen");

            migrationBuilder.DropTable(name: "enkelvoudiginformatieobjecten_2");

            migrationBuilder.DropTable(name: "enkelvoudiginformatieobject_locks_2");
        }
    }
}
