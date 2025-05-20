using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;
using OneGround.ZGW.Catalogi.DataModel;

#nullable disable

namespace OneGround.ZGW.Catalogi.DataModel.Migrations;

public partial class Initial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
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
            name: "catalogussen",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                domein = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                rsin = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                contactpersoonbeheernaam = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                contactpersoonbeheertelefoonnummer = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                contactpersoonbeheeremailadres = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                naam = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                versie = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                begindatumVersie = table.Column<DateOnly>(type: "date", nullable: true),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_catalogussen", x => x.id);
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
            name: "besluittypen",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                catalogus_id = table.Column<Guid>(type: "uuid", nullable: false),
                omschrijving = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                omschrijvinggeneriek = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                besluitcategorie = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                reactietermijn = table.Column<Period>(type: "interval", nullable: true),
                publicatieindicatie = table.Column<bool>(type: "boolean", nullable: false),
                publicatietekst = table.Column<string>(type: "text", nullable: true),
                publicatietermijn = table.Column<Period>(type: "interval", nullable: true),
                toelichting = table.Column<string>(type: "text", nullable: true),
                begingeldigheid = table.Column<DateOnly>(type: "date", nullable: false),
                eindegeldigheid = table.Column<DateOnly>(type: "date", nullable: true),
                beginObject = table.Column<DateOnly>(type: "date", nullable: true),
                eindeObject = table.Column<DateOnly>(type: "date", nullable: true),
                concept = table.Column<bool>(type: "boolean", nullable: false),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_besluittypen", x => x.id);
                table.ForeignKey(
                    name: "FK_besluittypen_catalogussen_catalogus_id",
                    column: x => x.catalogus_id,
                    principalTable: "catalogussen",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "informatieobjecttypen",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                omschrijving = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                vertrouwelijkheidaanduiding = table.Column<int>(type: "integer", maxLength: 50, nullable: false),
                concept = table.Column<bool>(type: "boolean", nullable: false),
                begingeldigheid = table.Column<DateOnly>(type: "date", nullable: false),
                eindegeldigheid = table.Column<DateOnly>(type: "date", nullable: true),
                beginObject = table.Column<DateOnly>(type: "date", nullable: true),
                eindeObject = table.Column<DateOnly>(type: "date", nullable: true),
                informatieobjectcategorie = table.Column<string>(type: "text", nullable: true),
                trefwoord = table.Column<string[]>(type: "text[]", nullable: true),
                omschrijvinggeneriek = table.Column<OmschrijvingGeneriek>(type: "jsonb", nullable: true),
                catalogus_id = table.Column<Guid>(type: "uuid", nullable: false),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_informatieobjecttypen", x => x.id);
                table.ForeignKey(
                    name: "FK_informatieobjecttypen_catalogussen_catalogus_id",
                    column: x => x.catalogus_id,
                    principalTable: "catalogussen",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "zaaktypen",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                identificatie = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                omschrijving = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                omschrijvinggeneriek = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                vertrouwelijkheidaanduiding = table.Column<short>(type: "smallint", nullable: false),
                doel = table.Column<string>(type: "text", nullable: false),
                aanleiding = table.Column<string>(type: "text", nullable: false),
                toelichting = table.Column<string>(type: "text", nullable: true),
                indicatieinternofextern = table.Column<short>(type: "smallint", nullable: false),
                handelinginitiator = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                onderwerp = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                handelingbehandelaar = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                doorlooptijd = table.Column<Period>(type: "interval", nullable: false),
                servicenorm = table.Column<Period>(type: "interval", nullable: true),
                opschortingenaanhoudingmogelijk = table.Column<bool>(type: "boolean", nullable: false),
                verlengingmogelijk = table.Column<bool>(type: "boolean", nullable: false),
                verlengingstermijn = table.Column<Period>(type: "interval", nullable: true),
                trefwoorden = table.Column<string[]>(type: "text[]", nullable: true),
                publicatieindicatie = table.Column<bool>(type: "boolean", nullable: false),
                publicatietekst = table.Column<string>(type: "text", nullable: true),
                verantwoordingsrelatie = table.Column<string[]>(type: "text[]", nullable: true),
                productenofdiensten = table.Column<string[]>(type: "text[]", nullable: false),
                selectielijstprocestype = table.Column<string>(type: "text", nullable: true),
                catalogus_id = table.Column<Guid>(type: "uuid", nullable: false),
                begingeldigheid = table.Column<DateOnly>(type: "date", nullable: false),
                eindegeldigheid = table.Column<DateOnly>(type: "date", nullable: true),
                versiedatum = table.Column<DateOnly>(type: "date", nullable: false),
                concept = table.Column<bool>(type: "boolean", nullable: false),
                verantwoordelijke = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                broncatalogus = table.Column<BronCatalogus>(type: "jsonb", nullable: true),
                bronzaaktype = table.Column<BronZaaktype>(type: "jsonb", nullable: true),
                beginObject = table.Column<DateOnly>(type: "date", nullable: true),
                eindeObject = table.Column<DateOnly>(type: "date", nullable: true),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_zaaktypen", x => x.id);
                table.ForeignKey(
                    name: "FK_zaaktypen_catalogussen_catalogus_id",
                    column: x => x.catalogus_id,
                    principalTable: "catalogussen",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "besluittypeinformatieobjecttypen",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                besluittype_id = table.Column<Guid>(type: "uuid", nullable: false),
                informatieobjecttype_omschrijving = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_besluittypeinformatieobjecttypen", x => x.id);
                table.ForeignKey(
                    name: "FK_besluittypeinformatieobjecttypen_besluittypen_besluittype_id",
                    column: x => x.besluittype_id,
                    principalTable: "besluittypen",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "referentieprocessen",
            columns: table => new
            {
                zaaktype_id = table.Column<Guid>(type: "uuid", nullable: false),
                naam = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                link = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_referentieprocessen", x => x.zaaktype_id);
                table.ForeignKey(
                    name: "FK_referentieprocessen_zaaktypen_zaaktype_id",
                    column: x => x.zaaktype_id,
                    principalTable: "zaaktypen",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "resultaattypen",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                zaaktype_id = table.Column<Guid>(type: "uuid", nullable: false),
                omschrijving = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                resultaattypeomschrijving = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                omschrijvinggeneriek = table.Column<string>(type: "text", nullable: false),
                selectielijstklasse = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                toelichting = table.Column<string>(type: "text", nullable: true),
                archiefnominatie = table.Column<short>(type: "smallint", nullable: true),
                archiefactietermijn = table.Column<Period>(type: "interval", nullable: true),
                procesobjectaard = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                begingeldigheid = table.Column<DateOnly>(type: "date", nullable: true),
                eindegeldigheid = table.Column<DateOnly>(type: "date", nullable: true),
                beginObject = table.Column<DateOnly>(type: "date", nullable: true),
                eindeObject = table.Column<DateOnly>(type: "date", nullable: true),
                indicatieSpecifiek = table.Column<bool>(type: "boolean", nullable: true),
                procestermijn = table.Column<Period>(type: "interval", nullable: true),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_resultaattypen", x => x.id);
                table.ForeignKey(
                    name: "FK_resultaattypen_zaaktypen_zaaktype_id",
                    column: x => x.zaaktype_id,
                    principalTable: "zaaktypen",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "roltypen",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                zaaktype_id = table.Column<Guid>(type: "uuid", nullable: false),
                omschrijving = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                omschrijvinggeneriek = table.Column<int>(type: "integer", nullable: false),
                begingeldigheid = table.Column<DateOnly>(type: "date", nullable: true),
                eindegeldigheid = table.Column<DateOnly>(type: "date", nullable: true),
                beginObject = table.Column<DateOnly>(type: "date", nullable: true),
                eindeObject = table.Column<DateOnly>(type: "date", nullable: true),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_roltypen", x => x.id);
                table.ForeignKey(
                    name: "FK_roltypen_zaaktypen_zaaktype_id",
                    column: x => x.zaaktype_id,
                    principalTable: "zaaktypen",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "statustypen",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                omschrijving = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                omschrijvinggeneriek = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                statustekst = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                volgnummer = table.Column<int>(type: "integer", nullable: false),
                informeren = table.Column<bool>(type: "boolean", nullable: false),
                doorlooptijd = table.Column<Period>(type: "interval", nullable: true),
                toelichting = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                checklistitemStatustype = table.Column<CheckListItemStatusType[]>(type: "jsonb", nullable: true),
                begingeldigheid = table.Column<DateOnly>(type: "date", nullable: true),
                eindegeldigheid = table.Column<DateOnly>(type: "date", nullable: true),
                beginObject = table.Column<DateOnly>(type: "date", nullable: true),
                eindeObject = table.Column<DateOnly>(type: "date", nullable: true),
                zaaktype_id = table.Column<Guid>(type: "uuid", nullable: false),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_statustypen", x => x.id);
                table.ForeignKey(
                    name: "FK_statustypen_zaaktypen_zaaktype_id",
                    column: x => x.zaaktype_id,
                    principalTable: "zaaktypen",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "zaakobjecttypen",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                anderobjecttype = table.Column<bool>(type: "boolean", nullable: false),
                begingeldigheid = table.Column<DateOnly>(type: "date", nullable: false),
                eindegeldigheid = table.Column<DateOnly>(type: "date", nullable: true),
                beginobject = table.Column<DateOnly>(type: "date", nullable: true),
                eindeobject = table.Column<DateOnly>(type: "date", nullable: true),
                objecttype = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                relatieomschrijving = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                zaaktype_id = table.Column<Guid>(type: "uuid", nullable: false),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_zaakobjecttypen", x => x.id);
                table.ForeignKey(
                    name: "FK_zaakobjecttypen_zaaktypen_zaaktype_id",
                    column: x => x.zaaktype_id,
                    principalTable: "zaaktypen",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "zaaktypebesluittypen",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                zaaktype_id = table.Column<Guid>(type: "uuid", nullable: false),
                besluittype_omschrijving = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_zaaktypebesluittypen", x => x.id);
                table.ForeignKey(
                    name: "FK_zaaktypebesluittypen_zaaktypen_zaaktype_id",
                    column: x => x.zaaktype_id,
                    principalTable: "zaaktypen",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "zaaktypedeelzaaktypen",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                zaaktype_id = table.Column<Guid>(type: "uuid", nullable: false),
                deelzaaktype_identificatie = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_zaaktypedeelzaaktypen", x => x.id);
                table.ForeignKey(
                    name: "FK_zaaktypedeelzaaktypen_zaaktypen_zaaktype_id",
                    column: x => x.zaaktype_id,
                    principalTable: "zaaktypen",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "zaaktypegerelateerdezaaktypen",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                zaaktype_id = table.Column<Guid>(type: "uuid", nullable: false),
                aardrelatie = table.Column<short>(type: "smallint", nullable: false),
                toelichting = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                gerelateerdezaaktype_identificatie = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_zaaktypegerelateerdezaaktypen", x => x.id);
                table.ForeignKey(
                    name: "FK_zaaktypegerelateerdezaaktypen_zaaktypen_zaaktype_id",
                    column: x => x.zaaktype_id,
                    principalTable: "zaaktypen",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "brondatumarchiefproceduren",
            columns: table => new
            {
                resulttype_id = table.Column<Guid>(type: "uuid", nullable: false),
                afleidingswijze = table.Column<int>(type: "integer", nullable: false),
                datumkenmerk = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                einddatumbekend = table.Column<bool>(type: "boolean", nullable: false),
                objecttype = table.Column<int>(type: "integer", nullable: true),
                registratie = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                procestermijn = table.Column<Period>(type: "interval", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_brondatumarchiefproceduren", x => x.resulttype_id);
                table.ForeignKey(
                    name: "FK_brondatumarchiefproceduren_resultaattypen_resulttype_id",
                    column: x => x.resulttype_id,
                    principalTable: "resultaattypen",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "resultaattypebesluittypen",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                resultaattype_id = table.Column<Guid>(type: "uuid", nullable: false),
                besluittype_omschrijving = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_resultaattypebesluittypen", x => x.id);
                table.ForeignKey(
                    name: "FK_resultaattypebesluittypen_resultaattypen_resultaattype_id",
                    column: x => x.resultaattype_id,
                    principalTable: "resultaattypen",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "eigenschappen",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                namm = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                definitie = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                toelichting = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                zaaktype_id = table.Column<Guid>(type: "uuid", nullable: false),
                statustype_id = table.Column<Guid>(type: "uuid", nullable: true),
                begingeldigheid = table.Column<DateOnly>(type: "date", nullable: true),
                eindegeldigheid = table.Column<DateOnly>(type: "date", nullable: true),
                beginObject = table.Column<DateOnly>(type: "date", nullable: true),
                eindeObject = table.Column<DateOnly>(type: "date", nullable: true),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_eigenschappen", x => x.id);
                table.ForeignKey(
                    name: "FK_eigenschappen_statustypen_statustype_id",
                    column: x => x.statustype_id,
                    principalTable: "statustypen",
                    principalColumn: "id"
                );
                table.ForeignKey(
                    name: "FK_eigenschappen_zaaktypen_zaaktype_id",
                    column: x => x.zaaktype_id,
                    principalTable: "zaaktypen",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "zaaktypeinformatieobjecttypen",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                richting = table.Column<int>(type: "integer", maxLength: 50, nullable: false),
                volgnummer = table.Column<int>(type: "integer", maxLength: 3, nullable: false),
                zaaktype_id = table.Column<Guid>(type: "uuid", nullable: false),
                informatieobjecttype_omschrijving = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                statustype_id = table.Column<Guid>(type: "uuid", nullable: true),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_zaaktypeinformatieobjecttypen", x => x.id);
                table.ForeignKey(
                    name: "FK_zaaktypeinformatieobjecttypen_statustypen_statustype_id",
                    column: x => x.statustype_id,
                    principalTable: "statustypen",
                    principalColumn: "id"
                );
                table.ForeignKey(
                    name: "FK_zaaktypeinformatieobjecttypen_zaaktypen_zaaktype_id",
                    column: x => x.zaaktype_id,
                    principalTable: "zaaktypen",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "eigenschappen_specificaties",
            columns: table => new
            {
                eigenschap_id = table.Column<Guid>(type: "uuid", nullable: false),
                groep = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                formaat = table.Column<int>(type: "integer", nullable: false),
                lengte = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                kardinaliteit = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                waardenverzameling = table.Column<List<string>>(type: "text[]", nullable: true),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_eigenschappen_specificaties", x => x.eigenschap_id);
                table.ForeignKey(
                    name: "FK_eigenschappen_specificaties_eigenschappen_eigenschap_id",
                    column: x => x.eigenschap_id,
                    principalTable: "eigenschappen",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "statustype_verplichte_eigenschappen",
            columns: table => new
            {
                eigenschap_id = table.Column<Guid>(type: "uuid", nullable: false),
                status_id = table.Column<Guid>(type: "uuid", nullable: false),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_statustype_verplichte_eigenschappen", x => new { x.status_id, x.eigenschap_id });
                table.ForeignKey(
                    name: "FK_statustype_verplichte_eigenschappen_eigenschappen_eigenscha~",
                    column: x => x.eigenschap_id,
                    principalTable: "eigenschappen",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
                table.ForeignKey(
                    name: "FK_statustype_verplichte_eigenschappen_statustypen_status_id",
                    column: x => x.status_id,
                    principalTable: "statustypen",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateIndex(
            name: "IX_besluittypeinformatieobjecttypen_besluittype_id_informatieo~",
            table: "besluittypeinformatieobjecttypen",
            columns: ["besluittype_id", "informatieobjecttype_omschrijving"],
            unique: true
        );

        migrationBuilder.CreateIndex(name: "IX_besluittypen_catalogus_id", table: "besluittypen", column: "catalogus_id");

        migrationBuilder.CreateIndex(name: "IX_catalogussen_owner_domein", table: "catalogussen", columns: ["owner", "domein"], unique: true);

        migrationBuilder.CreateIndex(name: "IX_catalogussen_rsin_domein", table: "catalogussen", columns: ["rsin", "domein"], unique: true);

        migrationBuilder.CreateIndex(name: "IX_eigenschappen_statustype_id", table: "eigenschappen", column: "statustype_id");

        migrationBuilder.CreateIndex(name: "IX_eigenschappen_zaaktype_id", table: "eigenschappen", column: "zaaktype_id");

        migrationBuilder.CreateIndex(name: "IX_informatieobjecttypen_catalogus_id", table: "informatieobjecttypen", column: "catalogus_id");

        migrationBuilder.CreateIndex(name: "IX_informatieobjecttypen_concept", table: "informatieobjecttypen", column: "concept");

        migrationBuilder.CreateIndex(
            name: "IX_informatieobjecttypen_concept_omschrijving_begingeldigheid",
            table: "informatieobjecttypen",
            columns: ["concept", "omschrijving", "begingeldigheid"]
        );

        migrationBuilder.CreateIndex(name: "IX_informatieobjecttypen_creationtime", table: "informatieobjecttypen", column: "creationtime");

        migrationBuilder.CreateIndex(name: "IX_informatieobjecttypen_omschrijving", table: "informatieobjecttypen", column: "omschrijving");

        migrationBuilder.CreateIndex(name: "IX_informatieobjecttypen_owner", table: "informatieobjecttypen", column: "owner");

        migrationBuilder.CreateIndex(
            name: "IX_resultaattypebesluittypen_resultaattype_id",
            table: "resultaattypebesluittypen",
            column: "resultaattype_id"
        );

        migrationBuilder.CreateIndex(name: "IX_resultaattypen_zaaktype_id", table: "resultaattypen", column: "zaaktype_id");

        migrationBuilder.CreateIndex(name: "IX_roltypen_zaaktype_id", table: "roltypen", column: "zaaktype_id");

        migrationBuilder.CreateIndex(
            name: "IX_statustype_verplichte_eigenschappen_eigenschap_id",
            table: "statustype_verplichte_eigenschappen",
            column: "eigenschap_id"
        );

        migrationBuilder.CreateIndex(name: "IX_statustypen_zaaktype_id", table: "statustypen", column: "zaaktype_id");

        migrationBuilder.CreateIndex(name: "IX_zaakobjecttypen_anderobjecttype", table: "zaakobjecttypen", column: "anderobjecttype");

        migrationBuilder.CreateIndex(name: "IX_zaakobjecttypen_begingeldigheid", table: "zaakobjecttypen", column: "begingeldigheid");

        migrationBuilder.CreateIndex(name: "IX_zaakobjecttypen_eindegeldigheid", table: "zaakobjecttypen", column: "eindegeldigheid");

        migrationBuilder.CreateIndex(name: "IX_zaakobjecttypen_objecttype", table: "zaakobjecttypen", column: "objecttype");

        migrationBuilder.CreateIndex(name: "IX_zaakobjecttypen_owner", table: "zaakobjecttypen", column: "owner");

        migrationBuilder.CreateIndex(name: "IX_zaakobjecttypen_relatieomschrijving", table: "zaakobjecttypen", column: "relatieomschrijving");

        migrationBuilder.CreateIndex(name: "IX_zaakobjecttypen_zaaktype_id", table: "zaakobjecttypen", column: "zaaktype_id");

        migrationBuilder.CreateIndex(
            name: "IX_zaaktypebesluittypen_zaaktype_id_besluittype_omschrijving",
            table: "zaaktypebesluittypen",
            columns: ["zaaktype_id", "besluittype_omschrijving"],
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_zaaktypedeelzaaktypen_zaaktype_id_deelzaaktype_identificatie",
            table: "zaaktypedeelzaaktypen",
            columns: ["zaaktype_id", "deelzaaktype_identificatie"],
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_zaaktypegerelateerdezaaktypen_zaaktype_id_gerelateerdezaakt~",
            table: "zaaktypegerelateerdezaaktypen",
            columns: ["zaaktype_id", "gerelateerdezaaktype_identificatie"],
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_zaaktypeinformatieobjecttypen_statustype_id",
            table: "zaaktypeinformatieobjecttypen",
            column: "statustype_id"
        );

        migrationBuilder.CreateIndex(
            name: "IX_zaaktypeinformatieobjecttypen_zaaktype_id_informatieobjectt~",
            table: "zaaktypeinformatieobjecttypen",
            columns: ["zaaktype_id", "informatieobjecttype_omschrijving", "volgnummer", "richting"],
            unique: true
        );

        migrationBuilder.CreateIndex(name: "IX_zaaktypen_catalogus_id", table: "zaaktypen", column: "catalogus_id");

        migrationBuilder.CreateIndex(name: "IX_zaaktypen_creationtime", table: "zaaktypen", column: "creationtime");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "audittrail");

        migrationBuilder.DropTable(name: "besluittypeinformatieobjecttypen");

        migrationBuilder.DropTable(name: "brondatumarchiefproceduren");

        migrationBuilder.DropTable(name: "eigenschappen_specificaties");

        migrationBuilder.DropTable(name: "finished_data_migrations");

        migrationBuilder.DropTable(name: "informatieobjecttypen");

        migrationBuilder.DropTable(name: "referentieprocessen");

        migrationBuilder.DropTable(name: "resultaattypebesluittypen");

        migrationBuilder.DropTable(name: "roltypen");

        migrationBuilder.DropTable(name: "statustype_verplichte_eigenschappen");

        migrationBuilder.DropTable(name: "zaakobjecttypen");

        migrationBuilder.DropTable(name: "zaaktypebesluittypen");

        migrationBuilder.DropTable(name: "zaaktypedeelzaaktypen");

        migrationBuilder.DropTable(name: "zaaktypegerelateerdezaaktypen");

        migrationBuilder.DropTable(name: "zaaktypeinformatieobjecttypen");

        migrationBuilder.DropTable(name: "besluittypen");

        migrationBuilder.DropTable(name: "resultaattypen");

        migrationBuilder.DropTable(name: "eigenschappen");

        migrationBuilder.DropTable(name: "statustypen");

        migrationBuilder.DropTable(name: "zaaktypen");

        migrationBuilder.DropTable(name: "catalogussen");
    }
}
