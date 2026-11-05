using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OneGround.ZGW.Gesprekken.DataModel.Migrations
{
    /// <inheritdoc />
    public partial class AddEventLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "events",
                columns: table => new
                {
                    sequence = table
                        .Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    gesprek_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    aanleidingzaak = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data = table.Column<string>(type: "jsonb", nullable: false),
                    owner = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_events", x => x.sequence);
                }
            );

            migrationBuilder.CreateIndex(name: "IX_events_aanleidingzaak", table: "events", column: "aanleidingzaak");

            migrationBuilder.CreateIndex(name: "IX_events_event_id", table: "events", column: "event_id", unique: true);

            migrationBuilder.CreateIndex(name: "IX_events_owner_sequence", table: "events", columns: new[] { "owner", "sequence" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "events");
        }
    }
}
