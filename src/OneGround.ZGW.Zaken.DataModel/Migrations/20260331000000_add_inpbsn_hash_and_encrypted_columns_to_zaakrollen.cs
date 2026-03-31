using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneGround.ZGW.Zaken.DataModel.Migrations
{
    /// <inheritdoc />
    public partial class add_inpbsn_hash_and_encrypted_columns_to_zaakrollen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "inpbsn_hash",
                table: "zaakrollen_natuurlijk_personen",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(name: "inpbsn_encrypted", table: "zaakrollen_natuurlijk_personen", type: "text", nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_zaakrollen_natuurlijk_personen_inpbsn_hash",
                table: "zaakrollen_natuurlijk_personen",
                column: "inpbsn_hash"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_zaakrollen_natuurlijk_personen_inpbsn_hash", table: "zaakrollen_natuurlijk_personen");

            migrationBuilder.DropColumn(name: "inpbsn_hash", table: "zaakrollen_natuurlijk_personen");

            migrationBuilder.DropColumn(name: "inpbsn_encrypted", table: "zaakrollen_natuurlijk_personen");
        }
    }
}
