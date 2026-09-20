using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bespoke_app_server.Migrations
{
    /// <inheritdoc />
    public partial class AddSunnahDuasToCollections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_DuaCollectionItems",
                table: "DuaCollectionItems");

            migrationBuilder.AlterColumn<Guid>(
                name: "DuaId",
                table: "DuaCollectionItems",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "ItemId",
                table: "DuaCollectionItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "DuaCollectionItems"
                SET "ItemId" = gen_random_uuid()
                WHERE "ItemId" IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "ItemId",
                table: "DuaCollectionItems",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SunnahDuaId",
                table: "DuaCollectionItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_DuaCollectionItems",
                table: "DuaCollectionItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_DuaCollectionItems_CollectionId_DuaId",
                table: "DuaCollectionItems",
                columns: new[] { "CollectionId", "DuaId" },
                unique: true,
                filter: "\"DuaId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DuaCollectionItems_CollectionId_SunnahDuaId",
                table: "DuaCollectionItems",
                columns: new[] { "CollectionId", "SunnahDuaId" },
                unique: true,
                filter: "\"SunnahDuaId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DuaCollectionItems_SunnahDuaId",
                table: "DuaCollectionItems",
                column: "SunnahDuaId");

            migrationBuilder.AddForeignKey(
                name: "FK_DuaCollectionItems_SavedSunnahDuas_SunnahDuaId",
                table: "DuaCollectionItems",
                column: "SunnahDuaId",
                principalTable: "SavedSunnahDuas",
                principalColumn: "SunnahDuaId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DuaCollectionItems_SavedSunnahDuas_SunnahDuaId",
                table: "DuaCollectionItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DuaCollectionItems",
                table: "DuaCollectionItems");

            migrationBuilder.DropIndex(
                name: "IX_DuaCollectionItems_CollectionId_DuaId",
                table: "DuaCollectionItems");

            migrationBuilder.DropIndex(
                name: "IX_DuaCollectionItems_CollectionId_SunnahDuaId",
                table: "DuaCollectionItems");

            migrationBuilder.DropIndex(
                name: "IX_DuaCollectionItems_SunnahDuaId",
                table: "DuaCollectionItems");

            migrationBuilder.DropColumn(
                name: "ItemId",
                table: "DuaCollectionItems");

            migrationBuilder.DropColumn(
                name: "SunnahDuaId",
                table: "DuaCollectionItems");

            migrationBuilder.AlterColumn<Guid>(
                name: "DuaId",
                table: "DuaCollectionItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_DuaCollectionItems",
                table: "DuaCollectionItems",
                columns: new[] { "CollectionId", "DuaId" });
        }
    }
}
