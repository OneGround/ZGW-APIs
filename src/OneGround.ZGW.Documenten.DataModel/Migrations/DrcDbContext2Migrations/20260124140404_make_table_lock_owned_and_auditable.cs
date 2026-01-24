using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneGround.ZGW.Documenten.DataModel.Migrations.DrcDbContext2Migrations
{
    /// <inheritdoc />
    public partial class make_table_lock_owned_and_auditable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_enkelvoudiginformatieobjecten_2_enkelvoudiginformatieobject~",
                table: "enkelvoudiginformatieobjecten_2"
            );

            migrationBuilder.AddColumn<string>(
                name: "createdby",
                table: "enkelvoudiginformatieobject_locks_2",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "creationtime",
                table: "enkelvoudiginformatieobject_locks_2",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified)
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "modificationtime",
                table: "enkelvoudiginformatieobject_locks_2",
                type: "timestamp with time zone",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "modifiedby",
                table: "enkelvoudiginformatieobject_locks_2",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "owner",
                table: "enkelvoudiginformatieobject_locks_2",
                type: "character varying(9)",
                maxLength: 9,
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddForeignKey(
                name: "FK_enkelvoudiginformatieobjecten_2_enkelvoudiginformatieobject~",
                table: "enkelvoudiginformatieobjecten_2",
                column: "enkelvoudiginformatieobject_lock_id",
                principalTable: "enkelvoudiginformatieobject_locks_2",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_enkelvoudiginformatieobjecten_2_enkelvoudiginformatieobject~",
                table: "enkelvoudiginformatieobjecten_2"
            );

            migrationBuilder.DropColumn(name: "createdby", table: "enkelvoudiginformatieobject_locks_2");

            migrationBuilder.DropColumn(name: "creationtime", table: "enkelvoudiginformatieobject_locks_2");

            migrationBuilder.DropColumn(name: "modificationtime", table: "enkelvoudiginformatieobject_locks_2");

            migrationBuilder.DropColumn(name: "modifiedby", table: "enkelvoudiginformatieobject_locks_2");

            migrationBuilder.DropColumn(name: "owner", table: "enkelvoudiginformatieobject_locks_2");

            migrationBuilder.AddForeignKey(
                name: "FK_enkelvoudiginformatieobjecten_2_enkelvoudiginformatieobject~",
                table: "enkelvoudiginformatieobjecten_2",
                column: "enkelvoudiginformatieobject_lock_id",
                principalTable: "enkelvoudiginformatieobject_locks_2",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade
            );
        }
    }
}
