using InteresMe.API.Modules.Interests.Models;

namespace InteresMe.API.Modules.Interests.Infrastructure.Seed;

public static class InterestCatalogSeed
{
    public static readonly Guid CreateCategoryId = Guid.Parse("00000000-0000-0000-0000-000000000101");
    public static readonly Guid BuildCategoryId = Guid.Parse("00000000-0000-0000-0000-000000000102");
    public static readonly Guid LearnCategoryId = Guid.Parse("00000000-0000-0000-0000-000000000103");
    public static readonly Guid MoveCategoryId = Guid.Parse("00000000-0000-0000-0000-000000000104");
    public static readonly Guid ImpactCategoryId = Guid.Parse("00000000-0000-0000-0000-000000000105");

    private static readonly DateTime SeedCreatedAt = new(2026, 6, 18, 0, 0, 0, DateTimeKind.Utc);

    public static readonly InterestCategory[] Categories =
    [
        Category(CreateCategoryId, "Create", "create", "Creative work, visual arts, music, writing, media and expression.", "CR", "#fb6f61", 10),
        Category(BuildCategoryId, "Build", "build", "Technology, programming, engineering, startups and product creation.", "BD", "#14b8a6", 20),
        Category(LearnCategoryId, "Learn", "learn", "Science, languages, education, research, books and self-development.", "LR", "#8b5cf6", 30),
        Category(MoveCategoryId, "Move", "move", "Sport, dance, travel, outdoor activities and physical challenges.", "MV", "#22c55e", 40),
        Category(ImpactCategoryId, "Impact", "impact", "Volunteering, communities, business, ecology and social initiatives.", "IM", "#f59e0b", 50)
    ];

    public static readonly Interest[] Interests =
    [
        Interest("00000000-0000-0000-0000-000000000201", CreateCategoryId, "Design", "design", "Shape visuals, interfaces and product experiences.", "DS", "#fb6f61", 10),
        Interest("00000000-0000-0000-0000-000000000202", CreateCategoryId, "Music", "music", "Play, write, produce and perform with other people.", "MS", "#f59e0b", 20),
        Interest("00000000-0000-0000-0000-000000000203", CreateCategoryId, "Photography", "photography", "Plan shoots, edit images and tell visual stories.", "PH", "#38bdf8", 30),
        Interest("00000000-0000-0000-0000-000000000204", CreateCategoryId, "Cinema", "cinema", "Make films, write scenes and gather creative crews.", "CN", "#ef4444", 40),
        Interest("00000000-0000-0000-0000-000000000205", CreateCategoryId, "Art", "art", "Draw, paint, explore concepts and share creative practice.", "AR", "#a855f7", 50),

        Interest("00000000-0000-0000-0000-000000000206", BuildCategoryId, "Programming", "programming", "Build apps, tools, systems, games and experiments with code.", "</>", "#14b8a6", 10),
        Interest("00000000-0000-0000-0000-000000000207", BuildCategoryId, "Startups", "startups", "Turn ideas into early products, teams and experiments.", "SU", "#38bdf8", 20),
        Interest("00000000-0000-0000-0000-000000000208", BuildCategoryId, "Engineering", "engineering", "Prototype physical systems, devices and technical ideas.", "EN", "#64748b", 30),
        Interest("00000000-0000-0000-0000-000000000209", BuildCategoryId, "Robotics", "robotics", "Create machines, drones, automation and sensor projects.", "RB", "#0ea5e9", 40),
        Interest("00000000-0000-0000-0000-000000000210", BuildCategoryId, "Product", "product", "Connect user problems, product decisions and outcomes.", "PD", "#22c55e", 50),

        Interest("00000000-0000-0000-0000-000000000211", LearnCategoryId, "Science", "science", "Explore research, experiments and curious discussions.", "SC", "#0ea5e9", 10),
        Interest("00000000-0000-0000-0000-000000000212", LearnCategoryId, "Languages", "languages", "Practice speaking, writing and learning with others.", "LA", "#8b5cf6", 20),
        Interest("00000000-0000-0000-0000-000000000213", LearnCategoryId, "Education", "education", "Study together, prepare for exams and share knowledge.", "ED", "#14b8a6", 30),
        Interest("00000000-0000-0000-0000-000000000214", LearnCategoryId, "Books", "books", "Read, discuss ideas and build reading communities.", "BK", "#f59e0b", 40),
        Interest("00000000-0000-0000-0000-000000000215", LearnCategoryId, "Research", "research", "Work on papers, analysis, experiments and reports.", "RS", "#38bdf8", 50),

        Interest("00000000-0000-0000-0000-000000000216", MoveCategoryId, "Sport", "sport", "Find teams, training partners, games and active communities.", "SP", "#22c55e", 10),
        Interest("00000000-0000-0000-0000-000000000217", MoveCategoryId, "Dance", "dance", "Practice movement, choreography and performance.", "DN", "#ec4899", 20),
        Interest("00000000-0000-0000-0000-000000000218", MoveCategoryId, "Travel", "travel", "Discover places, routes and cultural experiences together.", "TR", "#38bdf8", 30),
        Interest("00000000-0000-0000-0000-000000000219", MoveCategoryId, "Outdoor", "outdoor", "Plan active days outside and try new physical challenges.", "OD", "#16a34a", 40),
        Interest("00000000-0000-0000-0000-000000000220", MoveCategoryId, "Lifestyle", "lifestyle", "Build habits, routines and personal challenges.", "LF", "#f59e0b", 50),

        Interest("00000000-0000-0000-0000-000000000221", ImpactCategoryId, "Volunteering", "volunteering", "Help people, events and causes through practical action.", "VL", "#22c55e", 10),
        Interest("00000000-0000-0000-0000-000000000222", ImpactCategoryId, "Community", "community", "Create spaces where people can meet, support and organize.", "CM", "#14b8a6", 20),
        Interest("00000000-0000-0000-0000-000000000223", ImpactCategoryId, "Business", "business", "Learn markets, sales, finance and entrepreneurship.", "BZ", "#f59e0b", 30),
        Interest("00000000-0000-0000-0000-000000000224", ImpactCategoryId, "Ecology", "ecology", "Work on cleaner habits, climate awareness and local action.", "EC", "#16a34a", 40),
        Interest("00000000-0000-0000-0000-000000000225", ImpactCategoryId, "Social Projects", "social-projects", "Build initiatives around inclusion, access and youth support.", "SO", "#ef4444", 50)
    ];

    public static readonly Subinterest[] Subinterests =
        BuildSubinterests().ToArray();

    private static InterestCategory Category(
        Guid id,
        string name,
        string slug,
        string description,
        string icon,
        string color,
        int sortOrder) => new()
    {
        Id = id,
        Name = name,
        Slug = slug,
        Description = description,
        Icon = icon,
        Color = color,
        SortOrder = sortOrder,
        IsActive = true
    };

    private static Interest Interest(
        string id,
        Guid categoryId,
        string name,
        string slug,
        string description,
        string icon,
        string color,
        int sortOrder) => new()
    {
        Id = Guid.Parse(id),
        CategoryId = categoryId,
        Name = name,
        Slug = slug,
        Description = description,
        Icon = icon,
        Color = color,
        SortOrder = sortOrder,
        IsActive = true,
        CreatedAt = SeedCreatedAt
    };

    private static IEnumerable<Subinterest> BuildSubinterests()
    {
        var seed = new (string InterestSlug, string[] Names)[]
        {
            ("design", ["UI/UX Design", "Graphic Design", "Motion Design", "Product Design"]),
            ("music", ["Guitar", "Piano", "Vocals", "Production"]),
            ("photography", ["Portrait", "Street Photography", "Editing", "Visual Storytelling"]),
            ("cinema", ["Filmmaking", "Screenwriting", "Editing", "Acting"]),
            ("art", ["Illustration", "Digital Art", "Painting", "Concept Art"]),
            ("programming", ["Backend", "Frontend", "GameDev", "AI", "DevOps"]),
            ("startups", ["SaaS", "Marketplace", "Mobile App", "Student Startup"]),
            ("engineering", ["Electronics", "Mechanics", "3D Printing", "Prototyping"]),
            ("robotics", ["Arduino", "Drones", "Automation", "Sensors"]),
            ("product", ["Product Management", "Product Design", "Analytics", "User Research"]),
            ("science", ["Physics", "Biology", "Chemistry", "Astronomy"]),
            ("languages", ["English", "German", "Polish", "Spanish"]),
            ("education", ["Tutoring", "Study Groups", "Exams", "Courses"]),
            ("books", ["Fiction", "Non-fiction", "Philosophy", "Psychology"]),
            ("research", ["Academic Research", "Data Analysis", "Experiments", "Reports"]),
            ("sport", ["Gym", "Football", "Basketball", "Running"]),
            ("dance", ["Hip-Hop", "Contemporary", "Ballroom", "Choreography"]),
            ("travel", ["City Walks", "Hiking", "Backpacking", "Cultural Trips"]),
            ("outdoor", ["Camping", "Cycling", "Climbing", "Kayaking"]),
            ("lifestyle", ["Nutrition", "Discipline", "Morning Routine", "Challenges"]),
            ("volunteering", ["Charity", "Animal Help", "Event Help", "Fundraising"]),
            ("community", ["Student Clubs", "Local Communities", "Online Communities", "Events"]),
            ("business", ["Marketing", "Sales", "Finance", "Entrepreneurship"]),
            ("ecology", ["Recycling", "Cleanups", "Climate", "Sustainable Living"]),
            ("social-projects", ["Education Access", "Mental Health Awareness", "Inclusion", "Youth Projects"])
        };

        var counter = 1001;

        foreach (var (interestSlug, names) in seed)
        {
            var interest = Interests.Single(item => item.Slug == interestSlug);

            for (var index = 0; index < names.Length; index++)
            {
                var name = names[index];

                yield return new Subinterest
                {
                    Id = Guid.Parse($"00000000-0000-0000-0000-{counter:000000000000}"),
                    InterestId = interest.Id,
                    Name = name,
                    Slug = ToSlug(name),
                    Description = null,
                    SortOrder = (index + 1) * 10,
                    IsActive = true
                };

                counter++;
            }
        }
    }

    private static string ToSlug(string value) =>
        string.Join(
            "-",
            value
                .ToLowerInvariant()
                .Split(new[] { ' ', '/', '_' }, StringSplitOptions.RemoveEmptyEntries))
            .Replace(".", string.Empty);
}
