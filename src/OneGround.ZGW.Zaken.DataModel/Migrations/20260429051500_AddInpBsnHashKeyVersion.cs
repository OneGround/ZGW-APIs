using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneGround.ZGW.Zaken.DataModel.Migrations
{
    /// <inheritdoc />
    public partial class AddInpBsnHashKeyVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "inpbsn_hash_key_version",
                table: "zaakrollen_natuurlijk_personen",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "inpbsn_hash_key_version", table: "zaakrollen_natuurlijk_personen");
        }
    }
}
