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
            // The statements are idempotent: the index is built outside the migration transaction (see below), so a failed run can leave the
            // column behind without the migration being recorded, and the next run must be able to pick up where it stopped.
            migrationBuilder.Sql("ALTER TABLE relevanteanderezaken ADD COLUMN IF NOT EXISTS gerelateerde_zaak_id uuid NULL;");

            // CONCURRENTLY: does not block writes to relevanteanderezaken while the index is built. This is not possible inside a transaction.
            migrationBuilder.Sql(
                """
                CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_relevanteanderezaken_gerelateerde_zaak_id" ON relevanteanderezaken (gerelateerde_zaak_id);
                """,
                suppressTransaction: true
            );

            // NOT VALID: the constraint is enforced for new and changed rows (and cascades on delete) right away, but the existing rows are not
            // scanned. Adding it still takes a short lock on both tables, which can queue behind a long running transaction. The rows that existed
            // before this migration have a null gerelateerde_zaak_id; the constraint is validated (VALIDATE CONSTRAINT) once they have been backfilled.
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_relevanteanderezaken_zaken_gerelateerde_zaak_id') THEN
                        ALTER TABLE relevanteanderezaken
                            ADD CONSTRAINT "FK_relevanteanderezaken_zaken_gerelateerde_zaak_id"
                            FOREIGN KEY (gerelateerde_zaak_id) REFERENCES zaken (id) ON DELETE CASCADE NOT VALID;
                    END IF;
                END $$;
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
