using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bespoke_app_server.Migrations
{
    /// <inheritdoc />
    public partial class AddDuaCollections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DuaCollections",
                columns: table => new
                {
                    CollectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DuaCollections", x => x.CollectionId);
                    table.ForeignKey(
                        name: "FK_DuaCollections_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DuaCollectionItems",
                columns: table => new
                {
                    CollectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DuaId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DuaCollectionItems", x => new { x.CollectionId, x.DuaId });
                    table.ForeignKey(
                        name: "FK_DuaCollectionItems_DuaCollections_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "DuaCollections",
                        principalColumn: "CollectionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DuaCollectionItems_SavedDuas_DuaId",
                        column: x => x.DuaId,
                        principalTable: "SavedDuas",
                        principalColumn: "DuaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DuaCollectionItems_DuaId",
                table: "DuaCollectionItems",
                column: "DuaId");

            migrationBuilder.CreateIndex(
                name: "IX_DuaCollections_UserId",
                table: "DuaCollections",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DuaCollectionItems");

            migrationBuilder.DropTable(
                name: "DuaCollections");
        }
    }
}
