using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InteresMe.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChatModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "chat");

            migrationBuilder.CreateTable(
                name: "chat_rooms",
                schema: "chat",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InitiativeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_rooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_chat_rooms_initiatives_InitiativeId",
                        column: x => x.InitiativeId,
                        principalSchema: "initiatives",
                        principalTable: "initiatives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "chat_messages",
                schema: "chat",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChatRoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EditedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_chat_messages_chat_rooms_ChatRoomId",
                        column: x => x.ChatRoomId,
                        principalSchema: "chat",
                        principalTable: "chat_rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_chat_messages_users_SenderUserId",
                        column: x => x.SenderUserId,
                        principalSchema: "auth",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chat_participants",
                schema: "chat",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChatRoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_participants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_chat_participants_chat_rooms_ChatRoomId",
                        column: x => x.ChatRoomId,
                        principalSchema: "chat",
                        principalTable: "chat_rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_chat_participants_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "auth",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_ChatRoomId_CreatedAt",
                schema: "chat",
                table: "chat_messages",
                columns: new[] { "ChatRoomId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_SenderUserId",
                schema: "chat",
                table: "chat_messages",
                column: "SenderUserId");

            migrationBuilder.CreateIndex(
                name: "IX_chat_participants_ChatRoomId_UserId",
                schema: "chat",
                table: "chat_participants",
                columns: new[] { "ChatRoomId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chat_participants_UserId",
                schema: "chat",
                table: "chat_participants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_chat_rooms_InitiativeId",
                schema: "chat",
                table: "chat_rooms",
                column: "InitiativeId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chat_messages",
                schema: "chat");

            migrationBuilder.DropTable(
                name: "chat_participants",
                schema: "chat");

            migrationBuilder.DropTable(
                name: "chat_rooms",
                schema: "chat");
        }
    }
}
