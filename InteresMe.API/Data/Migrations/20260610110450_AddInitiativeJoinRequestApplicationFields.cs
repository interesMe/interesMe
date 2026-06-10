using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InteresMe.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInitiativeJoinRequestApplicationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Availability",
                schema: "initiatives",
                table: "initiative_join_requests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Contribution",
                schema: "initiatives",
                table: "initiative_join_requests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Experience",
                schema: "initiatives",
                table: "initiative_join_requests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Motivation",
                schema: "initiatives",
                table: "initiative_join_requests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RoleId",
                schema: "initiatives",
                table: "initiative_join_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_initiative_join_requests_RoleId",
                schema: "initiatives",
                table: "initiative_join_requests",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_initiative_join_requests_initiative_roles_RoleId",
                schema: "initiatives",
                table: "initiative_join_requests",
                column: "RoleId",
                principalSchema: "initiatives",
                principalTable: "initiative_roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_initiative_join_requests_initiative_roles_RoleId",
                schema: "initiatives",
                table: "initiative_join_requests");

            migrationBuilder.DropIndex(
                name: "IX_initiative_join_requests_RoleId",
                schema: "initiatives",
                table: "initiative_join_requests");

            migrationBuilder.DropColumn(
                name: "Availability",
                schema: "initiatives",
                table: "initiative_join_requests");

            migrationBuilder.DropColumn(
                name: "Contribution",
                schema: "initiatives",
                table: "initiative_join_requests");

            migrationBuilder.DropColumn(
                name: "Experience",
                schema: "initiatives",
                table: "initiative_join_requests");

            migrationBuilder.DropColumn(
                name: "Motivation",
                schema: "initiatives",
                table: "initiative_join_requests");

            migrationBuilder.DropColumn(
                name: "RoleId",
                schema: "initiatives",
                table: "initiative_join_requests");
        }
    }
}
