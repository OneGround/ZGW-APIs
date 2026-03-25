using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneGround.ZGW.Zaken.DataModel.Migrations
{
    /// <inheritdoc />
    public partial class cutover_overigedata_to_jsonb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX CONCURRENTLY IF EXISTS idx_zaakobjecten_overigen_backfill;", suppressTransaction: true);

            migrationBuilder.DropColumn(name: "overigedata", table: "zaakobjecten_overigen");

            migrationBuilder.Sql("ALTER TABLE zaakobjecten_overigen ALTER COLUMN overigedata_jsonb SET NOT NULL;");

            migrationBuilder.RenameColumn(name: "overigedata_jsonb", table: "zaakobjecten_overigen", newName: "overigedata");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(name: "overigedata", table: "zaakobjecten_overigen", newName: "overigedata_jsonb");

            migrationBuilder.Sql("ALTER TABLE zaakobjecten_overigen ALTER COLUMN overigedata_jsonb DROP NOT NULL;");

            migrationBuilder.AddColumn<string>(name: "overigedata", table: "zaakobjecten_overigen", type: "text", nullable: true);

            migrationBuilder.Sql(
                "CREATE INDEX CONCURRENTLY idx_zaakobjecten_overigen_backfill ON zaakobjecten_overigen (id) WHERE overigedata_jsonb IS NULL;",
                suppressTransaction: true
            );
        }
    }
}
