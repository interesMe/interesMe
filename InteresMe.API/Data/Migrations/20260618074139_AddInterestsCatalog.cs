using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InteresMe.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInterestsCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                schema: "interests",
                table: "interests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000102"));

            migrationBuilder.AddColumn<string>(
                name: "Color",
                schema: "interests",
                table: "interests",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "interests",
                table: "interests",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Icon",
                schema: "interests",
                table: "interests",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "interests",
                table: "interests",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                schema: "interests",
                table: "interests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "interest_categories",
                schema: "interests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Icon = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Color = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interest_categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "subinterests",
                schema: "interests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InterestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subinterests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_subinterests_interests_InterestId",
                        column: x => x.InterestId,
                        principalSchema: "interests",
                        principalTable: "interests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_interests_CategoryId",
                schema: "interests",
                table: "interests",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_interest_categories_Slug",
                schema: "interests",
                table: "interest_categories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subinterests_InterestId_Slug",
                schema: "interests",
                table: "subinterests",
                columns: new[] { "InterestId", "Slug" },
                unique: true);

            SeedCatalog(migrationBuilder);

            migrationBuilder.AddForeignKey(
                name: "FK_interests_interest_categories_CategoryId",
                schema: "interests",
                table: "interests",
                column: "CategoryId",
                principalSchema: "interests",
                principalTable: "interest_categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_interests_interest_categories_CategoryId",
                schema: "interests",
                table: "interests");

            migrationBuilder.DropTable(
                name: "interest_categories",
                schema: "interests");

            migrationBuilder.DropTable(
                name: "subinterests",
                schema: "interests");

            migrationBuilder.DropIndex(
                name: "IX_interests_CategoryId",
                schema: "interests",
                table: "interests");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                schema: "interests",
                table: "interests");

            migrationBuilder.DropColumn(
                name: "Color",
                schema: "interests",
                table: "interests");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "interests",
                table: "interests");

            migrationBuilder.DropColumn(
                name: "Icon",
                schema: "interests",
                table: "interests");

            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "interests",
                table: "interests");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                schema: "interests",
                table: "interests");
        }

        private static void SeedCatalog(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO interests.interest_categories ("Id", "Name", "Slug", "Description", "Icon", "Color", "SortOrder", "IsActive")
                VALUES
                    ('00000000-0000-0000-0000-000000000101', 'Create', 'create', 'Creative work, visual arts, music, writing, media and expression.', 'CR', '#fb6f61', 10, true),
                    ('00000000-0000-0000-0000-000000000102', 'Build', 'build', 'Technology, programming, engineering, startups and product creation.', 'BD', '#14b8a6', 20, true),
                    ('00000000-0000-0000-0000-000000000103', 'Learn', 'learn', 'Science, languages, education, research, books and self-development.', 'LR', '#8b5cf6', 30, true),
                    ('00000000-0000-0000-0000-000000000104', 'Move', 'move', 'Sport, dance, travel, outdoor activities and physical challenges.', 'MV', '#22c55e', 40, true),
                    ('00000000-0000-0000-0000-000000000105', 'Impact', 'impact', 'Volunteering, communities, business, ecology and social initiatives.', 'IM', '#f59e0b', 50, true)
                ON CONFLICT ("Slug") DO UPDATE SET
                    "Name" = EXCLUDED."Name",
                    "Description" = EXCLUDED."Description",
                    "Icon" = EXCLUDED."Icon",
                    "Color" = EXCLUDED."Color",
                    "SortOrder" = EXCLUDED."SortOrder",
                    "IsActive" = EXCLUDED."IsActive";

                INSERT INTO interests.interests ("Id", "CategoryId", "Name", "Slug", "Description", "Icon", "Color", "SortOrder", "IsActive", "CreatedAt")
                VALUES
                    ('00000000-0000-0000-0000-000000000201', '00000000-0000-0000-0000-000000000101', 'Design', 'design', 'Shape visuals, interfaces and product experiences.', 'DS', '#fb6f61', 10, true, '2026-06-18T00:00:00Z'),
                    ('00000000-0000-0000-0000-000000000202', '00000000-0000-0000-0000-000000000101', 'Music', 'music', 'Play, write, produce and perform with other people.', 'MS', '#f59e0b', 20, true, '2026-06-18T00:00:00Z'),
                    ('00000000-0000-0000-0000-000000000203', '00000000-0000-0000-0000-000000000101', 'Photography', 'photography', 'Plan shoots, edit images and tell visual stories.', 'PH', '#38bdf8', 30, true, '2026-06-18T00:00:00Z'),
                    ('00000000-0000-0000-0000-000000000204', '00000000-0000-0000-0000-000000000101', 'Cinema', 'cinema', 'Make films, write scenes and gather creative crews.', 'CN', '#ef4444', 40, true, '2026-06-18T00:00:00Z'),
                    ('00000000-0000-0000-0000-000000000205', '00000000-0000-0000-0000-000000000101', 'Art', 'art', 'Draw, paint, explore concepts and share creative practice.', 'AR', '#a855f7', 50, true, '2026-06-18T00:00:00Z'),
                    ('00000000-0000-0000-0000-000000000206', '00000000-0000-0000-0000-000000000102', 'Programming', 'programming', 'Build apps, tools, systems, games and experiments with code.', '</>', '#14b8a6', 10, true, '2026-06-18T00:00:00Z'),
                    ('00000000-0000-0000-0000-000000000207', '00000000-0000-0000-0000-000000000102', 'Startups', 'startups', 'Turn ideas into early products, teams and experiments.', 'SU', '#38bdf8', 20, true, '2026-06-18T00:00:00Z'),
                    ('00000000-0000-0000-0000-000000000208', '00000000-0000-0000-0000-000000000102', 'Engineering', 'engineering', 'Prototype physical systems, devices and technical ideas.', 'EN', '#64748b', 30, true, '2026-06-18T00:00:00Z'),
                    ('00000000-0000-0000-0000-000000000209', '00000000-0000-0000-0000-000000000102', 'Robotics', 'robotics', 'Create machines, drones, automation and sensor projects.', 'RB', '#0ea5e9', 40, true, '2026-06-18T00:00:00Z'),
                    ('00000000-0000-0000-0000-000000000210', '00000000-0000-0000-0000-000000000102', 'Product', 'product', 'Connect user problems, product decisions and outcomes.', 'PD', '#22c55e', 50, true, '2026-06-18T00:00:00Z'),
                    ('00000000-0000-0000-0000-000000000211', '00000000-0000-0000-0000-000000000103', 'Science', 'science', 'Explore research, experiments and curious discussions.', 'SC', '#0ea5e9', 10, true, '2026-06-18T00:00:00Z'),
                    ('00000000-0000-0000-0000-000000000212', '00000000-0000-0000-0000-000000000103', 'Languages', 'languages', 'Practice speaking, writing and learning with others.', 'LA', '#8b5cf6', 20, true, '2026-06-18T00:00:00Z'),
                    ('00000000-0000-0000-0000-000000000213', '00000000-0000-0000-0000-000000000103', 'Education', 'education', 'Study together, prepare for exams and share knowledge.', 'ED', '#14b8a6', 30, true, '2026-06-18T00:00:00Z'),
                    ('00000000-0000-0000-0000-000000000214', '00000000-0000-0000-0000-000000000103', 'Books', 'books', 'Read, discuss ideas and build reading communities.', 'BK', '#f59e0b', 40, true, '2026-06-18T00:00:00Z'),
                    ('00000000-0000-0000-0000-000000000215', '00000000-0000-0000-0000-000000000103', 'Research', 'research', 'Work on papers, analysis, experiments and reports.', 'RS', '#38bdf8', 50, true, '2026-06-18T00:00:00Z'),
                    ('00000000-0000-0000-0000-000000000216', '00000000-0000-0000-0000-000000000104', 'Sport', 'sport', 'Find teams, training partners, games and active communities.', 'SP', '#22c55e', 10, true, '2026-06-18T00:00:00Z'),
                    ('00000000-0000-0000-0000-000000000217', '00000000-0000-0000-0000-000000000104', 'Dance', 'dance', 'Practice movement, choreography and performance.', 'DN', '#ec4899', 20, true, '2026-06-18T00:00:00Z'),
                    ('00000000-0000-0000-0000-000000000218', '00000000-0000-0000-0000-000000000104', 'Travel', 'travel', 'Discover places, routes and cultural experiences together.', 'TR', '#38bdf8', 30, true, '2026-06-18T00:00:00Z'),
                    ('00000000-0000-0000-0000-000000000219', '00000000-0000-0000-0000-000000000104', 'Outdoor', 'outdoor', 'Plan active days outside and try new physical challenges.', 'OD', '#16a34a', 40, true, '2026-06-18T00:00:00Z'),
                    ('00000000-0000-0000-0000-000000000220', '00000000-0000-0000-0000-000000000104', 'Lifestyle', 'lifestyle', 'Build habits, routines and personal challenges.', 'LF', '#f59e0b', 50, true, '2026-06-18T00:00:00Z'),
                    ('00000000-0000-0000-0000-000000000221', '00000000-0000-0000-0000-000000000105', 'Volunteering', 'volunteering', 'Help people, events and causes through practical action.', 'VL', '#22c55e', 10, true, '2026-06-18T00:00:00Z'),
                    ('00000000-0000-0000-0000-000000000222', '00000000-0000-0000-0000-000000000105', 'Community', 'community', 'Create spaces where people can meet, support and organize.', 'CM', '#14b8a6', 20, true, '2026-06-18T00:00:00Z'),
                    ('00000000-0000-0000-0000-000000000223', '00000000-0000-0000-0000-000000000105', 'Business', 'business', 'Learn markets, sales, finance and entrepreneurship.', 'BZ', '#f59e0b', 30, true, '2026-06-18T00:00:00Z'),
                    ('00000000-0000-0000-0000-000000000224', '00000000-0000-0000-0000-000000000105', 'Ecology', 'ecology', 'Work on cleaner habits, climate awareness and local action.', 'EC', '#16a34a', 40, true, '2026-06-18T00:00:00Z'),
                    ('00000000-0000-0000-0000-000000000225', '00000000-0000-0000-0000-000000000105', 'Social Projects', 'social-projects', 'Build initiatives around inclusion, access and youth support.', 'SO', '#ef4444', 50, true, '2026-06-18T00:00:00Z')
                ON CONFLICT ("Slug") DO UPDATE SET
                    "CategoryId" = EXCLUDED."CategoryId",
                    "Name" = EXCLUDED."Name",
                    "Description" = EXCLUDED."Description",
                    "Icon" = EXCLUDED."Icon",
                    "Color" = EXCLUDED."Color",
                    "SortOrder" = EXCLUDED."SortOrder",
                    "IsActive" = EXCLUDED."IsActive";

                UPDATE interests.interests
                SET
                    "CategoryId" = CASE WHEN "CategoryId" = '00000000-0000-0000-0000-000000000000' THEN '00000000-0000-0000-0000-000000000102' ELSE "CategoryId" END,
                    "Description" = CASE WHEN "Description" = '' THEN "Name" ELSE "Description" END,
                    "Icon" = CASE WHEN "Icon" = '' THEN 'IN' ELSE "Icon" END,
                    "Color" = CASE WHEN "Color" = '' THEN '#14b8a6' ELSE "Color" END,
                    "IsActive" = "Slug" IN (
                        'design', 'music', 'photography', 'cinema', 'art',
                        'programming', 'startups', 'engineering', 'robotics', 'product',
                        'science', 'languages', 'education', 'books', 'research',
                        'sport', 'dance', 'travel', 'outdoor', 'lifestyle',
                        'volunteering', 'community', 'business', 'ecology', 'social-projects'
                    );

                WITH seed ("Id", "InterestSlug", "Name", "Slug", "SortOrder") AS (
                    VALUES
                    ('00000000-0000-0000-0000-000000001001'::uuid, 'design', 'UI/UX Design', 'ui-ux-design', 10),
                    ('00000000-0000-0000-0000-000000001002'::uuid, 'design', 'Graphic Design', 'graphic-design', 20),
                    ('00000000-0000-0000-0000-000000001003'::uuid, 'design', 'Motion Design', 'motion-design', 30),
                    ('00000000-0000-0000-0000-000000001004'::uuid, 'design', 'Product Design', 'product-design', 40),
                    ('00000000-0000-0000-0000-000000001005'::uuid, 'music', 'Guitar', 'guitar', 10),
                    ('00000000-0000-0000-0000-000000001006'::uuid, 'music', 'Piano', 'piano', 20),
                    ('00000000-0000-0000-0000-000000001007'::uuid, 'music', 'Vocals', 'vocals', 30),
                    ('00000000-0000-0000-0000-000000001008'::uuid, 'music', 'Production', 'production', 40),
                    ('00000000-0000-0000-0000-000000001009'::uuid, 'photography', 'Portrait', 'portrait', 10),
                    ('00000000-0000-0000-0000-000000001010'::uuid, 'photography', 'Street Photography', 'street-photography', 20),
                    ('00000000-0000-0000-0000-000000001011'::uuid, 'photography', 'Editing', 'editing', 30),
                    ('00000000-0000-0000-0000-000000001012'::uuid, 'photography', 'Visual Storytelling', 'visual-storytelling', 40),
                    ('00000000-0000-0000-0000-000000001013'::uuid, 'cinema', 'Filmmaking', 'filmmaking', 10),
                    ('00000000-0000-0000-0000-000000001014'::uuid, 'cinema', 'Screenwriting', 'screenwriting', 20),
                    ('00000000-0000-0000-0000-000000001015'::uuid, 'cinema', 'Editing', 'editing', 30),
                    ('00000000-0000-0000-0000-000000001016'::uuid, 'cinema', 'Acting', 'acting', 40),
                    ('00000000-0000-0000-0000-000000001017'::uuid, 'art', 'Illustration', 'illustration', 10),
                    ('00000000-0000-0000-0000-000000001018'::uuid, 'art', 'Digital Art', 'digital-art', 20),
                    ('00000000-0000-0000-0000-000000001019'::uuid, 'art', 'Painting', 'painting', 30),
                    ('00000000-0000-0000-0000-000000001020'::uuid, 'art', 'Concept Art', 'concept-art', 40),
                    ('00000000-0000-0000-0000-000000001021'::uuid, 'programming', 'Backend', 'backend', 10),
                    ('00000000-0000-0000-0000-000000001022'::uuid, 'programming', 'Frontend', 'frontend', 20),
                    ('00000000-0000-0000-0000-000000001023'::uuid, 'programming', 'GameDev', 'gamedev', 30),
                    ('00000000-0000-0000-0000-000000001024'::uuid, 'programming', 'AI', 'ai', 40),
                    ('00000000-0000-0000-0000-000000001025'::uuid, 'programming', 'DevOps', 'devops', 50),
                    ('00000000-0000-0000-0000-000000001026'::uuid, 'startups', 'SaaS', 'saas', 10),
                    ('00000000-0000-0000-0000-000000001027'::uuid, 'startups', 'Marketplace', 'marketplace', 20),
                    ('00000000-0000-0000-0000-000000001028'::uuid, 'startups', 'Mobile App', 'mobile-app', 30),
                    ('00000000-0000-0000-0000-000000001029'::uuid, 'startups', 'Student Startup', 'student-startup', 40),
                    ('00000000-0000-0000-0000-000000001030'::uuid, 'engineering', 'Electronics', 'electronics', 10),
                    ('00000000-0000-0000-0000-000000001031'::uuid, 'engineering', 'Mechanics', 'mechanics', 20),
                    ('00000000-0000-0000-0000-000000001032'::uuid, 'engineering', '3D Printing', '3d-printing', 30),
                    ('00000000-0000-0000-0000-000000001033'::uuid, 'engineering', 'Prototyping', 'prototyping', 40),
                    ('00000000-0000-0000-0000-000000001034'::uuid, 'robotics', 'Arduino', 'arduino', 10),
                    ('00000000-0000-0000-0000-000000001035'::uuid, 'robotics', 'Drones', 'drones', 20),
                    ('00000000-0000-0000-0000-000000001036'::uuid, 'robotics', 'Automation', 'automation', 30),
                    ('00000000-0000-0000-0000-000000001037'::uuid, 'robotics', 'Sensors', 'sensors', 40),
                    ('00000000-0000-0000-0000-000000001038'::uuid, 'product', 'Product Management', 'product-management', 10),
                    ('00000000-0000-0000-0000-000000001039'::uuid, 'product', 'Product Design', 'product-design', 20),
                    ('00000000-0000-0000-0000-000000001040'::uuid, 'product', 'Analytics', 'analytics', 30),
                    ('00000000-0000-0000-0000-000000001041'::uuid, 'product', 'User Research', 'user-research', 40),
                    ('00000000-0000-0000-0000-000000001042'::uuid, 'science', 'Physics', 'physics', 10),
                    ('00000000-0000-0000-0000-000000001043'::uuid, 'science', 'Biology', 'biology', 20),
                    ('00000000-0000-0000-0000-000000001044'::uuid, 'science', 'Chemistry', 'chemistry', 30),
                    ('00000000-0000-0000-0000-000000001045'::uuid, 'science', 'Astronomy', 'astronomy', 40),
                    ('00000000-0000-0000-0000-000000001046'::uuid, 'languages', 'English', 'english', 10),
                    ('00000000-0000-0000-0000-000000001047'::uuid, 'languages', 'German', 'german', 20),
                    ('00000000-0000-0000-0000-000000001048'::uuid, 'languages', 'Polish', 'polish', 30),
                    ('00000000-0000-0000-0000-000000001049'::uuid, 'languages', 'Spanish', 'spanish', 40),
                    ('00000000-0000-0000-0000-000000001050'::uuid, 'education', 'Tutoring', 'tutoring', 10),
                    ('00000000-0000-0000-0000-000000001051'::uuid, 'education', 'Study Groups', 'study-groups', 20),
                    ('00000000-0000-0000-0000-000000001052'::uuid, 'education', 'Exams', 'exams', 30),
                    ('00000000-0000-0000-0000-000000001053'::uuid, 'education', 'Courses', 'courses', 40),
                    ('00000000-0000-0000-0000-000000001054'::uuid, 'books', 'Fiction', 'fiction', 10),
                    ('00000000-0000-0000-0000-000000001055'::uuid, 'books', 'Non-fiction', 'non-fiction', 20),
                    ('00000000-0000-0000-0000-000000001056'::uuid, 'books', 'Philosophy', 'philosophy', 30),
                    ('00000000-0000-0000-0000-000000001057'::uuid, 'books', 'Psychology', 'psychology', 40),
                    ('00000000-0000-0000-0000-000000001058'::uuid, 'research', 'Academic Research', 'academic-research', 10),
                    ('00000000-0000-0000-0000-000000001059'::uuid, 'research', 'Data Analysis', 'data-analysis', 20),
                    ('00000000-0000-0000-0000-000000001060'::uuid, 'research', 'Experiments', 'experiments', 30),
                    ('00000000-0000-0000-0000-000000001061'::uuid, 'research', 'Reports', 'reports', 40),
                    ('00000000-0000-0000-0000-000000001062'::uuid, 'sport', 'Gym', 'gym', 10),
                    ('00000000-0000-0000-0000-000000001063'::uuid, 'sport', 'Football', 'football', 20),
                    ('00000000-0000-0000-0000-000000001064'::uuid, 'sport', 'Basketball', 'basketball', 30),
                    ('00000000-0000-0000-0000-000000001065'::uuid, 'sport', 'Running', 'running', 40),
                    ('00000000-0000-0000-0000-000000001066'::uuid, 'dance', 'Hip-Hop', 'hip-hop', 10),
                    ('00000000-0000-0000-0000-000000001067'::uuid, 'dance', 'Contemporary', 'contemporary', 20),
                    ('00000000-0000-0000-0000-000000001068'::uuid, 'dance', 'Ballroom', 'ballroom', 30),
                    ('00000000-0000-0000-0000-000000001069'::uuid, 'dance', 'Choreography', 'choreography', 40),
                    ('00000000-0000-0000-0000-000000001070'::uuid, 'travel', 'City Walks', 'city-walks', 10),
                    ('00000000-0000-0000-0000-000000001071'::uuid, 'travel', 'Hiking', 'hiking', 20),
                    ('00000000-0000-0000-0000-000000001072'::uuid, 'travel', 'Backpacking', 'backpacking', 30),
                    ('00000000-0000-0000-0000-000000001073'::uuid, 'travel', 'Cultural Trips', 'cultural-trips', 40),
                    ('00000000-0000-0000-0000-000000001074'::uuid, 'outdoor', 'Camping', 'camping', 10),
                    ('00000000-0000-0000-0000-000000001075'::uuid, 'outdoor', 'Cycling', 'cycling', 20),
                    ('00000000-0000-0000-0000-000000001076'::uuid, 'outdoor', 'Climbing', 'climbing', 30),
                    ('00000000-0000-0000-0000-000000001077'::uuid, 'outdoor', 'Kayaking', 'kayaking', 40),
                    ('00000000-0000-0000-0000-000000001078'::uuid, 'lifestyle', 'Nutrition', 'nutrition', 10),
                    ('00000000-0000-0000-0000-000000001079'::uuid, 'lifestyle', 'Discipline', 'discipline', 20),
                    ('00000000-0000-0000-0000-000000001080'::uuid, 'lifestyle', 'Morning Routine', 'morning-routine', 30),
                    ('00000000-0000-0000-0000-000000001081'::uuid, 'lifestyle', 'Challenges', 'challenges', 40),
                    ('00000000-0000-0000-0000-000000001082'::uuid, 'volunteering', 'Charity', 'charity', 10),
                    ('00000000-0000-0000-0000-000000001083'::uuid, 'volunteering', 'Animal Help', 'animal-help', 20),
                    ('00000000-0000-0000-0000-000000001084'::uuid, 'volunteering', 'Event Help', 'event-help', 30),
                    ('00000000-0000-0000-0000-000000001085'::uuid, 'volunteering', 'Fundraising', 'fundraising', 40),
                    ('00000000-0000-0000-0000-000000001086'::uuid, 'community', 'Student Clubs', 'student-clubs', 10),
                    ('00000000-0000-0000-0000-000000001087'::uuid, 'community', 'Local Communities', 'local-communities', 20),
                    ('00000000-0000-0000-0000-000000001088'::uuid, 'community', 'Online Communities', 'online-communities', 30),
                    ('00000000-0000-0000-0000-000000001089'::uuid, 'community', 'Events', 'events', 40),
                    ('00000000-0000-0000-0000-000000001090'::uuid, 'business', 'Marketing', 'marketing', 10),
                    ('00000000-0000-0000-0000-000000001091'::uuid, 'business', 'Sales', 'sales', 20),
                    ('00000000-0000-0000-0000-000000001092'::uuid, 'business', 'Finance', 'finance', 30),
                    ('00000000-0000-0000-0000-000000001093'::uuid, 'business', 'Entrepreneurship', 'entrepreneurship', 40),
                    ('00000000-0000-0000-0000-000000001094'::uuid, 'ecology', 'Recycling', 'recycling', 10),
                    ('00000000-0000-0000-0000-000000001095'::uuid, 'ecology', 'Cleanups', 'cleanups', 20),
                    ('00000000-0000-0000-0000-000000001096'::uuid, 'ecology', 'Climate', 'climate', 30),
                    ('00000000-0000-0000-0000-000000001097'::uuid, 'ecology', 'Sustainable Living', 'sustainable-living', 40),
                    ('00000000-0000-0000-0000-000000001098'::uuid, 'social-projects', 'Education Access', 'education-access', 10),
                    ('00000000-0000-0000-0000-000000001099'::uuid, 'social-projects', 'Mental Health Awareness', 'mental-health-awareness', 20),
                    ('00000000-0000-0000-0000-000000001100'::uuid, 'social-projects', 'Inclusion', 'inclusion', 30),
                    ('00000000-0000-0000-0000-000000001101'::uuid, 'social-projects', 'Youth Projects', 'youth-projects', 40)
                )
                INSERT INTO interests.subinterests ("Id", "InterestId", "Name", "Slug", "Description", "SortOrder", "IsActive")
                SELECT seed."Id", interests_table."Id", seed."Name", seed."Slug", NULL, seed."SortOrder", true
                FROM seed
                JOIN interests.interests interests_table ON interests_table."Slug" = seed."InterestSlug"
                ON CONFLICT ("InterestId", "Slug") DO UPDATE SET
                    "Name" = EXCLUDED."Name",
                    "SortOrder" = EXCLUDED."SortOrder",
                    "IsActive" = EXCLUDED."IsActive";
                """);
        }
    }
}
