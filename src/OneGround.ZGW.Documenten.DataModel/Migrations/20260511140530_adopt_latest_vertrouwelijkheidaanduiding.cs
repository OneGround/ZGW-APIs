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
            // 1. Create column if missing (lower envs). Nullable integer, no default.
            migrationBuilder.Sql(
                @"
                ALTER TABLE public.enkelvoudiginformatieobjecten
                    ADD COLUMN IF NOT EXISTS latest_vertrouwelijkheidaanduiding integer;
            "
            );

            // 2. Drop the hard-coded DEFAULT 9 so new INSERTs that omit the
            //    column produce NULL, not the wrong value. DROP DEFAULT is a no-op
            //    if there is no default.
            migrationBuilder.Sql(
                @"
                ALTER TABLE public.enkelvoudiginformatieobjecten
                    ALTER COLUMN latest_vertrouwelijkheidaanduiding DROP DEFAULT;
            "
            );

            // 3. Drop NOT NULL if present. DROP NOT NULL is a no-op if the
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
