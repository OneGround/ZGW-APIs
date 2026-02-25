using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneGround.ZGW.Zaken.DataModel.Migrations
{
    /// <inheritdoc />
    public partial class create_index_backfill_on_zaakobjecten_overigen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "CREATE INDEX CONCURRENTLY idx_zaakobjecten_overigen_backfill " + "ON zaakobjecten_overigen (id) WHERE overigedata_jsonb IS NULL;",
                true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX CONCURRENTLY IF EXISTS idx_zaakobjecten_overigen_backfill;", true);
        }
    }
}
