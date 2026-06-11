using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InteresMe.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInitiativePublicSharing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                schema: "initiatives",
                table: "initiatives",
                type: "character varying(140)",
                maxLength: 140,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Visibility",
                schema: "initiatives",
                table: "initiatives",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql("""
                UPDATE initiatives.initiatives
                SET "Slug" =
                    LEFT(
                        COALESCE(
                            NULLIF(
                                TRIM(BOTH '-' FROM regexp_replace(lower("Title"), '[^[:alnum:]]+', '-', 'g')),
                                ''),
                            'initiative'),
                        131)
                    || '-' || substring(replace("Id"::text, '-', '') from 1 for 8)
                WHERE "Slug" IS NULL OR "Slug" = '';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                schema: "initiatives",
                table: "initiatives",
                type: "character varying(140)",
                maxLength: 140,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(140)",
                oldMaxLength: 140,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_initiatives_Slug",
                schema: "initiatives",
                table: "initiatives",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_initiatives_Visibility",
                schema: "initiatives",
                table: "initiatives",
                column: "Visibility");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_initiatives_Slug",
                schema: "initiatives",
                table: "initiatives");

            migrationBuilder.DropIndex(
                name: "IX_initiatives_Visibility",
                schema: "initiatives",
                table: "initiatives");

            migrationBuilder.DropColumn(
                name: "Slug",
                schema: "initiatives",
                table: "initiatives");

            migrationBuilder.DropColumn(
                name: "Visibility",
                schema: "initiatives",
                table: "initiatives");
        }
    }
}
