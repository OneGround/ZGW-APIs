using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneGround.ZGW.Zaken.DataModel.Migrations
{
    /// <inheritdoc />
    public partial class added_gerelateerde_zaak_to_relevanteanderezaken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(name: "gerelateerde_zaak_id", table: "relevanteanderezaken", type: "uuid", nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_relevanteanderezaken_gerelateerde_zaak_id",
                table: "relevanteanderezaken",
                column: "gerelateerde_zaak_id"
            );

            // NOT VALID: the constraint is enforced for new and changed rows (and cascades on delete) right away, but the existing rows
            // are not scanned, which would hold a lock on the large zaken table. The rows that existed before this migration still have a
            // null gerelateerde_zaak_id; the constraint is validated once they have been backfilled (VALIDATE CONSTRAINT).
            migrationBuilder.Sql(
                """
                ALTER TABLE relevanteanderezaken
                    ADD CONSTRAINT "FK_relevanteanderezaken_zaken_gerelateerde_zaak_id"
                    FOREIGN KEY (gerelateerde_zaak_id) REFERENCES zaken (id) ON DELETE CASCADE NOT VALID;
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_relevanteanderezaken_zaken_gerelateerde_zaak_id", table: "relevanteanderezaken");

            migrationBuilder.DropIndex(name: "IX_relevanteanderezaken_gerelateerde_zaak_id", table: "relevanteanderezaken");

            migrationBuilder.DropColumn(name: "gerelateerde_zaak_id", table: "relevanteanderezaken");
        }
    }
}
