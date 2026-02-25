using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneGround.ZGW.Zaken.DataModel.Migrations
{
    /// <inheritdoc />
    public partial class add_column_overigedata_jsonb_to_zaakobjecten_overigen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "overigedata_jsonb", table: "zaakobjecten_overigen", type: "jsonb", nullable: true);

            migrationBuilder.Sql(
                "CREATE INDEX CONCURRENTLY idx_zaakobjecten_overigen_backfill " + "ON zaakobjecten_overigen (id) WHERE overigedata_jsonb IS NULL;",
                suppressTransaction: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX CONCURRENTLY IF EXISTS idx_zaakobjecten_overigen_backfill;", suppressTransaction: true);

            migrationBuilder.DropColumn(name: "overigedata_jsonb", table: "zaakobjecten_overigen");
        }
    }
}
