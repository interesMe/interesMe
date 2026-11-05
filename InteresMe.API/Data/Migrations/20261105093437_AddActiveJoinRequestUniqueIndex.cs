using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InteresMe.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddActiveJoinRequestUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Data clean-up so the unique index can be created on existing databases:
            // keep one active request per (initiative, user) - an accepted one if present, otherwise the
            // earliest pending one - and mark the remaining active duplicates as rejected (Status = 3).
            migrationBuilder.Sql(@"
                WITH ranked AS (
                    SELECT ""Id"",
                           ROW_NUMBER() OVER (
                               PARTITION BY ""InitiativeId"", ""UserId""
                               ORDER BY (""Status"" = 2) DESC, ""CreatedAt"", ""Id"") AS rn
                    FROM initiatives.initiative_join_requests
                    WHERE ""Status"" IN (1, 2)
                )
                UPDATE initiatives.initiative_join_requests AS jr
                SET ""Status"" = 3, ""UpdatedAt"" = now() AT TIME ZONE 'utc'
                FROM ranked
                WHERE jr.""Id"" = ranked.""Id"" AND ranked.rn > 1;");

            migrationBuilder.CreateIndex(
                name: "ux_initiative_join_requests_active",
                schema: "initiatives",
                table: "initiative_join_requests",
                columns: new[] { "InitiativeId", "UserId" },
                unique: true,
                filter: "\"Status\" IN (1, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_initiative_join_requests_active",
                schema: "initiatives",
                table: "initiative_join_requests");
        }
    }
}
