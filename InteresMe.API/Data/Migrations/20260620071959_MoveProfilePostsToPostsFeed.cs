using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InteresMe.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class MoveProfilePostsToPostsFeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "posts");

            migrationBuilder.DropForeignKey(
                name: "FK_profile_posts_users_UserId",
                schema: "profile",
                table: "profile_posts");

            migrationBuilder.DropIndex(
                name: "IX_profile_posts_UserId_CreatedAt",
                schema: "profile",
                table: "profile_posts");

            migrationBuilder.RenameTable(
                name: "profile_posts",
                schema: "profile",
                newName: "posts",
                newSchema: "posts");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "posts",
                table: "posts",
                newName: "AuthorId");

            migrationBuilder.DropColumn(
                name: "InterestPath",
                schema: "posts",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "MediaUrlsJson",
                schema: "posts",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "Title",
                schema: "posts",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "Type",
                schema: "posts",
                table: "posts");

            migrationBuilder.AddColumn<Guid>(
                name: "InitiativeId",
                schema: "posts",
                table: "posts",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                "ALTER TABLE posts.posts RENAME CONSTRAINT \"PK_profile_posts\" TO \"PK_posts\";");

            migrationBuilder.CreateIndex(
                name: "IX_posts_AuthorId_CreatedAt_Id",
                schema: "posts",
                table: "posts",
                columns: new[] { "AuthorId", "CreatedAt", "Id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_posts_InitiativeId",
                schema: "posts",
                table: "posts",
                column: "InitiativeId");

            migrationBuilder.AddForeignKey(
                name: "FK_posts_initiatives_InitiativeId",
                schema: "posts",
                table: "posts",
                column: "InitiativeId",
                principalSchema: "initiatives",
                principalTable: "initiatives",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_posts_users_AuthorId",
                schema: "posts",
                table: "posts",
                column: "AuthorId",
                principalSchema: "auth",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_posts_initiatives_InitiativeId",
                schema: "posts",
                table: "posts");

            migrationBuilder.DropForeignKey(
                name: "FK_posts_users_AuthorId",
                schema: "posts",
                table: "posts");

            migrationBuilder.DropIndex(
                name: "IX_posts_AuthorId_CreatedAt_Id",
                schema: "posts",
                table: "posts");

            migrationBuilder.DropIndex(
                name: "IX_posts_InitiativeId",
                schema: "posts",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "InitiativeId",
                schema: "posts",
                table: "posts");

            migrationBuilder.AddColumn<string>(
                name: "InterestPath",
                schema: "posts",
                table: "posts",
                type: "character varying(240)",
                maxLength: 240,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MediaUrlsJson",
                schema: "posts",
                table: "posts",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                schema: "posts",
                table: "posts",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                schema: "posts",
                table: "posts",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "post");

            migrationBuilder.RenameColumn(
                name: "AuthorId",
                schema: "posts",
                table: "posts",
                newName: "UserId");

            migrationBuilder.RenameTable(
                name: "posts",
                schema: "posts",
                newName: "profile_posts",
                newSchema: "profile");

            migrationBuilder.Sql(
                "ALTER TABLE profile.profile_posts RENAME CONSTRAINT \"PK_posts\" TO \"PK_profile_posts\";");

            migrationBuilder.CreateIndex(
                name: "IX_profile_posts_UserId_CreatedAt",
                schema: "profile",
                table: "profile_posts",
                columns: new[] { "UserId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.AddForeignKey(
                name: "FK_profile_posts_users_UserId",
                schema: "profile",
                table: "profile_posts",
                column: "UserId",
                principalSchema: "auth",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
