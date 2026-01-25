using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneGround.ZGW.Documenten.DataModel.Migrations.DrcDbContext2Migrations
{
    /// <inheritdoc />
    public partial class delete_duplicate_id : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "idx_e0_light_covering", table: "enkelvoudiginformatieobjecten_2");

            migrationBuilder.DropIndex(
                name: "IX_enkelvoudiginformatieobjecten_2_enkelvoudiginformatieobject~",
                table: "enkelvoudiginformatieobjecten_2"
            );

            migrationBuilder.DropIndex(
                name: "IX_enkelvoudiginformatieobjecten_2_owner_enkelvoudiginformatie~",
                table: "enkelvoudiginformatieobjecten_2"
            );

            migrationBuilder.DropIndex(
                name: "IX_enkelvoudiginformatieobjecten_2_owner_inhoud_vertrouwelijk~1",
                table: "enkelvoudiginformatieobjecten_2"
            );

            migrationBuilder.DropColumn(name: "enkelvoudiginformatieobject_id ", table: "enkelvoudiginformatieobjecten_2");

            migrationBuilder.RenameIndex(
                name: "IX_enkelvoudiginformatieobjecten_2_enkelvoudiginformatieobjec~1",
                table: "enkelvoudiginformatieobjecten_2",
                newName: "IX_enkelvoudiginformatieobjecten_2_enkelvoudiginformatieobject~"
            );

            migrationBuilder
                .CreateIndex(name: "idx_e0_light_covering", table: "enkelvoudiginformatieobjecten_2", column: "id")
                .Annotation("Npgsql:IndexInclude", new[] { "owner", "vertrouwelijkheidaanduiding", "enkelvoudiginformatieobject_lock_id" });

            migrationBuilder.CreateIndex(
                name: "IX_enkelvoudiginformatieobjecten_2_owner_enkelvoudiginformatie~",
                table: "enkelvoudiginformatieobjecten_2",
                columns: new[] { "owner", "enkelvoudiginformatieobject_lock_id", "versie", "vertrouwelijkheidaanduiding" },
                descending: new[] { false, false, true, false }
            );

            migrationBuilder
                .CreateIndex(
                    name: "IX_enkelvoudiginformatieobjecten_2_owner_inhoud_vertrouwelijk~1",
                    table: "enkelvoudiginformatieobjecten_2",
                    columns: new[] { "owner", "inhoud", "vertrouwelijkheidaanduiding", "enkelvoudiginformatieobject_lock_id" },
                    descending: new[] { false, false, true, false },
                    filter: "Bestandsomvang IS NOT NULL"
                )
                .Annotation("Npgsql:IndexInclude", new[] { "bestandsomvang" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "idx_e0_light_covering", table: "enkelvoudiginformatieobjecten_2");

            migrationBuilder.DropIndex(
                name: "IX_enkelvoudiginformatieobjecten_2_owner_enkelvoudiginformatie~",
                table: "enkelvoudiginformatieobjecten_2"
            );

            migrationBuilder.DropIndex(
                name: "IX_enkelvoudiginformatieobjecten_2_owner_inhoud_vertrouwelijk~1",
                table: "enkelvoudiginformatieobjecten_2"
            );

            migrationBuilder.RenameIndex(
                name: "IX_enkelvoudiginformatieobjecten_2_enkelvoudiginformatieobject~",
                table: "enkelvoudiginformatieobjecten_2",
                newName: "IX_enkelvoudiginformatieobjecten_2_enkelvoudiginformatieobjec~1"
            );

            migrationBuilder.AddColumn<Guid>(
                name: "enkelvoudiginformatieobject_id ",
                table: "enkelvoudiginformatieobjecten_2",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000")
            );

            migrationBuilder
                .CreateIndex(name: "idx_e0_light_covering", table: "enkelvoudiginformatieobjecten_2", column: "id")
                .Annotation("Npgsql:IndexInclude", new[] { "owner", "vertrouwelijkheidaanduiding", "enkelvoudiginformatieobject_id " });

            migrationBuilder.CreateIndex(
                name: "IX_enkelvoudiginformatieobjecten_2_enkelvoudiginformatieobject~",
                table: "enkelvoudiginformatieobjecten_2",
                column: "enkelvoudiginformatieobject_id "
            );

            migrationBuilder.CreateIndex(
                name: "IX_enkelvoudiginformatieobjecten_2_owner_enkelvoudiginformatie~",
                table: "enkelvoudiginformatieobjecten_2",
                columns: new[] { "owner", "enkelvoudiginformatieobject_id ", "versie", "vertrouwelijkheidaanduiding" },
                descending: new[] { false, false, true, false }
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
        }
    }
}
