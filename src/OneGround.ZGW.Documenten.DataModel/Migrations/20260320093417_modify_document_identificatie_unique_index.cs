using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneGround.ZGW.Documenten.DataModel.Migrations
{
    /// <inheritdoc />
    public partial class modify_document_identificatie_unique_index : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_enkelvoudiginformatieobjectversies_bronorganisatie_identifi~",
                table: "enkelvoudiginformatieobjectversies"
            );

            migrationBuilder
                .CreateIndex(
                    name: "IX_eiov_owner_identificatie_versie",
                    table: "enkelvoudiginformatieobjectversies",
                    columns: new[] { "owner", "identificatie", "versie" },
                    unique: true,
                    filter: $"creationtime > '{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}'"
                )
                .Annotation("Npgsql:CreatedConcurrently", true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_eiov_owner_identificatie_versie", table: "enkelvoudiginformatieobjectversies");

            migrationBuilder.CreateIndex(
                name: "IX_enkelvoudiginformatieobjectversies_bronorganisatie_identifi~",
                table: "enkelvoudiginformatieobjectversies",
                columns: new[] { "bronorganisatie", "identificatie", "versie" },
                unique: true
            );
        }
    }
}
