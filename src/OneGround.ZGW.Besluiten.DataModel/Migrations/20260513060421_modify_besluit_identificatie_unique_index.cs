using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneGround.ZGW.Besluiten.DataModel.Migrations
{
    /// <inheritdoc />
    public partial class modify_besluit_identificatie_unique_index : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_besluiten_owner_identificatie", table: "besluiten");

            migrationBuilder.DropIndex(name: "IX_besluiten_verantwoordelijkeorganisatie_identificatie", table: "besluiten");

            migrationBuilder
                .CreateIndex(
                    name: "IX_besluiten_owner_identificatie",
                    table: "besluiten",
                    columns: new[] { "owner", "identificatie" },
                    unique: true,
                    filter: $"creationtime > '{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}'"
                )
                .Annotation("Npgsql:CreatedConcurrently", true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_besluiten_owner_identificatie", table: "besluiten");

            migrationBuilder.CreateIndex(name: "IX_besluiten_owner_identificatie", table: "besluiten", columns: new[] { "owner", "identificatie" });

            migrationBuilder
                .CreateIndex(
                    name: "IX_besluiten_verantwoordelijkeorganisatie_identificatie",
                    table: "besluiten",
                    columns: new[] { "verantwoordelijkeorganisatie", "identificatie" },
                    unique: true
                )
                .Annotation("Npgsql:CreatedConcurrently", true);
        }
    }
}
