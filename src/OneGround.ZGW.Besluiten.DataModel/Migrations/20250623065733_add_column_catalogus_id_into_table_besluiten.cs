using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneGround.ZGW.Besluiten.DataModel.Migrations
{
    /// <inheritdoc />
    public partial class add_column_catalogus_id_into_table_besluiten : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(name: "catalogus_id", table: "besluiten", type: "uuid", nullable: true);
        }
    }
}
