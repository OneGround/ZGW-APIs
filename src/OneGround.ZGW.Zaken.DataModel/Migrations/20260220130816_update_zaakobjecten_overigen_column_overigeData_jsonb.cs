using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneGround.ZGW.Zaken.DataModel.Migrations
{
    /// <inheritdoc />
    public partial class update_zaakobjecten_overigen_column_overigeData_jsonb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PostgreSQL requires explicit conversion from text to jsonb
            migrationBuilder.Sql(
                @"ALTER TABLE zaakobjecten_overigen
                  ALTER COLUMN overigedata TYPE jsonb USING overigedata::jsonb;"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert jsonb back to text
            migrationBuilder.Sql(
                @"ALTER TABLE zaakobjecten_overigen
                  ALTER COLUMN overigedata TYPE text USING overigedata::text;"
            );
        }
    }
}
