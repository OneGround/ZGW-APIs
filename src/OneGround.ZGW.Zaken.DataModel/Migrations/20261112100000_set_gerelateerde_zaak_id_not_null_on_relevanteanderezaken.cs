using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneGround.ZGW.Zaken.DataModel.Migrations
{
    /// <inheritdoc />
    public partial class set_gerelateerde_zaak_id_not_null_on_relevanteanderezaken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Every statement runs outside the migration transaction (suppressTransaction): inside one transaction the ACCESS EXCLUSIVE lock of the
            // first ALTER TABLE would be held until the end, i.e. during the table scan of the VALIDATE. Without a transaction each statement
            // commits on its own, so the exclusive locks are short and the scan only takes a lock that does not block reads and writes.
            // Because of that the steps are idempotent: a failed run is not recorded and can simply be repeated.

            // Fail with a clear message when rows have not been given a gerelateerde_zaak_id yet (the backfill of existing rows has to run first),
            // instead of with a bare constraint violation.
            migrationBuilder.Sql(
                """
                DO $$
                DECLARE missing bigint;
                BEGIN
                    SELECT count(*) INTO missing FROM relevanteanderezaken WHERE gerelateerde_zaak_id IS NULL;
                    IF missing > 0 THEN
                        RAISE EXCEPTION 'relevanteanderezaken.gerelateerde_zaak_id cannot be made NOT NULL: % rows have no gerelateerde_zaak_id yet. Backfill the existing rows first.', missing;
                    END IF;
                END $$;
                """,
                suppressTransaction: true
            );

            // SET NOT NULL scans the table under an ACCESS EXCLUSIVE lock, unless PostgreSQL can prove from a valid CHECK constraint that there are no
            // nulls. So: add the constraint NOT VALID (no scan), VALIDATE it (scan without blocking reads/writes), then SET NOT NULL is instant.
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint
                        WHERE conname = 'CK_relevanteanderezaken_gerelateerde_zaak_id_not_null' AND conrelid = 'relevanteanderezaken'::regclass
                    ) THEN
                        ALTER TABLE relevanteanderezaken
                            ADD CONSTRAINT "CK_relevanteanderezaken_gerelateerde_zaak_id_not_null" CHECK (gerelateerde_zaak_id IS NOT NULL) NOT VALID;
                    END IF;
                END $$;
                """,
                suppressTransaction: true
            );

            migrationBuilder.Sql(
                """ALTER TABLE relevanteanderezaken VALIDATE CONSTRAINT "CK_relevanteanderezaken_gerelateerde_zaak_id_not_null";""",
                suppressTransaction: true
            );

            migrationBuilder.Sql("ALTER TABLE relevanteanderezaken ALTER COLUMN gerelateerde_zaak_id SET NOT NULL;", suppressTransaction: true);

            migrationBuilder.Sql(
                """ALTER TABLE relevanteanderezaken DROP CONSTRAINT IF EXISTS "CK_relevanteanderezaken_gerelateerde_zaak_id_not_null";""",
                suppressTransaction: true
            );

            // The url is no longer written (the relation is kept by gerelateerde_zaak_id); the existing values are kept. Metadata only, no table scan.
            migrationBuilder.Sql("ALTER TABLE relevanteanderezaken ALTER COLUMN url DROP NOT NULL;", suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE relevanteanderezaken ALTER COLUMN gerelateerde_zaak_id DROP NOT NULL;");

            // Note: url is deliberately left nullable. Rows created after this migration have no url, so NOT NULL cannot be restored without inventing values.
        }
    }
}
