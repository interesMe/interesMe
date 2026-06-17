using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InteresMe.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChatMessagingAreas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "chat");

            migrationBuilder.CreateTable(
                name: "channels",
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
                    table.PrimaryKey("PK_channels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_channels_initiatives_InitiativeId",
                        column: x => x.InitiativeId,
                        principalSchema: "initiatives",
                        principalTable: "initiatives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "direct_conversations",
                schema: "chat",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserOneId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserTwoId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_direct_conversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_direct_conversations_users_UserOneId",
                        column: x => x.UserOneId,
                        principalSchema: "auth",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_direct_conversations_users_UserTwoId",
                        column: x => x.UserTwoId,
                        principalSchema: "auth",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "group_chats",
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
                    table.PrimaryKey("PK_group_chats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_group_chats_initiatives_InitiativeId",
                        column: x => x.InitiativeId,
                        principalSchema: "initiatives",
                        principalTable: "initiatives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "channel_members",
                schema: "chat",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_channel_members", x => x.Id);
                    table.ForeignKey(
                        name: "FK_channel_members_channels_ChannelId",
                        column: x => x.ChannelId,
                        principalSchema: "chat",
                        principalTable: "channels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_channel_members_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "auth",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "direct_conversation_participants",
                schema: "chat",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DirectConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_direct_conversation_participants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_direct_conversation_participants_direct_conversations_Direc~",
                        column: x => x.DirectConversationId,
                        principalSchema: "chat",
                        principalTable: "direct_conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_direct_conversation_participants_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "auth",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chat_messages",
                schema: "chat",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationType = table.Column<int>(type: "integer", nullable: false),
                    DirectConversationId = table.Column<Guid>(type: "uuid", nullable: true),
                    GroupChatId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChannelId = table.Column<Guid>(type: "uuid", nullable: true),
                    SenderUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EditedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_messages", x => x.Id);
                    table.CheckConstraint("CK_chat_messages_single_conversation", "(\"ConversationType\" = 1 AND \"DirectConversationId\" IS NOT NULL AND \"GroupChatId\" IS NULL AND \"ChannelId\" IS NULL)\nOR (\"ConversationType\" = 2 AND \"DirectConversationId\" IS NULL AND \"GroupChatId\" IS NOT NULL AND \"ChannelId\" IS NULL)\nOR (\"ConversationType\" = 3 AND \"DirectConversationId\" IS NULL AND \"GroupChatId\" IS NULL AND \"ChannelId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_chat_messages_channels_ChannelId",
                        column: x => x.ChannelId,
                        principalSchema: "chat",
                        principalTable: "channels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_chat_messages_direct_conversations_DirectConversationId",
                        column: x => x.DirectConversationId,
                        principalSchema: "chat",
                        principalTable: "direct_conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_chat_messages_group_chats_GroupChatId",
                        column: x => x.GroupChatId,
                        principalSchema: "chat",
                        principalTable: "group_chats",
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
                name: "group_chat_participants",
                schema: "chat",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupChatId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_chat_participants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_group_chat_participants_group_chats_GroupChatId",
                        column: x => x.GroupChatId,
                        principalSchema: "chat",
                        principalTable: "group_chats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_group_chat_participants_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "auth",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_channel_members_ChannelId_UserId",
                schema: "chat",
                table: "channel_members",
                columns: new[] { "ChannelId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_channel_members_UserId",
                schema: "chat",
                table: "channel_members",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_channels_InitiativeId",
                schema: "chat",
                table: "channels",
                column: "InitiativeId");

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_ChannelId_CreatedAt",
                schema: "chat",
                table: "chat_messages",
                columns: new[] { "ChannelId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_DirectConversationId_CreatedAt",
                schema: "chat",
                table: "chat_messages",
                columns: new[] { "DirectConversationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_GroupChatId_CreatedAt",
                schema: "chat",
                table: "chat_messages",
                columns: new[] { "GroupChatId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_SenderUserId",
                schema: "chat",
                table: "chat_messages",
                column: "SenderUserId");

            migrationBuilder.CreateIndex(
                name: "IX_direct_conversation_participants_DirectConversationId_UserId",
                schema: "chat",
                table: "direct_conversation_participants",
                columns: new[] { "DirectConversationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_direct_conversation_participants_UserId",
                schema: "chat",
                table: "direct_conversation_participants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_direct_conversations_UserOneId_UserTwoId",
                schema: "chat",
                table: "direct_conversations",
                columns: new[] { "UserOneId", "UserTwoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_direct_conversations_UserTwoId",
                schema: "chat",
                table: "direct_conversations",
                column: "UserTwoId");

            migrationBuilder.CreateIndex(
                name: "IX_group_chat_participants_GroupChatId_UserId",
                schema: "chat",
                table: "group_chat_participants",
                columns: new[] { "GroupChatId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_group_chat_participants_UserId",
                schema: "chat",
                table: "group_chat_participants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_group_chats_InitiativeId",
                schema: "chat",
                table: "group_chats",
                column: "InitiativeId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "channel_members",
                schema: "chat");

            migrationBuilder.DropTable(
                name: "chat_messages",
                schema: "chat");

            migrationBuilder.DropTable(
                name: "direct_conversation_participants",
                schema: "chat");

            migrationBuilder.DropTable(
                name: "group_chat_participants",
                schema: "chat");

            migrationBuilder.DropTable(
                name: "channels",
                schema: "chat");

            migrationBuilder.DropTable(
                name: "direct_conversations",
                schema: "chat");

            migrationBuilder.DropTable(
                name: "group_chats",
                schema: "chat");
        }
    }
}
