using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneGround.ZGW.Gesprekken.DataModel.Migrations
{
    /// <inheritdoc />
    public partial class AanleidingenUrlsEnRedactie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "partij",
                table: "gespreksdeelnames",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                collation: "ci_collation",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "emailadres",
                table: "gespreksdeelnames",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(254)",
                oldMaxLength: 254
            );

            migrationBuilder.AlterColumn<string>(
                name: "tekstgespreksbijdrage",
                table: "gespreksbijdragen",
                type: "character varying(5000)",
                maxLength: 5000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text"
            );

            migrationBuilder.AddColumn<string>(
                name: "aanleidingagendaafspraak",
                table: "gesprekken",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "aanleidingproductdienst",
                table: "gesprekken",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "aanleidingthema",
                table: "gesprekken",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "contactmoment",
                table: "gesprekken",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true
            );

            migrationBuilder.AddColumn<bool>(name: "geredigeerd", table: "events", type: "boolean", nullable: false, defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "aanleidingagendaafspraak", table: "gesprekken");

            migrationBuilder.DropColumn(name: "aanleidingproductdienst", table: "gesprekken");

            migrationBuilder.DropColumn(name: "aanleidingthema", table: "gesprekken");

            migrationBuilder.DropColumn(name: "contactmoment", table: "gesprekken");

            migrationBuilder.DropColumn(name: "geredigeerd", table: "events");

            migrationBuilder.AlterColumn<Guid>(
                name: "partij",
                table: "gespreksdeelnames",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true,
                oldCollation: "ci_collation"
            );

            migrationBuilder.AlterColumn<string>(
                name: "emailadres",
                table: "gespreksdeelnames",
                type: "character varying(254)",
                maxLength: 254,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(254)",
                oldMaxLength: 254,
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "tekstgespreksbijdrage",
                table: "gespreksbijdragen",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(5000)",
                oldMaxLength: 5000
            );
        }
    }
}
