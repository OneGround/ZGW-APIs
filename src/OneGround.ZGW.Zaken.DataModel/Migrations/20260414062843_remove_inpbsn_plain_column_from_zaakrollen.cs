using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneGround.ZGW.Zaken.DataModel.Migrations
{
    /// <inheritdoc />
    public partial class remove_inpbsn_plain_column_from_zaakrollen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX CONCURRENTLY IF EXISTS IX_zaakrollen_natuurlijk_personen_inpbsn;", suppressTransaction: true);

            migrationBuilder.DropColumn(name: "inpbsn", table: "zaakrollen_natuurlijk_personen");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "inpbsn",
                table: "zaakrollen_natuurlijk_personen",
                type: "character varying(9)",
                maxLength: 9,
                nullable: true
            );

            migrationBuilder.Sql(
                "CREATE INDEX CONCURRENTLY IF NOT EXISTS IX_zaakrollen_natuurlijk_personen_inpbsn ON zaakrollen_natuurlijk_personen (inpbsn);",
                suppressTransaction: true
            );
        }
    }
}
