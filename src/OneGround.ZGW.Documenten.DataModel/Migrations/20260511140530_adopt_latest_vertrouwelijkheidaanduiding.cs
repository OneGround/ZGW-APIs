using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneGround.ZGW.Documenten.DataModel.Migrations
{
    /// <inheritdoc />
    public partial class adopt_latest_vertrouwelijkheidaanduiding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Design contract for this column (chosen by the original developer):
            //   - value 9  → "not yet migrated by application" (sentinel; not a valid VertrouwelijkheidAanduiding)
            //   - NULL     → "versie's vertrouwelijkheidaanduiding is genuinely null" (functionally valid)
            //   - 0..7     → real enum value
            // DEFAULT cannot be NULL because NULL is a legitimate functional value — a sentinel is
            // required to distinguish "never migrated" from "deliberately null".

            // 1. Create the column on envs where it does not yet exist (lower envs). Match prod's design.
            migrationBuilder.Sql(
                @"
                ALTER TABLE public.enkelvoudiginformatieobjecten
                    ADD COLUMN IF NOT EXISTS latest_vertrouwelijkheidaanduiding integer DEFAULT 9;
            "
            );

            // 2. Ensure DEFAULT 9 is set. Idempotent: covers the case where the column was previously
            //    created without the sentinel default (e.g. a prior version of this migration that
            //    dropped the default).
            migrationBuilder.Sql(
                @"
                ALTER TABLE public.enkelvoudiginformatieobjecten
                    ALTER COLUMN latest_vertrouwelijkheidaanduiding SET DEFAULT 9;
            "
            );

            // 3. Ensure the column is nullable. The dev's design requires NULL to be representable
            //    as a legitimate value (distinct from sentinel 9). DROP NOT NULL is a no-op if the
            //    column is already nullable.
            migrationBuilder.Sql(
                @"
                ALTER TABLE public.enkelvoudiginformatieobjecten
                    ALTER COLUMN latest_vertrouwelijkheidaanduiding DROP NOT NULL;
            "
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // To fully roll back: drop the column manually after confirming it is
            // unreferenced by application code.
        }
    }
}
