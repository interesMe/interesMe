using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InteresMe.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileStatusPostMediaAndUserSubinterests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfileStatus",
                schema: "profile",
                table: "user_profiles",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MediaUrlsJson",
                schema: "profile",
                table: "profile_posts",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "user_subinterests",
                schema: "interests",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubinterestId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_subinterests", x => new { x.UserId, x.SubinterestId });
                    table.ForeignKey(
                        name: "FK_user_subinterests_subinterests_SubinterestId",
                        column: x => x.SubinterestId,
                        principalSchema: "interests",
                        principalTable: "subinterests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_subinterests_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "auth",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_subinterests_SubinterestId",
                schema: "interests",
                table: "user_subinterests",
                column: "SubinterestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_subinterests",
                schema: "interests");

            migrationBuilder.DropColumn(
                name: "ProfileStatus",
                schema: "profile",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "MediaUrlsJson",
                schema: "profile",
                table: "profile_posts");
        }
    }
}
