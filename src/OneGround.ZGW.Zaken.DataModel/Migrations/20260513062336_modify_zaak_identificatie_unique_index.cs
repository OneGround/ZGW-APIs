using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneGround.ZGW.Zaken.DataModel.Migrations
{
    /// <inheritdoc />
    public partial class modify_zaak_identificatie_unique_index : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_zaken_bronorganisatie_identificatie", table: "zaken");

            migrationBuilder.DropIndex(name: "IX_zaken_owner_identificatie", table: "zaken");

            migrationBuilder
                .CreateIndex(
                    name: "IX_zaken_owner_identificatie",
                    table: "zaken",
                    columns: new[] { "owner", "identificatie" },
                    unique: true,
                    filter: $"creationtime > '{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}'"
                )
                .Annotation("Npgsql:CreatedConcurrently", true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_zaken_owner_identificatie", table: "zaken");

            migrationBuilder
                .CreateIndex(
                    name: "IX_zaken_bronorganisatie_identificatie",
                    table: "zaken",
                    columns: new[] { "bronorganisatie", "identificatie" },
                    unique: true
                )
                .Annotation("Npgsql:CreatedConcurrently", true);

            migrationBuilder.CreateIndex(name: "IX_zaken_owner_identificatie", table: "zaken", columns: new[] { "owner", "identificatie" });
        }
    }
}
