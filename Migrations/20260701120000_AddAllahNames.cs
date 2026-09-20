using BespokeDuaApi.Data;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bespoke_app_server.Migrations
{
    /// <inheritdoc />
    public partial class AddAllahNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FeelingLabels",
                columns: table => new
                {
                    FeelingLabelId = table.Column<int>(type: "integer", nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeelingLabels", x => x.FeelingLabelId);
                });

            migrationBuilder.CreateTable(
                name: "AllahNames",
                columns: table => new
                {
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Arabic = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Transliteration = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Translation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Meaning = table.Column<string>(type: "text", nullable: false),
                    FeelingLabelId = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllahNames", x => x.Number);
                    table.ForeignKey(
                        name: "FK_AllahNames_FeelingLabels_FeelingLabelId",
                        column: x => x.FeelingLabelId,
                        principalTable: "FeelingLabels",
                        principalColumn: "FeelingLabelId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AllahNames_FeelingLabelId_SortOrder",
                table: "AllahNames",
                columns: new[] { "FeelingLabelId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_FeelingLabels_Label",
                table: "FeelingLabels",
                column: "Label",
                unique: true);

            foreach (var (id, label, displayOrder) in AllahNamesSeedData.FeelingLabels)
            {
                migrationBuilder.InsertData(
                    table: "FeelingLabels",
                    columns: new[] { "FeelingLabelId", "Label", "DisplayOrder" },
                    values: new object[] { id, label, displayOrder });
            }

            foreach (var (number, arabic, transliteration, translation, meaning, feelingLabelId, sortOrder) in AllahNamesSeedData.Names)
            {
                migrationBuilder.InsertData(
                    table: "AllahNames",
                    columns: new[] { "Number", "Arabic", "Transliteration", "Translation", "Meaning", "FeelingLabelId", "SortOrder" },
                    values: new object[] { number, arabic, transliteration, translation, meaning, feelingLabelId, sortOrder });
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AllahNames");

            migrationBuilder.DropTable(
                name: "FeelingLabels");
        }
    }
}
