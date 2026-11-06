using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OneGround.ZGW.Gesprekken.DataModel.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
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
                name: "bestanden",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bestandsnaam = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    bestandsomvang = table.Column<long>(type: "bigint", nullable: false),
                    contenttype = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    titel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    sleutel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    aangemaaktop = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    verlooptop = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    geuploadop = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bestanden", x => x.id);
                }
            );

            migrationBuilder.CreateTable(
                name: "events",
                columns: table => new
                {
                    sequence = table
                        .Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    gesprek_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    aanleidingzaak = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    geredigeerd = table.Column<bool>(type: "boolean", nullable: false),
                    data = table.Column<string>(type: "jsonb", nullable: false),
                    owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_events", x => x.sequence);
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
                name: "gesprekken",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    referentie = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    gespreksonderwerp = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    aanleidingzaak = table.Column<string>(
                        type: "character varying(1000)",
                        maxLength: 1000,
                        nullable: true,
                        collation: "ci_collation"
                    ),
                    aanleidingthema = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    aanleidingproductdienst = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    aanleidingagendaafspraak = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    aanleidingplan = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    contactmoment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    aanvangsmomentgesprek = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    momenteindegesprek = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gesprekken", x => x.id);
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
                name: "gespreksdeelnames",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    gesprek_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    partij = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true, collation: "ci_collation"),
                    actor = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true, collation: "ci_collation"),
                    naam = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    namens = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    aanvangsmomentdeelname = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    momenteindedeelname = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gespreksdeelnames", x => x.id);
                    table.ForeignKey(
                        name: "FK_gespreksdeelnames_gesprekken_gesprek_id",
                        column: x => x.gesprek_id,
                        principalTable: "gesprekken",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "gespreksbijdragen",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    gesprek_id = table.Column<Guid>(type: "uuid", nullable: false),
                    verzender_id = table.Column<Guid>(type: "uuid", nullable: false),
                    isreactieop_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tekstgespreksbijdrage = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    automatisch = table.Column<bool>(type: "boolean", nullable: false),
                    leveringsmoment = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gespreksbijdragen", x => x.id);
                    table.ForeignKey(
                        name: "FK_gespreksbijdragen_gesprekken_gesprek_id",
                        column: x => x.gesprek_id,
                        principalTable: "gesprekken",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_gespreksbijdragen_gespreksdeelnames_verzender_id",
                        column: x => x.verzender_id,
                        principalTable: "gespreksdeelnames",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "bijlagen",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    gespreksbijdrage_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bestand_id = table.Column<Guid>(type: "uuid", nullable: false),
                    titel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    bestandsnaam = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    contenttype = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    bestandsomvang = table.Column<long>(type: "bigint", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bijlagen", x => x.id);
                    table.ForeignKey(
                        name: "FK_bijlagen_gespreksbijdragen_gespreksbijdrage_id",
                        column: x => x.gespreksbijdrage_id,
                        principalTable: "gespreksbijdragen",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "gelezengespreksbijdragen",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    gespreksbijdrage_id = table.Column<Guid>(type: "uuid", nullable: false),
                    gespreksdeelname_id = table.Column<Guid>(type: "uuid", nullable: false),
                    momentgelezen = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    creationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modificationtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gelezengespreksbijdragen", x => x.id);
                    table.ForeignKey(
                        name: "FK_gelezengespreksbijdragen_gespreksbijdragen_gespreksbijdrage~",
                        column: x => x.gespreksbijdrage_id,
                        principalTable: "gespreksbijdragen",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(name: "IX_bijlagen_bestand_id", table: "bijlagen", column: "bestand_id", unique: true);

            migrationBuilder.CreateIndex(name: "IX_bijlagen_gespreksbijdrage_id", table: "bijlagen", column: "gespreksbijdrage_id");

            migrationBuilder.CreateIndex(name: "IX_events_aanleidingzaak", table: "events", column: "aanleidingzaak");

            migrationBuilder.CreateIndex(name: "IX_events_event_id", table: "events", column: "event_id", unique: true);

            migrationBuilder.CreateIndex(name: "IX_events_owner_sequence", table: "events", columns: new[] { "owner", "sequence" });

            migrationBuilder.CreateIndex(
                name: "IX_gelezengespreksbijdragen_gespreksbijdrage_id_gespreksdeelna~",
                table: "gelezengespreksbijdragen",
                columns: new[] { "gespreksbijdrage_id", "gespreksdeelname_id" },
                unique: true
            );

            migrationBuilder.CreateIndex(name: "IX_gesprekken_aanleidingplan", table: "gesprekken", column: "aanleidingplan");

            migrationBuilder.CreateIndex(name: "IX_gesprekken_aanleidingzaak", table: "gesprekken", column: "aanleidingzaak");

            migrationBuilder.CreateIndex(name: "IX_gesprekken_aanvangsmomentgesprek", table: "gesprekken", column: "aanvangsmomentgesprek");

            migrationBuilder.CreateIndex(
                name: "IX_gesprekken_owner_referentie",
                table: "gesprekken",
                columns: new[] { "owner", "referentie" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_gespreksbijdragen_gesprek_id_leveringsmoment",
                table: "gespreksbijdragen",
                columns: new[] { "gesprek_id", "leveringsmoment" }
            );

            migrationBuilder.CreateIndex(name: "IX_gespreksbijdragen_verzender_id", table: "gespreksbijdragen", column: "verzender_id");

            migrationBuilder.CreateIndex(name: "IX_gespreksdeelnames_actor", table: "gespreksdeelnames", column: "actor");

            migrationBuilder.CreateIndex(name: "IX_gespreksdeelnames_gesprek_id", table: "gespreksdeelnames", column: "gesprek_id");

            migrationBuilder.CreateIndex(name: "IX_gespreksdeelnames_partij", table: "gespreksdeelnames", column: "partij");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "bestanden");

            migrationBuilder.DropTable(name: "bijlagen");

            migrationBuilder.DropTable(name: "events");

            migrationBuilder.DropTable(name: "finished_data_migrations");

            migrationBuilder.DropTable(name: "gelezengespreksbijdragen");

            migrationBuilder.DropTable(name: "organisatie_nummers");

            migrationBuilder.DropTable(name: "gespreksbijdragen");

            migrationBuilder.DropTable(name: "gespreksdeelnames");

            migrationBuilder.DropTable(name: "gesprekken");
        }
    }
}
