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
            migrationBuilder.AlterColumn<string>(
                name: "overigedata",
                table: "zaakobjecten_overigen",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text"
            );

            migrationBuilder.AddColumn<string>(name: "overigedata_jsonb", table: "zaakobjecten_overigen", type: "jsonb", nullable: true);

            // Phase 1: column is added, application starts writing to overigedata_jsonb.
            // The backfill of existing rows is handled by a separate service.
            // Once the backfill is confirmed complete and the application is fully using the new column,
            // this UPDATE will be uncommented and applied as part of the next migration stage (Phase 2 cutover).
            // migrationBuilder.Sql(
            //     "UPDATE zaakobjecten_overigen SET overigedata_jsonb = to_jsonb(overigedata) WHERE overigedata IS NOT NULL AND overigedata_jsonb IS NULL;"
            // );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "overigedata_jsonb", table: "zaakobjecten_overigen");

            migrationBuilder.AlterColumn<string>(
                name: "overigedata",
                table: "zaakobjecten_overigen",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true
            );
        }
    }
}
