using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bespoke_app_server.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedDuaUpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "SavedDuas",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "SavedDuas");
        }
    }
}
