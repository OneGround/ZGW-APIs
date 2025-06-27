using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging;

#nullable disable

namespace OneGround.ZGW.Catalogi.DataModel.Migrations
{
    /// <inheritdoc />
    public partial class cleanup_old_tables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"drop table if exists besluittypeinformatieobjecttypen_old");
            migrationBuilder.Sql(@"drop table if exists besluittypezaaktypen_old");
            migrationBuilder.Sql(@"drop table if exists zaaktypedeelzaaktypen_old");
            migrationBuilder.Sql(@"drop table if exists zaaktypeinformatieobjecttypen_old");
            migrationBuilder.Sql(@"drop table if exists gerelateerdezaaktypen_old");
        }
    }
}
