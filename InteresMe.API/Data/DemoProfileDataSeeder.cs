using System.Text.Json;
using InteresMe.API.Modules.Auth.Models;
using InteresMe.API.Modules.History.Models;
using InteresMe.API.Modules.History.Services;
using InteresMe.API.Modules.Initiatives.Domain.Entities;
using InteresMe.API.Modules.Initiatives.Domain.Enums;
using InteresMe.API.Modules.Interests.Models;
using InteresMe.API.Modules.Posts.Feed.Models;
using InteresMe.API.Modules.Profile.Models;
using Microsoft.EntityFrameworkCore;

namespace InteresMe.API.Data;

internal static class DemoProfileDataSeeder
{
    private const string DemoEmailDomain = "demo.interesme.local";

    public static async Task SeedAsync(AppDbContext dbContext, ILogger logger)
    {
        var users = await dbContext.Users
            .Include(user => user.Profile)
            .OrderBy(user => user.CreatedAt)
            .ToListAsync();

        if (users.Count == 0)
        {
            return;
        }

        var interests = await dbContext.Interests
            .Where(interest => new[]
            {
                "programming",
                "startups",
                "languages",
                "sport",
                "design",
                "photography",
                "community",
                "volunteering"
            }.Contains(interest.Slug))
            .ToDictionaryAsync(interest => interest.Slug);

        var now = DateTime.UtcNow;
        var localUsers = users
            .Where(user => !user.Email.EndsWith($"@{DemoEmailDomain}"))
            .ToList();
        var demoUsers = users
            .Where(user => user.Email.EndsWith($"@{DemoEmailDomain}"))
            .ToList();

        SeedProfiles(dbContext, users, now);
        await SeedInterestsAsync(dbContext, localUsers, demoUsers, interests, now);
        await SeedSubinterestsAsync(dbContext, localUsers, demoUsers, now);
        await SeedPostsAsync(dbContext, localUsers, demoUsers, now);

        var mainLocalUser = localUsers.FirstOrDefault();
        if (mainLocalUser is not null)
        {
            await SeedMainUserInitiativesAsync(dbContext, mainLocalUser, demoUsers, interests, now);
            await SeedFollowsAsync(dbContext, mainLocalUser, demoUsers, now);
        }

        await SeedHistoryAsync(dbContext, localUsers, demoUsers, now);
        await dbContext.SaveChangesAsync();

        logger.LogInformation(
            "Demo profile content is ready for {LocalUsersCount} local users and {DemoUsersCount} demo users.",
            localUsers.Count,
            demoUsers.Count);
    }

    private static void SeedProfiles(
        AppDbContext dbContext,
        IReadOnlyCollection<User> users,
        DateTime now)
    {
        foreach (var user in users)
        {
            if (user.Profile is null)
            {
                user.Profile = new UserProfile
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    DisplayName = user.DisplayName,
                    Bio = DefaultBio(user),
                    City = "Kyiv",
                    CreatedAt = now.AddMonths(-2),
                    UpdatedAt = now
                };

                dbContext.UserProfiles.Add(user.Profile);
            }
            else if (string.IsNullOrWhiteSpace(user.Profile.Bio))
            {
                user.Profile.Bio = DefaultBio(user);
                user.Profile.UpdatedAt = now;
            }

            if (string.IsNullOrWhiteSpace(user.Profile.SocialLinksJson))
            {
                user.Profile.SocialLinksJson = JsonSerializer.Serialize(SocialLinks(user));
                user.Profile.UpdatedAt = now;
            }

            if (string.IsNullOrWhiteSpace(user.Profile.ProfileStatus))
            {
                user.Profile.ProfileStatus = ProfileStatus(user);
                user.Profile.UpdatedAt = now;
            }
        }
    }

    private static async Task SeedSubinterestsAsync(
        AppDbContext dbContext,
        IReadOnlyCollection<User> localUsers,
        IReadOnlyCollection<User> demoUsers,
        DateTime now)
    {
        var selections = new Dictionary<Guid, string[]>();

        foreach (var localUser in localUsers)
        {
            selections[localUser.Id] = ["backend", "german", "football"];
        }

        var anna = demoUsers.FirstOrDefault(user => user.DisplayName == "Anna Designer");
        if (anna is not null)
        {
            selections[anna.Id] = ["ui-ux-design"];
        }

        if (selections.Count == 0)
        {
            return;
        }

        var requestedSlugs = selections.Values.SelectMany(slugs => slugs).Distinct().ToList();
        var subinterests = await dbContext.Subinterests
            .Where(subinterest => requestedSlugs.Contains(subinterest.Slug))
            .ToDictionaryAsync(subinterest => subinterest.Slug);
        var userIds = selections.Keys.ToList();
        var existing = await dbContext.UserSubinterests
            .AsNoTracking()
            .Where(selection => userIds.Contains(selection.UserId))
            .Select(selection => new { selection.UserId, selection.SubinterestId })
            .ToListAsync();
        var existingPairs = existing
            .Select(item => (item.UserId, item.SubinterestId))
            .ToHashSet();

        foreach (var (userId, slugs) in selections)
        {
            foreach (var slug in slugs)
            {
                if (!subinterests.TryGetValue(slug, out var subinterest) ||
                    existingPairs.Contains((userId, subinterest.Id)))
                {
                    continue;
                }

                dbContext.UserSubinterests.Add(new UserSubinterest
                {
                    UserId = userId,
                    SubinterestId = subinterest.Id,
                    CreatedAt = now.AddDays(-27)
                });
            }
        }
    }

    private static async Task SeedInterestsAsync(
        AppDbContext dbContext,
        IReadOnlyCollection<User> localUsers,
        IReadOnlyCollection<User> demoUsers,
        IReadOnlyDictionary<string, Interest> interests,
        DateTime now)
    {
        var requested = new Dictionary<Guid, string[]>();

        foreach (var localUser in localUsers)
        {
            requested[localUser.Id] = ["programming", "startups", "languages", "sport"];
        }

        var anna = demoUsers.FirstOrDefault(user => user.DisplayName == "Anna Designer");
        if (anna is not null)
        {
            requested[anna.Id] = ["design", "photography", "community", "volunteering"];
        }

        if (requested.Count == 0)
        {
            return;
        }

        var localUserIds = localUsers.Select(user => user.Id).ToList();
        var localInterestIds = new[] { "programming", "startups", "languages", "sport" }
            .Where(interests.ContainsKey)
            .Select(slug => interests[slug].Id)
            .ToList();

        if (localUserIds.Count > 0)
        {
            await dbContext.UserInterests
                .Where(selection =>
                    localUserIds.Contains(selection.UserId) &&
                    !localInterestIds.Contains(selection.InterestId))
                .ExecuteDeleteAsync();
        }

        var userIds = requested.Keys.ToList();
        var existing = await dbContext.UserInterests
            .AsNoTracking()
            .Where(userInterest => userIds.Contains(userInterest.UserId))
            .Select(userInterest => new { userInterest.UserId, userInterest.InterestId })
            .ToListAsync();
        var existingPairs = existing
            .Select(item => (item.UserId, item.InterestId))
            .ToHashSet();

        foreach (var (userId, slugs) in requested)
        {
            foreach (var slug in slugs)
            {
                if (!interests.TryGetValue(slug, out var interest) ||
                    existingPairs.Contains((userId, interest.Id)))
                {
                    continue;
                }

                dbContext.UserInterests.Add(new UserInterest
                {
                    UserId = userId,
                    InterestId = interest.Id,
                    CreatedAt = now.AddDays(-28)
                });
                existingPairs.Add((userId, interest.Id));
            }
        }
    }

    private static async Task SeedPostsAsync(
        AppDbContext dbContext,
        IReadOnlyCollection<User> localUsers,
        IReadOnlyCollection<User> demoUsers,
        DateTime now)
    {
        var definitions = new Dictionary<Guid, PostSeed[]>();

        foreach (var localUser in localUsers)
        {
            definitions[localUser.Id] =
            [
                new("Connected profile view, history, interests and follow system into one user journey page.", 1),
                new("Need a few students to create test initiatives, join them and break the flow before real users do.", 3),
                new("Thinking about weekly football groups where people join by interest, not by awkward random chats.", 6)
            ];
        }

        AddUserPosts(definitions, demoUsers, "Anna Designer",
        [
            new("A calm space where student initiatives can explain what they need and find people ready to help.", 2),
            new("Testing a shorter path from choosing interests to joining a first meaningful initiative.", 5),
            new("I have the flow and prototype. Looking for someone who enjoys accessible Angular interfaces.", 8)
        ]);
        AddUserPosts(definitions, demoUsers, "Sarah Musician",
        [
            new("Trying shorter weekly sessions focused on one song and one concrete result.", 2),
            new("Our student band needs someone who enjoys indie arrangements and collaborative writing.", 7)
        ]);
        AddUserPosts(definitions, demoUsers, "Diana Photographer",
        [
            new("We collected quiet student stories from places people usually walk past.", 3),
            new("Planning a small outdoor portrait session for beginners and curious volunteers.", 9)
        ]);
        AddUserPosts(definitions, demoUsers, "Mark Startup Founder",
        [
            new("The strongest signal is still whether students return to work together after the first meetup.", 4),
            new("Looking for teams that want direct product feedback instead of polished presentation advice.", 10)
        ]);

        if (definitions.Count == 0)
        {
            return;
        }

        var userIds = definitions.Keys.ToList();
        var existing = await dbContext.Posts
            .Where(post => userIds.Contains(post.AuthorId))
            .ToListAsync();
        var existingBodies = existing
            .Select(post => (post.AuthorId, post.Body))
            .ToHashSet();

        foreach (var (userId, posts) in definitions)
        {
            foreach (var post in posts)
            {
                if (existingBodies.Contains((userId, post.Body)))
                {
                    continue;
                }

                dbContext.Posts.Add(new Post
                {
                    Id = Guid.NewGuid(),
                    AuthorId = userId,
                    Body = post.Body,
                    CreatedAt = now.AddDays(-post.DaysAgo)
                });
            }
        }
    }

    private static async Task SeedMainUserInitiativesAsync(
        AppDbContext dbContext,
        User mainUser,
        IReadOnlyCollection<User> demoUsers,
        IReadOnlyDictionary<string, Interest> interests,
        DateTime now)
    {
        var existingTitles = await dbContext.Initiatives
            .AsNoTracking()
            .Select(initiative => initiative.Title)
            .ToListAsync();

        AddInitiativeIfMissing(
            dbContext,
            existingTitles,
            mainUser.Id,
            "InteresMe",
            "interesme-local-demo",
            "A student platform built around interests, initiatives, and meaningful participation.",
            InitiativeGoalType.Build,
            interests,
            ["programming", "startups"],
            ["Frontend Developer", "Student Tester"],
            now.AddDays(-36));
        AddInitiativeIfMissing(
            dbContext,
            existingTitles,
            mainUser.Id,
            "Student Hackathon Team",
            "student-hackathon-team-local-demo",
            "A small team preparing a practical prototype for the next student hackathon.",
            InitiativeGoalType.Build,
            interests,
            ["programming", "startups"],
            ["Designer", "Backend Developer"],
            now.AddDays(-18));

        var footballOwner = demoUsers.FirstOrDefault(user => user.DisplayName == "Alex Developer") ?? demoUsers.FirstOrDefault();
        var startupOwner = demoUsers.FirstOrDefault(user => user.DisplayName == "Mark Startup Founder") ?? demoUsers.FirstOrDefault();

        if (footballOwner is not null)
        {
            AddInitiativeIfMissing(
                dbContext,
                existingTitles,
                footballOwner.Id,
                "Weekly Football Team",
                "weekly-football-team-demo",
                "Friendly weekly football sessions for students who want a consistent team without awkward recruiting chats.",
                InitiativeGoalType.Play,
                interests,
                ["sport"],
                ["Player", "Organizer"],
                now.AddDays(-24));
        }

        if (startupOwner is not null)
        {
            AddInitiativeIfMissing(
                dbContext,
                existingTitles,
                startupOwner.Id,
                "Student Startup Club",
                "student-startup-club-demo",
                "A practical club for validating student product ideas and forming small teams.",
                InitiativeGoalType.Build,
                interests,
                ["startups"],
                ["Builder", "Product Researcher"],
                now.AddDays(-29));
        }

        await dbContext.SaveChangesAsync();

        var joinedInitiatives = await dbContext.Initiatives
            .Where(initiative =>
                initiative.Title == "Weekly Football Team" ||
                initiative.Title == "Student Startup Club")
            .ToListAsync();
        var existingJoins = await dbContext.InitiativeJoinRequests
            .AsNoTracking()
            .Where(joinRequest =>
                joinRequest.UserId == mainUser.Id &&
                joinedInitiatives.Select(initiative => initiative.Id).Contains(joinRequest.InitiativeId) &&
                joinRequest.Status == InitiativeJoinRequestStatus.Accepted)
            .Select(joinRequest => joinRequest.InitiativeId)
            .ToListAsync();

        foreach (var initiative in joinedInitiatives.Where(initiative => !existingJoins.Contains(initiative.Id)))
        {
            dbContext.InitiativeJoinRequests.Add(new InitiativeJoinRequest
            {
                Id = Guid.NewGuid(),
                InitiativeId = initiative.Id,
                UserId = mainUser.Id,
                Status = InitiativeJoinRequestStatus.Accepted,
                Message = "Joining through development demo data.",
                CreatedAt = now.AddDays(-12),
                UpdatedAt = now.AddDays(-11)
            });
        }
    }

    private static async Task SeedFollowsAsync(
        AppDbContext dbContext,
        User mainUser,
        IReadOnlyCollection<User> demoUsers,
        DateTime now)
    {
        var demoFollowers = demoUsers.Take(4).ToList();
        var pairs = demoFollowers
            .Select(user => (FollowerId: user.Id, FollowedId: mainUser.Id))
            .Concat(demoFollowers.Take(2).Select(user => (FollowerId: mainUser.Id, FollowedId: user.Id)))
            .ToList();
        var userIds = pairs.SelectMany(pair => new[] { pair.FollowerId, pair.FollowedId }).Distinct().ToList();
        var existing = await dbContext.UserFollows
            .AsNoTracking()
            .Where(follow => userIds.Contains(follow.FollowerId) && userIds.Contains(follow.FollowedId))
            .Select(follow => new { follow.FollowerId, follow.FollowedId })
            .ToListAsync();
        var existingPairs = existing.Select(item => (item.FollowerId, item.FollowedId)).ToHashSet();

        foreach (var pair in pairs.Where(pair => !existingPairs.Contains(pair)))
        {
            dbContext.UserFollows.Add(new UserFollow
            {
                FollowerId = pair.FollowerId,
                FollowedId = pair.FollowedId,
                CreatedAt = now.AddDays(-5)
            });
        }
    }

    private static async Task SeedHistoryAsync(
        AppDbContext dbContext,
        IReadOnlyCollection<User> localUsers,
        IReadOnlyCollection<User> demoUsers,
        DateTime now)
    {
        var targetUsers = localUsers
            .Concat(demoUsers.Where(user => user.DisplayName is "Anna Designer" or "Sarah Musician" or "Diana Photographer"))
            .ToList();
        var userIds = targetUsers.Select(user => user.Id).ToList();
        var existingUsers = await dbContext.UserHistoryEvents
            .AsNoTracking()
            .Where(historyEvent => userIds.Contains(historyEvent.UserId))
            .Select(historyEvent => historyEvent.UserId)
            .Distinct()
            .ToListAsync();

        foreach (var user in targetUsers.Where(user => !existingUsers.Contains(user.Id)))
        {
            dbContext.UserHistoryEvents.AddRange(
                HistoryEvent(user.Id, HistoryEventTypes.AddedInterest, "Mapped meaningful interests", "Added interests that make discovery more useful.", "interest", null, "Interest map", now.AddDays(-20)),
                HistoryEvent(user.Id, HistoryEventTypes.JoinedInitiative, "Joined a new initiative", "Started contributing to a small student team.", "initiative", null, "Student initiative", now.AddDays(-11)),
                HistoryEvent(user.Id, HistoryEventTypes.CreatedPost, "Shared a profile update", "Published a short update about current work and ideas.", "post", null, "Profile post", now.AddDays(-3)));
        }
    }

    private static void AddInitiativeIfMissing(
        AppDbContext dbContext,
        ICollection<string> existingTitles,
        Guid ownerUserId,
        string title,
        string slug,
        string description,
        InitiativeGoalType goalType,
        IReadOnlyDictionary<string, Interest> interests,
        string[] interestSlugs,
        string[] roles,
        DateTime createdAt)
    {
        if (existingTitles.Contains(title))
        {
            return;
        }

        var initiative = new Initiative
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            Title = title,
            Slug = slug,
            ShortDescription = description,
            GoalType = goalType,
            TeamSize = 8,
            Status = InitiativeStatus.Active,
            Visibility = InitiativeVisibility.Public,
            CreatedAt = createdAt,
            UpdatedAt = createdAt.AddDays(2)
        };

        foreach (var interestSlug in interestSlugs)
        {
            if (interests.TryGetValue(interestSlug, out var interest))
            {
                initiative.InitiativeInterests.Add(new InitiativeInterest
                {
                    InitiativeId = initiative.Id,
                    InterestId = interest.Id
                });
            }
        }

        foreach (var role in roles)
        {
            initiative.Roles.Add(new InitiativeRole
            {
                Id = Guid.NewGuid(),
                InitiativeId = initiative.Id,
                Name = role
            });
        }

        dbContext.Initiatives.Add(initiative);
        existingTitles.Add(title);
    }

    private static UserHistoryEvent HistoryEvent(
        Guid userId,
        string type,
        string title,
        string description,
        string targetType,
        Guid? targetId,
        string targetName,
        DateTime occurredAt) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Type = type,
        Title = title,
        Description = description,
        TargetType = targetType,
        TargetId = targetId,
        TargetName = targetName,
        OccurredAt = new DateTimeOffset(occurredAt),
        CreatedAt = new DateTimeOffset(occurredAt)
    };

    private static void AddUserPosts(
        IDictionary<Guid, PostSeed[]> definitions,
        IEnumerable<User> users,
        string displayName,
        PostSeed[] posts)
    {
        var user = users.FirstOrDefault(item => item.DisplayName == displayName);
        if (user is not null)
        {
            definitions[user.Id] = posts;
        }
    }

    private static string DefaultBio(User user) =>
        user.Email.EndsWith($"@{DemoEmailDomain}")
            ? $"Building meaningful student projects around {user.DisplayName.Split(' ').Last().ToLowerInvariant()} and collaboration."
            : "Building InteresMe as a place where interests become initiatives, teams, and visible personal journeys.";

    private static string ProfileStatus(User user) =>
        user.DisplayName switch
        {
            "Anna Designer" => "community_builder",
            "Mark Startup Founder" => "initiative_creator",
            "Emma Volunteer" => "community_builder",
            _ when !user.Email.EndsWith($"@{DemoEmailDomain}") => "initiative_creator",
            _ => "active_member"
        };

    private static object[] SocialLinks(User user)
    {
        if (!user.Email.EndsWith($"@{DemoEmailDomain}"))
        {
            return
            [
                new { type = "instagram", label = "Instagram", url = "https://instagram.com/max.dev" },
                new { type = "github", label = "GitHub", url = "https://github.com/maxum" },
                new { type = "telegram", label = "Telegram", url = "https://t.me/maxum" }
            ];
        }

        var handle = string.Concat(user.DisplayName.Where(char.IsLetter)).ToLowerInvariant();
        return
        [
            new { type = "instagram", label = "Instagram", url = $"https://instagram.com/{handle}" },
            new { type = "github", label = "GitHub", url = $"https://github.com/{handle}" }
        ];
    }

    private sealed record PostSeed(
        string Body,
        int DaysAgo);
}
