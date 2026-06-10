using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InteresMe.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInitiativesModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "initiatives");

            migrationBuilder.CreateTable(
                name: "initiatives",
                schema: "initiatives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ShortDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    GoalType = table.Column<int>(type: "integer", nullable: false),
                    University = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    TeamSize = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_initiatives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_initiatives_users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalSchema: "auth",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "initiative_interests",
                schema: "initiatives",
                columns: table => new
                {
                    InitiativeId = table.Column<Guid>(type: "uuid", nullable: false),
                    InterestId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_initiative_interests", x => new { x.InitiativeId, x.InterestId });
                    table.ForeignKey(
                        name: "FK_initiative_interests_initiatives_InitiativeId",
                        column: x => x.InitiativeId,
                        principalSchema: "initiatives",
                        principalTable: "initiatives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_initiative_interests_interests_InterestId",
                        column: x => x.InterestId,
                        principalSchema: "interests",
                        principalTable: "interests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "initiative_join_requests",
                schema: "initiatives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InitiativeId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_initiative_join_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_initiative_join_requests_initiatives_InitiativeId",
                        column: x => x.InitiativeId,
                        principalSchema: "initiatives",
                        principalTable: "initiatives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_initiative_join_requests_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "auth",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "initiative_roles",
                schema: "initiatives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InitiativeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_initiative_roles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_initiative_roles_initiatives_InitiativeId",
                        column: x => x.InitiativeId,
                        principalSchema: "initiatives",
                        principalTable: "initiatives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_initiative_interests_InterestId",
                schema: "initiatives",
                table: "initiative_interests",
                column: "InterestId");

            migrationBuilder.CreateIndex(
                name: "IX_initiative_join_requests_InitiativeId",
                schema: "initiatives",
                table: "initiative_join_requests",
                column: "InitiativeId");

            migrationBuilder.CreateIndex(
                name: "IX_initiative_join_requests_UserId",
                schema: "initiatives",
                table: "initiative_join_requests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_initiative_roles_InitiativeId",
                schema: "initiatives",
                table: "initiative_roles",
                column: "InitiativeId");

            migrationBuilder.CreateIndex(
                name: "IX_initiatives_GoalType",
                schema: "initiatives",
                table: "initiatives",
                column: "GoalType");

            migrationBuilder.CreateIndex(
                name: "IX_initiatives_OwnerUserId",
                schema: "initiatives",
                table: "initiatives",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_initiatives_Status",
                schema: "initiatives",
                table: "initiatives",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "initiative_interests",
                schema: "initiatives");

            migrationBuilder.DropTable(
                name: "initiative_join_requests",
                schema: "initiatives");

            migrationBuilder.DropTable(
                name: "initiative_roles",
                schema: "initiatives");

            migrationBuilder.DropTable(
                name: "initiatives",
                schema: "initiatives");
        }
    }
}
