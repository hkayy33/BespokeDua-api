using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bespoke_app_server.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedSunnahDuas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SavedSunnahDuas",
                columns: table => new
                {
                    SunnahDuaId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    SunnahDua = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedSunnahDuas", x => x.SunnahDuaId);
                    table.ForeignKey(
                        name: "FK_SavedSunnahDuas_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedSunnahDuas_UserId",
                table: "SavedSunnahDuas",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavedSunnahDuas");
        }
    }
}
