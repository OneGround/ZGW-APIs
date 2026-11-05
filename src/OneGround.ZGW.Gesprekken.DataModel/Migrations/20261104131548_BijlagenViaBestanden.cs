using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneGround.ZGW.Gesprekken.DataModel.Migrations
{
    /// <inheritdoc />
    public partial class BijlagenViaBestanden : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "enkelvoudiginformatieobject", table: "bijlagen");

            migrationBuilder.DropColumn(name: "versienummer", table: "bijlagen");

            migrationBuilder.AddColumn<Guid>(
                name: "bestand_id",
                table: "bijlagen",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000")
            );

            migrationBuilder.AddColumn<string>(
                name: "bestandsnaam",
                table: "bijlagen",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true
            );

            migrationBuilder.AddColumn<long>(name: "bestandsomvang", table: "bijlagen", type: "bigint", nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "contenttype",
                table: "bijlagen",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(name: "titel", table: "bijlagen", type: "character varying(200)", maxLength: 200, nullable: true);

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

            migrationBuilder.CreateIndex(name: "IX_bijlagen_bestand_id", table: "bijlagen", column: "bestand_id", unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "bestanden");

            migrationBuilder.DropIndex(name: "IX_bijlagen_bestand_id", table: "bijlagen");

            migrationBuilder.DropColumn(name: "bestand_id", table: "bijlagen");

            migrationBuilder.DropColumn(name: "bestandsnaam", table: "bijlagen");

            migrationBuilder.DropColumn(name: "bestandsomvang", table: "bijlagen");

            migrationBuilder.DropColumn(name: "contenttype", table: "bijlagen");

            migrationBuilder.DropColumn(name: "titel", table: "bijlagen");

            migrationBuilder.AddColumn<string>(
                name: "enkelvoudiginformatieobject",
                table: "bijlagen",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<int>(name: "versienummer", table: "bijlagen", type: "integer", nullable: false, defaultValue: 0);
        }
    }
}
