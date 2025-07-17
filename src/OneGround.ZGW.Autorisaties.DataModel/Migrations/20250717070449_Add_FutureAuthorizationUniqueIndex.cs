using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneGround.ZGW.Autorisaties.DataModel.Migrations
{
    /// <inheritdoc />
    public partial class Add_FutureAuthorizationUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Sort scopes for Autorisatie
            migrationBuilder.Sql(
                @"
                UPDATE autorisaties
                SET scopes = (
                    SELECT array_agg(s ORDER BY s)
                    FROM unnest(scopes) AS s
                )
                WHERE scopes IS NOT NULL;
            "
            );

            // Sort scopes for FutureAutorisatie
            migrationBuilder.Sql(
                @"
                UPDATE future_autorisaties
                SET scopes = (
                    SELECT array_agg(s ORDER BY s)
                    FROM unnest(scopes) AS s
                )
                WHERE scopes IS NOT NULL;
            "
            );

            migrationBuilder
                .CreateIndex(
                    name: "IX_future_autorisaties_component_max_vertrouwelijkheidaanduidi~",
                    table: "future_autorisaties",
                    columns: new[] { "component", "max_vertrouwelijkheidaanduiding", "scopes", "applicatie_id", "owner" },
                    unique: true
                )
                .Annotation("Npgsql:CreatedConcurrently", true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_future_autorisaties_component_max_vertrouwelijkheidaanduidi~", table: "future_autorisaties");
        }
    }
}
