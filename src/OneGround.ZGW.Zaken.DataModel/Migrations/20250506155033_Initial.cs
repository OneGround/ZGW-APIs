using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OneGround.ZGW.Zaken.DataModel.Migrations;

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
            name: "contactpersoonrollen",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                emailadres = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                functie = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                telefoonnummer = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                naam = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_contactpersoonrollen", x => x.id);
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
            name: "subverblijfbuitenland",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                lndlandcode = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                lndlandnaam = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                subadresbuitenland1 = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: true),
                subadresbuitenland2 = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: true),
                subadresbuitenland3 = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_subverblijfbuitenland", x => x.id);
            }
        );

        migrationBuilder.CreateTable(
            name: "verblijfsadresen",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                aoaidentificatie = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                wplwoonplaatsnaam = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                goropenbareruimtenaam = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                aoapostcode = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                aoahuisnummer = table.Column<int>(type: "integer", nullable: false),
                aoahuisletter = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                aoahuisnummertoevoeging = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                inplocatiebeschrijving = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_verblijfsadresen", x => x.id);
            }
        );

        migrationBuilder.CreateTable(
            name: "woz_object_aanduidingen",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                aoaidentificatie = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                wplwoonplaatsnaam = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                goropenbareruimtenaam = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                aoapostcode = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                aoahuisnummer = table.Column<int>(type: "integer", nullable: false),
                aoahuisletter = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                aoahuisnummertoevoeging = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                locatieomschrijving = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_woz_object_aanduidingen", x => x.id);
            }
        );

        migrationBuilder.CreateTable(
            name: "zaken",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                identificatie = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                bronorganisatie = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                omschrijving = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                toelichting = table.Column<string>(type: "text", nullable: true),
                zaaktype = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, collation: "ci_collation"),
                registratiedatum = table.Column<DateOnly>(type: "date", nullable: true),
                verantwoordelijkeorganisatie = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                einddatum = table.Column<DateOnly>(type: "date", nullable: true),
                einddatumgepland = table.Column<DateOnly>(type: "date", nullable: true),
                uiterlijkeeinddatumafdoening = table.Column<DateOnly>(type: "date", nullable: true),
                publicatiedatum = table.Column<DateOnly>(type: "date", nullable: true),
                communicatiekanaal = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                betalingsindicatieweergave = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                laatstebetaaldatum = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                selectielijstklasse = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                hoofdzaak_id = table.Column<Guid>(type: "uuid", nullable: true),
                archiefnominatie = table.Column<short>(type: "smallint", maxLength: 50, nullable: true),
                archiefactiedatum = table.Column<DateOnly>(type: "date", nullable: true),
                archiefstatus = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                startdatum = table.Column<DateOnly>(type: "date", nullable: false),
                betalingsindicatie = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                vertrouwelijkheidaanduiding = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                productenofdiensten = table.Column<List<string>>(type: "text[]", nullable: true),
                zaakgeometrie = table.Column<Geometry>(type: "geometry", nullable: true),
                opdrachtgevendeorganisatie = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                processobjectaard = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                startdatumbewaartermijn = table.Column<DateOnly>(type: "date", nullable: true),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_zaken", x => x.id);
                table.ForeignKey(name: "FK_zaken_zaken_hoofdzaak_id", column: x => x.hoofdzaak_id, principalTable: "zaken", principalColumn: "id");
            }
        );

        migrationBuilder.CreateTable(
            name: "woz_objecten",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                wozobjectnummer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                aanduidingwozobject_id = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_woz_objecten", x => x.id);
                table.ForeignKey(
                    name: "FK_woz_objecten_woz_object_aanduidingen_aanduidingwozobject_id",
                    column: x => x.aanduidingwozobject_id,
                    principalTable: "woz_object_aanduidingen",
                    principalColumn: "id"
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "klantcontacten",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                identificatie = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                datum_tijd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                kanaal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                onderwerp = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                toelichting = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                zaak_id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_klantcontacten", x => x.id);
                table.ForeignKey(
                    name: "FK_klantcontacten_zaken_zaak_id",
                    column: x => x.zaak_id,
                    principalTable: "zaken",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "relevanteanderezaken",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                zaak_id = table.Column<Guid>(type: "uuid", nullable: false),
                aardrelatie = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_relevanteanderezaken", x => x.id);
                table.ForeignKey(
                    name: "FK_relevanteanderezaken_zaken_zaak_id",
                    column: x => x.zaak_id,
                    principalTable: "zaken",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "zaakbesluiten",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                zaak_id = table.Column<Guid>(type: "uuid", nullable: false),
                besluit = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_zaakbesluiten", x => x.id);
                table.ForeignKey(
                    name: "FK_zaakbesluiten_zaken_zaak_id",
                    column: x => x.zaak_id,
                    principalTable: "zaken",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "zaakcontactmomenten",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                zaak_id = table.Column<Guid>(type: "uuid", nullable: false),
                contactmoment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true, collation: "ci_collation"),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_zaakcontactmomenten", x => x.id);
                table.ForeignKey(
                    name: "FK_zaakcontactmomenten_zaken_zaak_id",
                    column: x => x.zaak_id,
                    principalTable: "zaken",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "zaakeigenschappen",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                zaak_id = table.Column<Guid>(type: "uuid", nullable: false),
                eigenschap = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                naam = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                waarde = table.Column<string>(type: "text", nullable: false),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_zaakeigenschappen", x => x.id);
                table.ForeignKey(
                    name: "FK_zaakeigenschappen_zaken_zaak_id",
                    column: x => x.zaak_id,
                    principalTable: "zaken",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "zaakkenmerken",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                zaak_id = table.Column<Guid>(type: "uuid", nullable: false),
                kenmerk = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                bron = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_zaakkenmerken", x => x.id);
                table.ForeignKey(
                    name: "FK_zaakkenmerken_zaken_zaak_id",
                    column: x => x.zaak_id,
                    principalTable: "zaken",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "zaakobjecten",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                zaak_id = table.Column<Guid>(type: "uuid", nullable: false),
                @object = table.Column<string>(
                    name: "object",
                    type: "character varying(1000)",
                    maxLength: 1000,
                    nullable: true,
                    collation: "ci_collation"
                ),
                zaakobjecttype = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                objecttype = table.Column<short>(type: "smallint", nullable: false),
                objecttypeoverige = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                relatieomschrijving = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_zaakobjecten", x => x.id);
                table.ForeignKey(
                    name: "FK_zaakobjecten_zaken_zaak_id",
                    column: x => x.zaak_id,
                    principalTable: "zaken",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "zaakopschortingen",
            columns: table => new
            {
                zaak_id = table.Column<Guid>(type: "uuid", nullable: false),
                indicatie = table.Column<bool>(type: "boolean", nullable: false),
                reden = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_zaakopschortingen", x => x.zaak_id);
                table.ForeignKey(
                    name: "FK_zaakopschortingen_zaken_zaak_id",
                    column: x => x.zaak_id,
                    principalTable: "zaken",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "zaakprocessobjecten",
            columns: table => new
            {
                zaak_id = table.Column<Guid>(type: "uuid", nullable: false),
                datumkenmerk = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                identificatie = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                objecttype = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                registratie = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_zaakprocessobjecten", x => x.zaak_id);
                table.ForeignKey(
                    name: "FK_zaakprocessobjecten_zaken_zaak_id",
                    column: x => x.zaak_id,
                    principalTable: "zaken",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "zaakresultaten",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                zaak_id = table.Column<Guid>(type: "uuid", nullable: false),
                resultaattype = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true, collation: "ci_collation"),
                toelichting = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_zaakresultaten", x => x.id);
                table.ForeignKey(
                    name: "FK_zaakresultaten_zaken_zaak_id",
                    column: x => x.zaak_id,
                    principalTable: "zaken",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "zaakrollen",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                zaak_id = table.Column<Guid>(type: "uuid", nullable: false),
                betrokkene = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true, collation: "ci_collation"),
                betrokkenetype = table.Column<short>(type: "smallint", nullable: false),
                afwijkendeNaamBetrokkene = table.Column<string>(type: "character varying(625)", maxLength: 625, nullable: true),
                roltype = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true, collation: "ci_collation"),
                roltoelichting = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                registratiedatum = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                omschrijving = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                omschrijvinggeneriek = table.Column<short>(type: "smallint", nullable: false),
                indicatiemachtiging = table.Column<short>(type: "smallint", nullable: true),
                contactpersoonrol_id = table.Column<Guid>(type: "uuid", nullable: true),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_zaakrollen", x => x.id);
                table.ForeignKey(
                    name: "FK_zaakrollen_contactpersoonrollen_contactpersoonrol_id",
                    column: x => x.contactpersoonrol_id,
                    principalTable: "contactpersoonrollen",
                    principalColumn: "id"
                );
                table.ForeignKey(
                    name: "FK_zaakrollen_zaken_zaak_id",
                    column: x => x.zaak_id,
                    principalTable: "zaken",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "zaakstatussen",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                zaak_id = table.Column<Guid>(type: "uuid", nullable: false),
                statustype = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, collation: "ci_collation"),
                datumstatusgezet = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                statustoelichting = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                indicatielaatstgezettestatus = table.Column<bool>(type: "boolean", nullable: true),
                gezetdoor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_zaakstatussen", x => x.id);
                table.ForeignKey(
                    name: "FK_zaakstatussen_zaken_zaak_id",
                    column: x => x.zaak_id,
                    principalTable: "zaken",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "zaakverlengingen",
            columns: table => new
            {
                zaak_id = table.Column<Guid>(type: "uuid", nullable: false),
                creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                reden = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                duur = table.Column<Period>(type: "interval", nullable: false),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_zaakverlengingen", x => x.zaak_id);
                table.ForeignKey(
                    name: "FK_zaakverlengingen_zaken_zaak_id",
                    column: x => x.zaak_id,
                    principalTable: "zaken",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "zaakverzoeken",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                zaak_id = table.Column<Guid>(type: "uuid", nullable: false),
                verzoek = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true, collation: "ci_collation"),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_zaakverzoeken", x => x.id);
                table.ForeignKey(
                    name: "FK_zaakverzoeken_zaken_zaak_id",
                    column: x => x.zaak_id,
                    principalTable: "zaken",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "objecttype_overige_definities",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                schema = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                objectdata = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                zaakobject_id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_objecttype_overige_definities", x => x.id);
                table.ForeignKey(
                    name: "FK_objecttype_overige_definities_zaakobjecten_zaakobject_id",
                    column: x => x.zaakobject_id,
                    principalTable: "zaakobjecten",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "zaakobjecten_adressen",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                identificatie = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                wplwoonplaatsnaam = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                goropenbareruimtenaam = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                huisnummer = table.Column<int>(type: "integer", nullable: false),
                huisletter = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                huisnummertoevoeging = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                postcode = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                zaakobject_id = table.Column<Guid>(type: "uuid", nullable: false),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_zaakobjecten_adressen", x => x.id);
                table.ForeignKey(
                    name: "FK_zaakobjecten_adressen_zaakobjecten_zaakobject_id",
                    column: x => x.zaakobject_id,
                    principalTable: "zaakobjecten",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "zaakobjecten_buurten",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                buurtcode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                buurtnaam = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                gemgemeentecode = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                wykwijkcode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                zaakobject_id = table.Column<Guid>(type: "uuid", nullable: false),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_zaakobjecten_buurten", x => x.id);
                table.ForeignKey(
                    name: "FK_zaakobjecten_buurten_zaakobjecten_zaakobject_id",
                    column: x => x.zaakobject_id,
                    principalTable: "zaakobjecten",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "zaakobjecten_gemeenten",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                gemeentenaam = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                gemeentecode = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                zaakobject_id = table.Column<Guid>(type: "uuid", nullable: false),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_zaakobjecten_gemeenten", x => x.id);
                table.ForeignKey(
                    name: "FK_zaakobjecten_gemeenten_zaakobjecten_zaakobject_id",
                    column: x => x.zaakobject_id,
                    principalTable: "zaakobjecten",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "zaakobjecten_kadastrale_onroerende_zaken",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                kadastraleidentificatie = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                kadastraleaanduiding = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                zaakobject_id = table.Column<Guid>(type: "uuid", nullable: false),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_zaakobjecten_kadastrale_onroerende_zaken", x => x.id);
                table.ForeignKey(
                    name: "FK_zaakobjecten_kadastrale_onroerende_zaken_zaakobjecten_zaako~",
                    column: x => x.zaakobject_id,
                    principalTable: "zaakobjecten",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "zaakobjecten_overigen",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                overigedata = table.Column<string>(type: "text", nullable: false),
                zaakobject_id = table.Column<Guid>(type: "uuid", nullable: false),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_zaakobjecten_overigen", x => x.id);
                table.ForeignKey(
                    name: "FK_zaakobjecten_overigen_zaakobjecten_zaakobject_id",
                    column: x => x.zaakobject_id,
                    principalTable: "zaakobjecten",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "zaakobjecten_panden",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                identificatie = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                zaakobject_id = table.Column<Guid>(type: "uuid", nullable: false),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_zaakobjecten_panden", x => x.id);
                table.ForeignKey(
                    name: "FK_zaakobjecten_panden_zaakobjecten_zaakobject_id",
                    column: x => x.zaakobject_id,
                    principalTable: "zaakobjecten",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "zaakobjecten_terreingebouwdobjectzaakobjecten",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                identificatie = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                adresaanduidinggrp_numidentificatie = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                adresaanduidinggrp_oaoidentificatie = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                adresaanduidinggrp_wplwoonplaatsnaam = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                adresaanduidinggrp_goropenbareruimtenaam = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                adresaanduidinggrp_aoapostcode = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                adresaanduidinggrp_aoahuisnummer = table.Column<int>(type: "integer", nullable: false),
                adresaanduidinggrp_aoahuisletter = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                adresaanduidinggrp_aoahuisnummertoevoeging = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                adresaanduidinggrp_ogolocatieaanduiding = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                zaakobject_id = table.Column<Guid>(type: "uuid", nullable: false),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_zaakobjecten_terreingebouwdobjectzaakobjecten", x => x.id);
                table.ForeignKey(
                    name: "FK_zaakobjecten_terreingebouwdobjectzaakobjecten_zaakobjecten_~",
                    column: x => x.zaakobject_id,
                    principalTable: "zaakobjecten",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "zaakobjecten_woz_waarden",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                waardepeildatum = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                woz_object_id = table.Column<Guid>(type: "uuid", nullable: true),
                zaakobject_id = table.Column<Guid>(type: "uuid", nullable: false),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_zaakobjecten_woz_waarden", x => x.id);
                table.ForeignKey(
                    name: "FK_zaakobjecten_woz_waarden_woz_objecten_woz_object_id",
                    column: x => x.woz_object_id,
                    principalTable: "woz_objecten",
                    principalColumn: "id"
                );
                table.ForeignKey(
                    name: "FK_zaakobjecten_woz_waarden_zaakobjecten_zaakobject_id",
                    column: x => x.zaakobject_id,
                    principalTable: "zaakobjecten",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "zaakrollen_medewerkers",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                identificatie = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true),
                achternaam = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                voorletters = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                voorvoegselachternaam = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                zaakrol_id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_zaakrollen_medewerkers", x => x.id);
                table.ForeignKey(
                    name: "FK_zaakrollen_medewerkers_zaakrollen_zaakrol_id",
                    column: x => x.zaakrol_id,
                    principalTable: "zaakrollen",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "zaakrollen_natuurlijk_personen",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                inpbsn = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                anpidentificatie = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: true),
                inpanummer = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                geslachtsnaam = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                voorvoegselgeslachtsnaam = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                voorletters = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                voornamen = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                geslachtsaanduiding = table.Column<short>(type: "smallint", nullable: true),
                geboortedatum = table.Column<DateTime>(type: "date", nullable: true),
                verblijfsadres_id = table.Column<Guid>(type: "uuid", nullable: true),
                subverblijfbuitenland_id = table.Column<Guid>(type: "uuid", nullable: true),
                zaakrol_id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_zaakrollen_natuurlijk_personen", x => x.id);
                table.ForeignKey(
                    name: "FK_zaakrollen_natuurlijk_personen_subverblijfbuitenland_subver~",
                    column: x => x.subverblijfbuitenland_id,
                    principalTable: "subverblijfbuitenland",
                    principalColumn: "id"
                );
                table.ForeignKey(
                    name: "FK_zaakrollen_natuurlijk_personen_verblijfsadresen_verblijfsad~",
                    column: x => x.verblijfsadres_id,
                    principalTable: "verblijfsadresen",
                    principalColumn: "id"
                );
                table.ForeignKey(
                    name: "FK_zaakrollen_natuurlijk_personen_zaakrollen_zaakrol_id",
                    column: x => x.zaakrol_id,
                    principalTable: "zaakrollen",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "zaakrollen_niet_natuurlijk_personen",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                innnnpid = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                annidentificatie = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: true),
                statutairenaam = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                innrechtsvorm = table.Column<short>(type: "smallint", nullable: true),
                bezoekadres = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                subverblijfbuitenland_id = table.Column<Guid>(type: "uuid", nullable: true),
                zaakrol_id = table.Column<Guid>(type: "uuid", nullable: false),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_zaakrollen_niet_natuurlijk_personen", x => x.id);
                table.ForeignKey(
                    name: "FK_zaakrollen_niet_natuurlijk_personen_subverblijfbuitenland_s~",
                    column: x => x.subverblijfbuitenland_id,
                    principalTable: "subverblijfbuitenland",
                    principalColumn: "id"
                );
                table.ForeignKey(
                    name: "FK_zaakrollen_niet_natuurlijk_personen_zaakrollen_zaakrol_id",
                    column: x => x.zaakrol_id,
                    principalTable: "zaakrollen",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "zaakrollen_organisatorische_eenheden",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                identificatie = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true),
                naam = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                isgehuisvestin = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true),
                zaakrol_id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_zaakrollen_organisatorische_eenheden", x => x.id);
                table.ForeignKey(
                    name: "FK_zaakrollen_organisatorische_eenheden_zaakrollen_zaakrol_id",
                    column: x => x.zaakrol_id,
                    principalTable: "zaakrollen",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "zaakrollen_vestigingen",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                vestigingsnummer = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true),
                handelsnaam = table.Column<List<string>>(type: "text[]", nullable: true),
                verblijfsadres_id = table.Column<Guid>(type: "uuid", nullable: true),
                subverblijfbuitenland_id = table.Column<Guid>(type: "uuid", nullable: true),
                zaakrol_id = table.Column<Guid>(type: "uuid", nullable: false),
                kvknummer = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_zaakrollen_vestigingen", x => x.id);
                table.ForeignKey(
                    name: "FK_zaakrollen_vestigingen_subverblijfbuitenland_subverblijfbui~",
                    column: x => x.subverblijfbuitenland_id,
                    principalTable: "subverblijfbuitenland",
                    principalColumn: "id"
                );
                table.ForeignKey(
                    name: "FK_zaakrollen_vestigingen_verblijfsadresen_verblijfsadres_id",
                    column: x => x.verblijfsadres_id,
                    principalTable: "verblijfsadresen",
                    principalColumn: "id"
                );
                table.ForeignKey(
                    name: "FK_zaakrollen_vestigingen_zaakrollen_zaakrol_id",
                    column: x => x.zaakrol_id,
                    principalTable: "zaakrollen",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "zaakinformatieobjecten",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                zaak_id = table.Column<Guid>(type: "uuid", nullable: false),
                informatieobject = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, collation: "ci_collation"),
                aardrelatieweergave = table.Column<short>(type: "smallint", nullable: false),
                registratiedatum = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                titel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                beschrijving = table.Column<string>(type: "text", nullable: true),
                vernietigingsdatum = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                status_id = table.Column<Guid>(type: "uuid", nullable: true),
                owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_zaakinformatieobjecten", x => x.id);
                table.ForeignKey(
                    name: "FK_zaakinformatieobjecten_zaakstatussen_status_id",
                    column: x => x.status_id,
                    principalTable: "zaakstatussen",
                    principalColumn: "id"
                );
                table.ForeignKey(
                    name: "FK_zaakinformatieobjecten_zaken_zaak_id",
                    column: x => x.zaak_id,
                    principalTable: "zaken",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateIndex(name: "IX_audittrail_hoofdobject_id", table: "audittrail", column: "hoofdobject_id");

        migrationBuilder.CreateIndex(name: "IX_klantcontacten_zaak_id", table: "klantcontacten", column: "zaak_id");

        migrationBuilder.CreateIndex(
            name: "IX_objecttype_overige_definities_zaakobject_id",
            table: "objecttype_overige_definities",
            column: "zaakobject_id",
            unique: true
        );

        migrationBuilder.CreateIndex(name: "IX_relevanteanderezaken_zaak_id", table: "relevanteanderezaken", column: "zaak_id");

        migrationBuilder.CreateIndex(name: "IX_woz_objecten_aanduidingwozobject_id", table: "woz_objecten", column: "aanduidingwozobject_id");

        migrationBuilder.CreateIndex(name: "IX_zaakbesluiten_zaak_id", table: "zaakbesluiten", column: "zaak_id");

        migrationBuilder.CreateIndex(name: "IX_zaakcontactmomenten_contactmoment", table: "zaakcontactmomenten", column: "contactmoment");

        migrationBuilder.CreateIndex(name: "IX_zaakcontactmomenten_owner", table: "zaakcontactmomenten", column: "owner");

        migrationBuilder.CreateIndex(name: "IX_zaakcontactmomenten_zaak_id", table: "zaakcontactmomenten", column: "zaak_id");

        migrationBuilder.CreateIndex(name: "IX_zaakeigenschappen_owner", table: "zaakeigenschappen", column: "owner");

        migrationBuilder.CreateIndex(name: "IX_zaakeigenschappen_zaak_id", table: "zaakeigenschappen", column: "zaak_id");

        migrationBuilder.CreateIndex(name: "IX_zaakinformatieobjecten_informatieobject", table: "zaakinformatieobjecten", column: "informatieobject");

        migrationBuilder.CreateIndex(name: "IX_zaakinformatieobjecten_owner", table: "zaakinformatieobjecten", column: "owner");

        migrationBuilder.CreateIndex(name: "IX_zaakinformatieobjecten_status_id", table: "zaakinformatieobjecten", column: "status_id");

        migrationBuilder.CreateIndex(name: "IX_zaakinformatieobjecten_zaak_id", table: "zaakinformatieobjecten", column: "zaak_id");

        migrationBuilder.CreateIndex(name: "IX_zaakkenmerken_zaak_id", table: "zaakkenmerken", column: "zaak_id");

        migrationBuilder.CreateIndex(name: "IX_zaakobjecten_object", table: "zaakobjecten", column: "object");

        migrationBuilder.CreateIndex(name: "IX_zaakobjecten_objecttype", table: "zaakobjecten", column: "objecttype");

        migrationBuilder.CreateIndex(name: "IX_zaakobjecten_owner", table: "zaakobjecten", column: "owner");

        migrationBuilder.CreateIndex(name: "IX_zaakobjecten_zaak_id", table: "zaakobjecten", column: "zaak_id");

        migrationBuilder.CreateIndex(
            name: "IX_zaakobjecten_adressen_zaakobject_id",
            table: "zaakobjecten_adressen",
            column: "zaakobject_id",
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_zaakobjecten_buurten_zaakobject_id",
            table: "zaakobjecten_buurten",
            column: "zaakobject_id",
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_zaakobjecten_gemeenten_zaakobject_id",
            table: "zaakobjecten_gemeenten",
            column: "zaakobject_id",
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_zaakobjecten_kadastrale_onroerende_zaken_zaakobject_id",
            table: "zaakobjecten_kadastrale_onroerende_zaken",
            column: "zaakobject_id",
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_zaakobjecten_overigen_zaakobject_id",
            table: "zaakobjecten_overigen",
            column: "zaakobject_id",
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_zaakobjecten_panden_zaakobject_id",
            table: "zaakobjecten_panden",
            column: "zaakobject_id",
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_zaakobjecten_terreingebouwdobjectzaakobjecten_zaakobject_id",
            table: "zaakobjecten_terreingebouwdobjectzaakobjecten",
            column: "zaakobject_id",
            unique: true
        );

        migrationBuilder.CreateIndex(name: "IX_zaakobjecten_woz_waarden_woz_object_id", table: "zaakobjecten_woz_waarden", column: "woz_object_id");

        migrationBuilder.CreateIndex(
            name: "IX_zaakobjecten_woz_waarden_zaakobject_id",
            table: "zaakobjecten_woz_waarden",
            column: "zaakobject_id",
            unique: true
        );

        migrationBuilder.CreateIndex(name: "IX_zaakresultaten_owner", table: "zaakresultaten", column: "owner");

        migrationBuilder.CreateIndex(name: "IX_zaakresultaten_resultaattype", table: "zaakresultaten", column: "resultaattype");

        migrationBuilder.CreateIndex(name: "IX_zaakresultaten_zaak_id", table: "zaakresultaten", column: "zaak_id", unique: true);

        migrationBuilder.CreateIndex(name: "IX_zaakrollen_betrokkene", table: "zaakrollen", column: "betrokkene");

        migrationBuilder.CreateIndex(name: "IX_zaakrollen_betrokkenetype", table: "zaakrollen", column: "betrokkenetype");

        migrationBuilder.CreateIndex(name: "IX_zaakrollen_contactpersoonrol_id", table: "zaakrollen", column: "contactpersoonrol_id");

        migrationBuilder.CreateIndex(name: "IX_zaakrollen_omschrijving", table: "zaakrollen", column: "omschrijving");

        migrationBuilder.CreateIndex(name: "IX_zaakrollen_omschrijvinggeneriek", table: "zaakrollen", column: "omschrijvinggeneriek");

        migrationBuilder.CreateIndex(name: "IX_zaakrollen_owner", table: "zaakrollen", column: "owner");

        migrationBuilder.CreateIndex(name: "IX_zaakrollen_roltype", table: "zaakrollen", column: "roltype");

        migrationBuilder.CreateIndex(name: "IX_zaakrollen_zaak_id", table: "zaakrollen", column: "zaak_id");

        migrationBuilder.CreateIndex(name: "IX_zaakrollen_medewerkers_identificatie", table: "zaakrollen_medewerkers", column: "identificatie");

        migrationBuilder.CreateIndex(
            name: "IX_zaakrollen_medewerkers_zaakrol_id",
            table: "zaakrollen_medewerkers",
            column: "zaakrol_id",
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_zaakrollen_natuurlijk_personen_anpidentificatie",
            table: "zaakrollen_natuurlijk_personen",
            column: "anpidentificatie"
        );

        migrationBuilder.CreateIndex(
            name: "IX_zaakrollen_natuurlijk_personen_inpanummer",
            table: "zaakrollen_natuurlijk_personen",
            column: "inpanummer"
        );

        migrationBuilder.CreateIndex(name: "IX_zaakrollen_natuurlijk_personen_inpbsn", table: "zaakrollen_natuurlijk_personen", column: "inpbsn");

        migrationBuilder.CreateIndex(
            name: "IX_zaakrollen_natuurlijk_personen_subverblijfbuitenland_id",
            table: "zaakrollen_natuurlijk_personen",
            column: "subverblijfbuitenland_id"
        );

        migrationBuilder.CreateIndex(
            name: "IX_zaakrollen_natuurlijk_personen_verblijfsadres_id",
            table: "zaakrollen_natuurlijk_personen",
            column: "verblijfsadres_id"
        );

        migrationBuilder.CreateIndex(
            name: "IX_zaakrollen_natuurlijk_personen_zaakrol_id",
            table: "zaakrollen_natuurlijk_personen",
            column: "zaakrol_id",
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_zaakrollen_niet_natuurlijk_personen_annidentificatie",
            table: "zaakrollen_niet_natuurlijk_personen",
            column: "annidentificatie"
        );

        migrationBuilder.CreateIndex(
            name: "IX_zaakrollen_niet_natuurlijk_personen_innnnpid",
            table: "zaakrollen_niet_natuurlijk_personen",
            column: "innnnpid"
        );

        migrationBuilder.CreateIndex(
            name: "IX_zaakrollen_niet_natuurlijk_personen_subverblijfbuitenland_id",
            table: "zaakrollen_niet_natuurlijk_personen",
            column: "subverblijfbuitenland_id"
        );

        migrationBuilder.CreateIndex(
            name: "IX_zaakrollen_niet_natuurlijk_personen_zaakrol_id",
            table: "zaakrollen_niet_natuurlijk_personen",
            column: "zaakrol_id",
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_zaakrollen_organisatorische_eenheden_identificatie",
            table: "zaakrollen_organisatorische_eenheden",
            column: "identificatie"
        );

        migrationBuilder.CreateIndex(
            name: "IX_zaakrollen_organisatorische_eenheden_zaakrol_id",
            table: "zaakrollen_organisatorische_eenheden",
            column: "zaakrol_id",
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_zaakrollen_vestigingen_subverblijfbuitenland_id",
            table: "zaakrollen_vestigingen",
            column: "subverblijfbuitenland_id"
        );

        migrationBuilder.CreateIndex(
            name: "IX_zaakrollen_vestigingen_verblijfsadres_id",
            table: "zaakrollen_vestigingen",
            column: "verblijfsadres_id"
        );

        migrationBuilder.CreateIndex(name: "IX_zaakrollen_vestigingen_vestigingsnummer", table: "zaakrollen_vestigingen", column: "vestigingsnummer");

        migrationBuilder.CreateIndex(
            name: "IX_zaakrollen_vestigingen_zaakrol_id",
            table: "zaakrollen_vestigingen",
            column: "zaakrol_id",
            unique: true
        );

        migrationBuilder.CreateIndex(name: "IX_zaakstatussen_owner", table: "zaakstatussen", column: "owner");

        migrationBuilder.CreateIndex(name: "IX_zaakstatussen_statustype", table: "zaakstatussen", column: "statustype");

        migrationBuilder.CreateIndex(name: "IX_zaakstatussen_zaak_id", table: "zaakstatussen", column: "zaak_id");

        migrationBuilder.CreateIndex(name: "IX_zaakverzoeken_owner", table: "zaakverzoeken", column: "owner");

        migrationBuilder.CreateIndex(name: "IX_zaakverzoeken_verzoek", table: "zaakverzoeken", column: "verzoek");

        migrationBuilder.CreateIndex(name: "IX_zaakverzoeken_zaak_id", table: "zaakverzoeken", column: "zaak_id");

        migrationBuilder.CreateIndex(name: "IX_zaken_archiefactiedatum", table: "zaken", column: "archiefactiedatum");

        migrationBuilder.CreateIndex(name: "IX_zaken_archiefnominatie", table: "zaken", column: "archiefnominatie");

        migrationBuilder.CreateIndex(name: "IX_zaken_archiefstatus", table: "zaken", column: "archiefstatus");

        migrationBuilder.CreateIndex(
            name: "IX_zaken_bronorganisatie_identificatie",
            table: "zaken",
            columns: ["bronorganisatie", "identificatie"],
            unique: true
        );

        migrationBuilder.CreateIndex(name: "IX_zaken_hoofdzaak_id", table: "zaken", column: "hoofdzaak_id");

        migrationBuilder.CreateIndex(name: "IX_zaken_id_hoofdzaak_id", table: "zaken", columns: ["id", "hoofdzaak_id"]);

        migrationBuilder.CreateIndex(name: "IX_zaken_identificatie", table: "zaken", column: "identificatie");

        migrationBuilder.CreateIndex(name: "IX_zaken_owner", table: "zaken", column: "owner");

        migrationBuilder.CreateIndex(name: "IX_zaken_owner_identificatie", table: "zaken", columns: ["owner", "identificatie"]);

        migrationBuilder.CreateIndex(name: "IX_zaken_startdatum", table: "zaken", column: "startdatum");

        migrationBuilder
            .CreateIndex(name: "IX_zaken_zaakgeometrie", table: "zaken", column: "zaakgeometrie")
            .Annotation("Npgsql:IndexMethod", "gist");

        migrationBuilder.CreateIndex(name: "IX_zaken_zaaktype", table: "zaken", column: "zaaktype");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "audittrail");

        migrationBuilder.DropTable(name: "finished_data_migrations");

        migrationBuilder.DropTable(name: "klantcontacten");

        migrationBuilder.DropTable(name: "objecttype_overige_definities");

        migrationBuilder.DropTable(name: "organisatie_nummers");

        migrationBuilder.DropTable(name: "relevanteanderezaken");

        migrationBuilder.DropTable(name: "zaakbesluiten");

        migrationBuilder.DropTable(name: "zaakcontactmomenten");

        migrationBuilder.DropTable(name: "zaakeigenschappen");

        migrationBuilder.DropTable(name: "zaakinformatieobjecten");

        migrationBuilder.DropTable(name: "zaakkenmerken");

        migrationBuilder.DropTable(name: "zaakobjecten_adressen");

        migrationBuilder.DropTable(name: "zaakobjecten_buurten");

        migrationBuilder.DropTable(name: "zaakobjecten_gemeenten");

        migrationBuilder.DropTable(name: "zaakobjecten_kadastrale_onroerende_zaken");

        migrationBuilder.DropTable(name: "zaakobjecten_overigen");

        migrationBuilder.DropTable(name: "zaakobjecten_panden");

        migrationBuilder.DropTable(name: "zaakobjecten_terreingebouwdobjectzaakobjecten");

        migrationBuilder.DropTable(name: "zaakobjecten_woz_waarden");

        migrationBuilder.DropTable(name: "zaakopschortingen");

        migrationBuilder.DropTable(name: "zaakprocessobjecten");

        migrationBuilder.DropTable(name: "zaakresultaten");

        migrationBuilder.DropTable(name: "zaakrollen_medewerkers");

        migrationBuilder.DropTable(name: "zaakrollen_natuurlijk_personen");

        migrationBuilder.DropTable(name: "zaakrollen_niet_natuurlijk_personen");

        migrationBuilder.DropTable(name: "zaakrollen_organisatorische_eenheden");

        migrationBuilder.DropTable(name: "zaakrollen_vestigingen");

        migrationBuilder.DropTable(name: "zaakverlengingen");

        migrationBuilder.DropTable(name: "zaakverzoeken");

        migrationBuilder.DropTable(name: "zaakstatussen");

        migrationBuilder.DropTable(name: "woz_objecten");

        migrationBuilder.DropTable(name: "zaakobjecten");

        migrationBuilder.DropTable(name: "subverblijfbuitenland");

        migrationBuilder.DropTable(name: "verblijfsadresen");

        migrationBuilder.DropTable(name: "zaakrollen");

        migrationBuilder.DropTable(name: "woz_object_aanduidingen");

        migrationBuilder.DropTable(name: "contactpersoonrollen");

        migrationBuilder.DropTable(name: "zaken");
    }
}
