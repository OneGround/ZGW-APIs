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
                        nullable: false,
                        collation: "ci_collation"
                    ),
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
                    partij = table.Column<Guid>(type: "uuid", nullable: true),
                    actor = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true, collation: "ci_collation"),
                    naam = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    emailadres = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    bsn = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                    rsin = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                    kvknummer = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    vestigingsnummer = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
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
                    tekstgespreksbijdrage = table.Column<string>(type: "text", nullable: false),
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
                    versienummer = table.Column<int>(type: "integer", nullable: false),
                    enkelvoudiginformatieobject = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
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

            migrationBuilder.CreateIndex(name: "IX_bijlagen_gespreksbijdrage_id", table: "bijlagen", column: "gespreksbijdrage_id");

            migrationBuilder.CreateIndex(
                name: "IX_gelezengespreksbijdragen_gespreksbijdrage_id_gespreksdeelna~",
                table: "gelezengespreksbijdragen",
                columns: new[] { "gespreksbijdrage_id", "gespreksdeelname_id" },
                unique: true
            );

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
            migrationBuilder.DropTable(name: "bijlagen");

            migrationBuilder.DropTable(name: "finished_data_migrations");

            migrationBuilder.DropTable(name: "gelezengespreksbijdragen");

            migrationBuilder.DropTable(name: "organisatie_nummers");

            migrationBuilder.DropTable(name: "gespreksbijdragen");

            migrationBuilder.DropTable(name: "gespreksdeelnames");

            migrationBuilder.DropTable(name: "gesprekken");
        }
    }
}
