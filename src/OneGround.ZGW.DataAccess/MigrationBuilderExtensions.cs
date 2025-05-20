using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

namespace OneGround.ZGW.DataAccess;

public static class MigrationBuilderExtensions
{
    public static void ChangeTextColumnToPeriod(this MigrationBuilder migrationBuilder, string column, string table, bool nullable = true)
    {
        migrationBuilder.AddColumn<Period>(name: $"{column}_period", table: table, nullable: true);

        migrationBuilder.Sql(
            @$"
                UPDATE {table}
                SET {column}_period = {column}::INTERVAL"
        );

        migrationBuilder.DropColumn(name: column, table: table);

        migrationBuilder.RenameColumn(name: $"{column}_period", table: table, newName: column);

        if (!nullable)
        {
            migrationBuilder.AlterColumn<Period>(name: column, table: table, nullable: false, oldNullable: true);
        }
    }
}
