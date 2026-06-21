using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InteresMe.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPostAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "post_attachments",
                schema: "posts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_post_attachments", x => x.Id);
                    table.CheckConstraint("CK_post_attachments_SizeBytes_positive", "\"SizeBytes\" > 0");
                    table.CheckConstraint("CK_post_attachments_SortOrder_nonnegative", "\"SortOrder\" >= 0");
                    table.ForeignKey(
                        name: "FK_post_attachments_posts_PostId",
                        column: x => x.PostId,
                        principalSchema: "posts",
                        principalTable: "posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_post_attachments_PostId_SortOrder",
                schema: "posts",
                table: "post_attachments",
                columns: new[] { "PostId", "SortOrder" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "post_attachments",
                schema: "posts");
        }
    }
}
