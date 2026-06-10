using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InteresMe.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscoveryPreferenceGoalIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_user_discovery_preferences_Goal",
                schema: "discovery",
                table: "user_discovery_preferences",
                column: "Goal");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_user_discovery_preferences_Goal",
                schema: "discovery",
                table: "user_discovery_preferences");
        }
    }
}
