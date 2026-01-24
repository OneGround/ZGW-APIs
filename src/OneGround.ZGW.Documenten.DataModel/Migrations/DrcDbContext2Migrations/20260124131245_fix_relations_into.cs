using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneGround.ZGW.Documenten.DataModel.Migrations.DrcDbContext2Migrations
{
    /// <inheritdoc />
    public partial class fix_relations_into : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_gebruiksrechten_enkelvoudiginformatieobjecten_2_informatieo~", table: "gebruiksrechten");

            migrationBuilder.DropForeignKey(
                name: "FK_objectinformatieobjecten_enkelvoudiginformatieobjecten_2_in~",
                table: "objectinformatieobjecten"
            );

            migrationBuilder.DropForeignKey(name: "FK_verzendingen_enkelvoudiginformatieobjecten_2_informatieobje~", table: "verzendingen");

            migrationBuilder.DropIndex(name: "IX_verzendingen_informatieobject_id21", table: "verzendingen");

            migrationBuilder.DropIndex(name: "IX_objectinformatieobjecten_informatieobject_id21", table: "objectinformatieobjecten");

            migrationBuilder.DropIndex(name: "IX_gebruiksrechten_informatieobject_id21", table: "gebruiksrechten");

            migrationBuilder.DropColumn(name: "informatieobject_id21", table: "verzendingen");

            migrationBuilder.DropColumn(name: "informatieobject_id21", table: "objectinformatieobjecten");

            migrationBuilder.DropColumn(name: "informatieobject_id21", table: "gebruiksrechten");

            migrationBuilder.RenameColumn(name: "informatieobject_id2", table: "verzendingen", newName: "enkelvoudiginformatieobjectlock_id");

            migrationBuilder.RenameColumn(name: "informatieobject_id", table: "objectinformatieobjecten", newName: "InformatieObjectId");

            migrationBuilder.RenameColumn(
                name: "informatieobject_id2",
                table: "objectinformatieobjecten",
                newName: "enkelvoudiginformatieobjectlock_id"
            );

            migrationBuilder.RenameIndex(
                name: "IX_objectinformatieobjecten_object_informatieobject_id_objectt~",
                table: "objectinformatieobjecten",
                newName: "IX_objectinformatieobjecten_object_InformatieObjectId_objectty~"
            );

            migrationBuilder.RenameIndex(
                name: "IX_objectinformatieobjecten_informatieobject_id",
                table: "objectinformatieobjecten",
                newName: "IX_objectinformatieobjecten_InformatieObjectId"
            );

            migrationBuilder.RenameColumn(name: "informatieobject_id2", table: "gebruiksrechten", newName: "enkelvoudiginformatieobjectlock_id");

            migrationBuilder.AddColumn<Guid>(
                name: "catalogus_id",
                table: "enkelvoudiginformatieobjecten_2",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000")
            );

            migrationBuilder.CreateIndex(
                name: "IX_verzendingen_enkelvoudiginformatieobjectlock_id",
                table: "verzendingen",
                column: "enkelvoudiginformatieobjectlock_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_objectinformatieobjecten_enkelvoudiginformatieobjectlock_id",
                table: "objectinformatieobjecten",
                column: "enkelvoudiginformatieobjectlock_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_gebruiksrechten_enkelvoudiginformatieobjectlock_id",
                table: "gebruiksrechten",
                column: "enkelvoudiginformatieobjectlock_id"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_gebruiksrechten_enkelvoudiginformatieobject_locks_2_enkelvo~",
                table: "gebruiksrechten",
                column: "enkelvoudiginformatieobjectlock_id",
                principalTable: "enkelvoudiginformatieobject_locks_2",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "FK_objectinformatieobjecten_enkelvoudiginformatieobject_locks_~",
                table: "objectinformatieobjecten",
                column: "enkelvoudiginformatieobjectlock_id",
                principalTable: "enkelvoudiginformatieobject_locks_2",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "FK_verzendingen_enkelvoudiginformatieobject_locks_2_enkelvoudi~",
                table: "verzendingen",
                column: "enkelvoudiginformatieobjectlock_id",
                principalTable: "enkelvoudiginformatieobject_locks_2",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_gebruiksrechten_enkelvoudiginformatieobject_locks_2_enkelvo~", table: "gebruiksrechten");

            migrationBuilder.DropForeignKey(
                name: "FK_objectinformatieobjecten_enkelvoudiginformatieobject_locks_~",
                table: "objectinformatieobjecten"
            );

            migrationBuilder.DropForeignKey(name: "FK_verzendingen_enkelvoudiginformatieobject_locks_2_enkelvoudi~", table: "verzendingen");

            migrationBuilder.DropIndex(name: "IX_verzendingen_enkelvoudiginformatieobjectlock_id", table: "verzendingen");

            migrationBuilder.DropIndex(name: "IX_objectinformatieobjecten_enkelvoudiginformatieobjectlock_id", table: "objectinformatieobjecten");

            migrationBuilder.DropIndex(name: "IX_gebruiksrechten_enkelvoudiginformatieobjectlock_id", table: "gebruiksrechten");

            migrationBuilder.DropColumn(name: "catalogus_id", table: "enkelvoudiginformatieobjecten_2");

            migrationBuilder.RenameColumn(name: "enkelvoudiginformatieobjectlock_id", table: "verzendingen", newName: "informatieobject_id2");

            migrationBuilder.RenameColumn(name: "InformatieObjectId", table: "objectinformatieobjecten", newName: "informatieobject_id");

            migrationBuilder.RenameColumn(
                name: "enkelvoudiginformatieobjectlock_id",
                table: "objectinformatieobjecten",
                newName: "informatieobject_id2"
            );

            migrationBuilder.RenameIndex(
                name: "IX_objectinformatieobjecten_object_InformatieObjectId_objectty~",
                table: "objectinformatieobjecten",
                newName: "IX_objectinformatieobjecten_object_informatieobject_id_objectt~"
            );

            migrationBuilder.RenameIndex(
                name: "IX_objectinformatieobjecten_InformatieObjectId",
                table: "objectinformatieobjecten",
                newName: "IX_objectinformatieobjecten_informatieobject_id"
            );

            migrationBuilder.RenameColumn(name: "enkelvoudiginformatieobjectlock_id", table: "gebruiksrechten", newName: "informatieobject_id2");

            migrationBuilder.AddColumn<Guid>(name: "informatieobject_id21", table: "verzendingen", type: "uuid", nullable: true);

            migrationBuilder.AddColumn<Guid>(name: "informatieobject_id21", table: "objectinformatieobjecten", type: "uuid", nullable: true);

            migrationBuilder.AddColumn<Guid>(name: "informatieobject_id21", table: "gebruiksrechten", type: "uuid", nullable: true);

            migrationBuilder.CreateIndex(name: "IX_verzendingen_informatieobject_id21", table: "verzendingen", column: "informatieobject_id21");

            migrationBuilder.CreateIndex(
                name: "IX_objectinformatieobjecten_informatieobject_id21",
                table: "objectinformatieobjecten",
                column: "informatieobject_id21"
            );

            migrationBuilder.CreateIndex(name: "IX_gebruiksrechten_informatieobject_id21", table: "gebruiksrechten", column: "informatieobject_id21");

            migrationBuilder.AddForeignKey(
                name: "FK_gebruiksrechten_enkelvoudiginformatieobjecten_2_informatieo~",
                table: "gebruiksrechten",
                column: "informatieobject_id21",
                principalTable: "enkelvoudiginformatieobjecten_2",
                principalColumn: "id"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_objectinformatieobjecten_enkelvoudiginformatieobjecten_2_in~",
                table: "objectinformatieobjecten",
                column: "informatieobject_id21",
                principalTable: "enkelvoudiginformatieobjecten_2",
                principalColumn: "id"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_verzendingen_enkelvoudiginformatieobjecten_2_informatieobje~",
                table: "verzendingen",
                column: "informatieobject_id21",
                principalTable: "enkelvoudiginformatieobjecten_2",
                principalColumn: "id"
            );
        }
    }
}
