using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InteresMe.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPhoneVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Target",
                schema: "verification",
                table: "verification_tokens",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                schema: "verification",
                table: "verification_tokens",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                schema: "verification",
                table: "user_verifications",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Target",
                schema: "verification",
                table: "verification_tokens");

            migrationBuilder.DropColumn(
                name: "Type",
                schema: "verification",
                table: "verification_tokens");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                schema: "verification",
                table: "user_verifications");
        }
    }
}
