using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InteresMe.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPostCommentReplies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentCommentId",
                schema: "posts",
                table: "comments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_comments_ParentCommentId_CreatedAt_Id",
                schema: "posts",
                table: "comments",
                columns: new[] { "ParentCommentId", "CreatedAt", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_comments_comments_ParentCommentId",
                schema: "posts",
                table: "comments",
                column: "ParentCommentId",
                principalSchema: "posts",
                principalTable: "comments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_comments_comments_ParentCommentId",
                schema: "posts",
                table: "comments");

            migrationBuilder.DropIndex(
                name: "IX_comments_ParentCommentId_CreatedAt_Id",
                schema: "posts",
                table: "comments");

            migrationBuilder.DropColumn(
                name: "ParentCommentId",
                schema: "posts",
                table: "comments");
        }
    }
}
