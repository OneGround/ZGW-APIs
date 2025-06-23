using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneGround.ZGW.Documenten.DataModel.Migrations
{
    /// <inheritdoc />
    public partial class add_column_catalogus_id_into_table_enkelvoudiginformatieobjecten : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(name: "catalogus_id", table: "enkelvoudiginformatieobjecten", type: "uuid", nullable: true);
        }
    }
}
