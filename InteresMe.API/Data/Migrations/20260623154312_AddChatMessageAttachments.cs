using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InteresMe.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChatMessageAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Text",
                schema: "chat",
                table: "chat_messages",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.CreateTable(
                name: "chat_message_attachments",
                schema: "chat",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChatMessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    FileName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_message_attachments", x => x.Id);
                    table.CheckConstraint("CK_chat_message_attachments_size_positive", "\"SizeBytes\" > 0");
                    table.ForeignKey(
                        name: "FK_chat_message_attachments_chat_messages_ChatMessageId",
                        column: x => x.ChatMessageId,
                        principalSchema: "chat",
                        principalTable: "chat_messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_chat_message_attachments_ChatMessageId",
                schema: "chat",
                table: "chat_message_attachments",
                column: "ChatMessageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chat_message_attachments",
                schema: "chat");

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                schema: "chat",
                table: "chat_messages",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);
        }
    }
}
