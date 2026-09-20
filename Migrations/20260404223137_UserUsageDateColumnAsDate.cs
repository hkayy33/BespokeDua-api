using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bespoke_app_server.Migrations
{
    /// <inheritdoc />
    public partial class UserUsageDateColumnAsDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """ALTER TABLE "UserUsages" ALTER COLUMN "Date" TYPE date USING (("Date" AT TIME ZONE 'UTC')::date);""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """ALTER TABLE "UserUsages" ALTER COLUMN "Date" TYPE timestamp with time zone USING ("Date"::timestamp AT TIME ZONE 'UTC');""");
        }
    }
}
