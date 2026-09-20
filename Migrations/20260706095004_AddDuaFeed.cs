using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bespoke_app_server.Migrations
{
    /// <inheritdoc />
    public partial class AddDuaFeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DuaFeedPosts",
                columns: table => new
                {
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    SavedDuaId = table.Column<Guid>(type: "uuid", nullable: true),
                    SavedSunnahDuaId = table.Column<Guid>(type: "uuid", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: false),
                    IsAnonymous = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DuaFeedPosts", x => x.PostId);
                    table.ForeignKey(
                        name: "FK_DuaFeedPosts_SavedDuas_SavedDuaId",
                        column: x => x.SavedDuaId,
                        principalTable: "SavedDuas",
                        principalColumn: "DuaId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DuaFeedPosts_SavedSunnahDuas_SavedSunnahDuaId",
                        column: x => x.SavedSunnahDuaId,
                        principalTable: "SavedSunnahDuas",
                        principalColumn: "SunnahDuaId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DuaFeedPosts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DuaFeedLikes",
                columns: table => new
                {
                    LikeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DuaFeedLikes", x => x.LikeId);
                    table.ForeignKey(
                        name: "FK_DuaFeedLikes_DuaFeedPosts_PostId",
                        column: x => x.PostId,
                        principalTable: "DuaFeedPosts",
                        principalColumn: "PostId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DuaFeedLikes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DuaFeedLikes_PostId",
                table: "DuaFeedLikes",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_DuaFeedLikes_PostId_UserId",
                table: "DuaFeedLikes",
                columns: new[] { "PostId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DuaFeedLikes_UserId",
                table: "DuaFeedLikes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DuaFeedPosts_ExpiresAt",
                table: "DuaFeedPosts",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_DuaFeedPosts_SavedDuaId",
                table: "DuaFeedPosts",
                column: "SavedDuaId");

            migrationBuilder.CreateIndex(
                name: "IX_DuaFeedPosts_SavedSunnahDuaId",
                table: "DuaFeedPosts",
                column: "SavedSunnahDuaId");

            migrationBuilder.CreateIndex(
                name: "IX_DuaFeedPosts_UserId",
                table: "DuaFeedPosts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DuaFeedPosts_UserId_ExpiresAt",
                table: "DuaFeedPosts",
                columns: new[] { "UserId", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DuaFeedLikes");

            migrationBuilder.DropTable(
                name: "DuaFeedPosts");
        }
    }
}
