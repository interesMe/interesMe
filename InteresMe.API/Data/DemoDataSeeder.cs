using InteresMe.API.Modules.Auth.Models;
using InteresMe.API.Modules.Chat.Channels.Models;
using InteresMe.API.Modules.Chat.DirectMessages.Models;
using InteresMe.API.Modules.Chat.Groups.Models;
using InteresMe.API.Modules.Chat.Shared.Enums;
using InteresMe.API.Modules.Chat.Shared.Models;
using InteresMe.API.Modules.Discovery.Models;
using InteresMe.API.Modules.Initiatives.Domain.Entities;
using InteresMe.API.Modules.Initiatives.Domain.Enums;
using InteresMe.API.Modules.Interests.Models;
using InteresMe.API.Modules.Profile.Models;
using InteresMe.API.Security;
using Microsoft.EntityFrameworkCore;

namespace InteresMe.API.Data;

public static class DemoDataSeeder
{
    private const string DemoEmailDomain = "demo.interesme.local";
    private const string DemoPassword = "Demo123!";

    public static async Task SeedDevelopmentDemoDataAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        using var scope = app.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        var hasDemoUsers = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Email.EndsWith($"@{DemoEmailDomain}"));

        if (hasDemoUsers)
        {
            logger.LogInformation("Development demo data already exists. Checking demo inboxes for local users.");
            await SeedDemoInboxForExistingUsersAsync(dbContext, logger);
            return;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        var now = DateTime.UtcNow;
        var interests = await EnsureInterestsAsync(dbContext, now);
        var users = CreateUsers(now);

        dbContext.Users.AddRange(users);
        dbContext.UserProfiles.AddRange(CreateProfiles(users, now));
        dbContext.UserInterests.AddRange(CreateUserInterests(users, interests, now));
        dbContext.UserDiscoveryPreferences.AddRange(CreateDiscoveryPreferences(users, now));

        var initiatives = CreateInitiatives(users, interests, now);
        dbContext.Initiatives.AddRange(initiatives);
        dbContext.InitiativeJoinRequests.AddRange(CreateAcceptedJoinRequests(initiatives, users, now));

        var directConversations = CreateDirectConversations(users, now);
        dbContext.DirectConversations.AddRange(directConversations.Conversations);
        dbContext.DirectConversationParticipants.AddRange(directConversations.Participants);

        var groupChats = CreateGroupChats(initiatives, users, now);
        dbContext.GroupChats.AddRange(groupChats.Chats);
        dbContext.GroupChatParticipants.AddRange(groupChats.Participants);

        var channels = CreateChannels(initiatives, users, now);
        dbContext.Channels.AddRange(channels.Channels);
        dbContext.ChannelMembers.AddRange(channels.Members);

        dbContext.ChatMessages.AddRange(
            CreateDirectMessages(directConversations.Conversations, users, now)
                .Concat(CreateGroupMessages(groupChats.Chats, groupChats.Participants, users, now))
                .Concat(CreateChannelMessages(channels.Channels, channels.Members, users, now)));

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        logger.LogInformation(
            "Seeded development demo data: {UsersCount} users, {InitiativesCount} initiatives, {DirectCount} direct conversations, {GroupCount} group chats, {ChannelCount} channels. Demo password: {DemoPassword}",
            users.Count,
            initiatives.Count,
            directConversations.Conversations.Count,
            groupChats.Chats.Count,
            channels.Channels.Count,
            DemoPassword);

        await SeedDemoInboxForExistingUsersAsync(dbContext, logger);
    }

    private static async Task<Dictionary<string, Interest>> EnsureInterestsAsync(
        AppDbContext dbContext,
        DateTime now)
    {
        var requested = new[]
        {
            ("Design", "design"),
            ("UI/UX", "ui-ux"),
            ("Startups", "startups"),
            ("Frontend", "frontend"),
            ("Backend", "backend"),
            ("Music", "music"),
            ("Photography", "photography"),
            ("Product", "product"),
            ("Volunteering", "volunteering"),
            ("AI", "ai"),
            ("Study Groups", "study-groups"),
            ("Game Development", "game-development"),
            ("Community", "community"),
            ("Events", "events")
        };

        var slugs = requested.Select(interest => interest.Item2).ToList();
        var existing = await dbContext.Interests
            .Where(interest => slugs.Contains(interest.Slug))
            .ToDictionaryAsync(interest => interest.Slug);

        foreach (var (name, slug) in requested)
        {
            if (existing.ContainsKey(slug))
            {
                continue;
            }

            var interest = new Interest
            {
                Id = Guid.NewGuid(),
                Name = name,
                Slug = slug,
                CreatedAt = now
            };

            dbContext.Interests.Add(interest);
            existing[slug] = interest;
        }

        return existing;
    }

    private static List<User> CreateUsers(DateTime now)
    {
        var users = new[]
        {
            "Anna Designer",
            "Mark Startup Founder",
            "Sarah Musician",
            "Alex Developer",
            "Diana Photographer",
            "Mike Product Manager",
            "Emma Volunteer",
            "Daniel UI Designer",
            "Olivia AI Researcher",
            "Nazar Game Developer"
        };

        return users
            .Select(name => new User
            {
                Id = Guid.NewGuid(),
                Email = $"{ToEmailPrefix(name)}@{DemoEmailDomain}",
                DisplayName = name,
                PasswordHash = PasswordHasher.Hash(DemoPassword),
                CreatedAt = now.AddDays(-12)
            })
            .ToList();
    }

    private static List<UserProfile> CreateProfiles(
        IReadOnlyList<User> users,
        DateTime now)
    {
        var headlines = new Dictionary<string, (string Headline, string Bio, string City)>
        {
            ["Anna Designer"] = ("Product designer and brand storyteller", "Skills: product design, design systems, Figma, visual identity. Looking for meaningful student projects.", "Kyiv"),
            ["Mark Startup Founder"] = ("Early-stage founder building student products", "Skills: pitching, validation, growth, operations. Loves turning rough ideas into weekend prototypes.", "Lviv"),
            ["Sarah Musician"] = ("Vocalist and songwriter", "Skills: songwriting, live performance, audio production. Open to indie collaborations and community events.", "Odesa"),
            ["Alex Developer"] = ("Frontend developer and Angular learner", "Skills: Angular, TypeScript, APIs, accessibility. Happy to build useful tools with small teams.", "Kyiv"),
            ["Diana Photographer"] = ("Portrait and event photographer", "Skills: photo walks, editing, visual storytelling. Interested in creative clubs and student media.", "Dnipro"),
            ["Mike Product Manager"] = ("Product manager for student communities", "Skills: product strategy, research, roadmaps, facilitation. Helps teams decide what to build first.", "Warsaw"),
            ["Emma Volunteer"] = ("Community volunteer and organizer", "Skills: event planning, partnerships, moderation. Focused on practical local impact.", "Kharkiv"),
            ["Daniel UI Designer"] = ("UI designer focused on calm interfaces", "Skills: UI design, prototyping, design critique. Likes polished details and clear flows.", "Ternopil"),
            ["Olivia AI Researcher"] = ("AI study group mentor", "Skills: Python, machine learning basics, study plans. Makes technical topics easier to start.", "Kyiv"),
            ["Nazar Game Developer"] = ("Gameplay programmer and pixel art hobbyist", "Skills: Unity, gameplay loops, level design. Building small games with motivated teammates.", "Ivano-Frankivsk")
        };

        return users.Select(user =>
        {
            var profile = headlines[user.DisplayName];

            return new UserProfile
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                DisplayName = user.DisplayName,
                Headline = profile.Headline,
                Bio = profile.Bio,
                City = profile.City,
                AvatarUrl = $"https://api.dicebear.com/7.x/initials/svg?seed={Uri.EscapeDataString(user.DisplayName)}",
                BirthDate = DateOnly.FromDateTime(now.AddYears(-22).AddDays(user.DisplayName.Length)),
                CreatedAt = now.AddDays(-11),
                UpdatedAt = now.AddDays(-2)
            };
        }).ToList();
    }

    private static List<UserInterest> CreateUserInterests(
        IReadOnlyList<User> users,
        IReadOnlyDictionary<string, Interest> interests,
        DateTime now)
    {
        var byUser = new Dictionary<string, string[]>
        {
            ["Anna Designer"] = ["design", "ui-ux", "product", "community"],
            ["Mark Startup Founder"] = ["startups", "product", "events", "ai"],
            ["Sarah Musician"] = ["music", "events", "community", "design"],
            ["Alex Developer"] = ["frontend", "backend", "ai", "game-development"],
            ["Diana Photographer"] = ["photography", "events", "design", "community"],
            ["Mike Product Manager"] = ["product", "startups", "community", "study-groups"],
            ["Emma Volunteer"] = ["volunteering", "community", "events", "study-groups"],
            ["Daniel UI Designer"] = ["ui-ux", "design", "frontend", "product"],
            ["Olivia AI Researcher"] = ["ai", "study-groups", "backend", "product"],
            ["Nazar Game Developer"] = ["game-development", "frontend", "design", "music"]
        };

        return users
            .SelectMany(user => byUser[user.DisplayName].Select(slug => new UserInterest
            {
                UserId = user.Id,
                InterestId = interests[slug].Id,
                CreatedAt = now.AddDays(-10)
            }))
            .ToList();
    }

    private static List<UserDiscoveryPreference> CreateDiscoveryPreferences(
        IReadOnlyList<User> users,
        DateTime now)
    {
        var goals = new Dictionary<string, UserGoal>
        {
            ["Anna Designer"] = UserGoal.Build,
            ["Mark Startup Founder"] = UserGoal.Build,
            ["Sarah Musician"] = UserGoal.Play,
            ["Alex Developer"] = UserGoal.Learn,
            ["Diana Photographer"] = UserGoal.Explore,
            ["Mike Product Manager"] = UserGoal.Connect,
            ["Emma Volunteer"] = UserGoal.Connect,
            ["Daniel UI Designer"] = UserGoal.Build,
            ["Olivia AI Researcher"] = UserGoal.Learn,
            ["Nazar Game Developer"] = UserGoal.Play
        };

        return users.Select(user => new UserDiscoveryPreference
        {
            UserId = user.Id,
            Goal = goals[user.DisplayName],
            CreatedAt = now.AddDays(-10),
            UpdatedAt = now.AddDays(-1)
        }).ToList();
    }

    private static List<Initiative> CreateInitiatives(
        IReadOnlyList<User> users,
        IReadOnlyDictionary<string, Interest> interests,
        DateTime now)
    {
        var definitions = new[]
        {
            new InitiativeSeed("Student Startup Weekend", "student-startup-weekend-demo", "A weekend sprint for students to validate ideas, form teams, and present a small working prototype.", "Mark Startup Founder", InitiativeGoalType.Build, 24, new[] { "startups", "product", "frontend" }, new[] { "Frontend Developer", "Pitch Coach", "Designer" }),
            new InitiativeSeed("Indie Band Project", "indie-band-project-demo", "A small group for writing original songs, rehearsing weekly, and playing at local student events.", "Sarah Musician", InitiativeGoalType.Play, 6, new[] { "music", "events", "community" }, new[] { "Guitarist", "Producer", "Visual Designer" }),
            new InitiativeSeed("AI Study Group", "ai-study-group-demo", "Beginner-friendly study sessions for machine learning basics, paper reading, and practical Python notebooks.", "Olivia AI Researcher", InitiativeGoalType.Learn, 12, new[] { "ai", "study-groups", "backend" }, new[] { "Study Buddy", "Python Mentor", "Note Taker" }),
            new InitiativeSeed("Photography Club", "photography-club-demo", "Photo walks, editing sessions, and a shared student gallery for portraits and campus stories.", "Diana Photographer", InitiativeGoalType.Explore, 16, new[] { "photography", "events", "design" }, new[] { "Event Photographer", "Editor", "Gallery Curator" }),
            new InitiativeSeed("Volunteer Community", "volunteer-community-demo", "A lightweight community for organizing practical volunteer actions around campus and nearby neighborhoods.", "Emma Volunteer", InitiativeGoalType.Connect, 30, new[] { "volunteering", "community", "events" }, new[] { "Coordinator", "Partnerships", "Content Helper" }),
            new InitiativeSeed("Game Development Team", "game-development-team-demo", "A tiny team building a playable student game prototype with simple mechanics and a clear art direction.", "Nazar Game Developer", InitiativeGoalType.Build, 8, new[] { "game-development", "frontend", "design" }, new[] { "Unity Developer", "Pixel Artist", "Sound Designer" }),
            new InitiativeSeed("Campus UX Lab", "campus-ux-lab-demo", "A design critique and usability testing group for student-made apps and initiative pages.", "Daniel UI Designer", InitiativeGoalType.Learn, 10, new[] { "ui-ux", "design", "product" }, new[] { "Researcher", "Prototype Designer", "Tester" })
        };

        return definitions.Select(definition =>
        {
            var initiative = new Initiative
            {
                Id = Guid.NewGuid(),
                OwnerUserId = UserId(users, definition.OwnerName),
                Title = definition.Title,
                Slug = definition.Slug,
                ShortDescription = definition.Description,
                GoalType = definition.GoalType,
                University = "InteresMe Demo University",
                TeamSize = definition.TeamSize,
                Status = InitiativeStatus.Active,
                Visibility = InitiativeVisibility.Public,
                CreatedAt = now.AddDays(-8),
                UpdatedAt = now.AddHours(-definition.Title.Length)
            };

            foreach (var slug in definition.InterestSlugs)
            {
                initiative.InitiativeInterests.Add(new InitiativeInterest
                {
                    InitiativeId = initiative.Id,
                    InterestId = interests[slug].Id
                });
            }

            foreach (var roleName in definition.Roles)
            {
                initiative.Roles.Add(new InitiativeRole
                {
                    Id = Guid.NewGuid(),
                    InitiativeId = initiative.Id,
                    Name = roleName
                });
            }

            return initiative;
        }).ToList();
    }

    private static List<InitiativeJoinRequest> CreateAcceptedJoinRequests(
        IReadOnlyList<Initiative> initiatives,
        IReadOnlyList<User> users,
        DateTime now)
    {
        var members = new Dictionary<string, string[]>
        {
            ["Student Startup Weekend"] = ["Anna Designer", "Alex Developer", "Mike Product Manager", "Daniel UI Designer"],
            ["Indie Band Project"] = ["Nazar Game Developer", "Diana Photographer", "Emma Volunteer"],
            ["AI Study Group"] = ["Alex Developer", "Mark Startup Founder", "Mike Product Manager", "Daniel UI Designer"],
            ["Photography Club"] = ["Anna Designer", "Sarah Musician", "Emma Volunteer"],
            ["Volunteer Community"] = ["Diana Photographer", "Mike Product Manager", "Sarah Musician", "Anna Designer"],
            ["Game Development Team"] = ["Alex Developer", "Daniel UI Designer", "Sarah Musician"],
            ["Campus UX Lab"] = ["Anna Designer", "Mike Product Manager", "Alex Developer", "Diana Photographer"]
        };

        return initiatives.SelectMany(initiative =>
        {
            var role = initiative.Roles.FirstOrDefault();

            return members[initiative.Title]
                .Where(memberName => UserId(users, memberName) != initiative.OwnerUserId)
                .Select((memberName, index) => new InitiativeJoinRequest
                {
                    Id = Guid.NewGuid(),
                    InitiativeId = initiative.Id,
                    UserId = UserId(users, memberName),
                    RoleId = role?.Id,
                    Message = "Happy to join and help move this forward.",
                    Motivation = "This matches my interests and I can contribute consistently.",
                    Experience = "I have worked on similar student projects and small teams.",
                    Contribution = "I can help with planning, execution, and feedback.",
                    Availability = "A few evenings per week.",
                    Status = InitiativeJoinRequestStatus.Accepted,
                    CreatedAt = now.AddDays(-7).AddHours(index),
                    UpdatedAt = now.AddDays(-6).AddHours(index)
                });
        }).ToList();
    }

    private static DirectSeedResult CreateDirectConversations(
        IReadOnlyList<User> users,
        DateTime now)
    {
        var pairs = new[]
        {
            ("Anna Designer", "Alex Developer"),
            ("Mark Startup Founder", "Mike Product Manager"),
            ("Sarah Musician", "Nazar Game Developer"),
            ("Diana Photographer", "Emma Volunteer"),
            ("Daniel UI Designer", "Olivia AI Researcher")
        };

        var conversations = new List<DirectConversation>();
        var participants = new List<DirectConversationParticipant>();

        foreach (var (firstName, secondName) in pairs)
        {
            var firstId = UserId(users, firstName);
            var secondId = UserId(users, secondName);
            var (userOneId, userTwoId) = OrderUserPair(firstId, secondId);
            var createdAt = now.AddDays(-5).AddHours(conversations.Count * 4);
            var conversation = new DirectConversation
            {
                Id = Guid.NewGuid(),
                UserOneId = userOneId,
                UserTwoId = userTwoId,
                CreatedAt = createdAt,
                UpdatedAt = now.AddHours(-conversations.Count - 1)
            };

            conversations.Add(conversation);
            participants.Add(new DirectConversationParticipant
            {
                Id = Guid.NewGuid(),
                DirectConversationId = conversation.Id,
                UserId = firstId,
                JoinedAt = createdAt
            });
            participants.Add(new DirectConversationParticipant
            {
                Id = Guid.NewGuid(),
                DirectConversationId = conversation.Id,
                UserId = secondId,
                JoinedAt = createdAt
            });
        }

        return new DirectSeedResult(conversations, participants);
    }

    private static GroupSeedResult CreateGroupChats(
        IReadOnlyList<Initiative> initiatives,
        IReadOnlyList<User> users,
        DateTime now)
    {
        var members = new Dictionary<string, string[]>
        {
            ["Student Startup Weekend"] = ["Mark Startup Founder", "Anna Designer", "Alex Developer", "Mike Product Manager", "Daniel UI Designer"],
            ["Indie Band Project"] = ["Sarah Musician", "Nazar Game Developer", "Diana Photographer", "Emma Volunteer"],
            ["AI Study Group"] = ["Olivia AI Researcher", "Alex Developer", "Mark Startup Founder", "Mike Product Manager", "Daniel UI Designer"],
            ["Photography Club"] = ["Diana Photographer", "Anna Designer", "Sarah Musician", "Emma Volunteer"],
            ["Volunteer Community"] = ["Emma Volunteer", "Diana Photographer", "Mike Product Manager", "Sarah Musician", "Anna Designer"],
            ["Game Development Team"] = ["Nazar Game Developer", "Alex Developer", "Daniel UI Designer", "Sarah Musician"],
            ["Campus UX Lab"] = ["Daniel UI Designer", "Anna Designer", "Mike Product Manager", "Alex Developer", "Diana Photographer"]
        };

        var chats = new List<GroupChat>();
        var participants = new List<GroupChatParticipant>();

        foreach (var initiative in initiatives)
        {
            var createdAt = now.AddDays(-6).AddHours(chats.Count);
            var chat = new GroupChat
            {
                Id = Guid.NewGuid(),
                InitiativeId = initiative.Id,
                Title = initiative.Title,
                CreatedAt = createdAt,
                UpdatedAt = now.AddHours(-chats.Count - 1)
            };

            chats.Add(chat);

            foreach (var memberName in members[initiative.Title])
            {
                participants.Add(new GroupChatParticipant
                {
                    Id = Guid.NewGuid(),
                    GroupChatId = chat.Id,
                    UserId = UserId(users, memberName),
                    JoinedAt = createdAt.AddMinutes(participants.Count % 30),
                    Role = UserId(users, memberName) == initiative.OwnerUserId
                        ? ChatParticipantRole.Owner
                        : ChatParticipantRole.Member
                });
            }
        }

        return new GroupSeedResult(chats, participants);
    }

    private static ChannelSeedResult CreateChannels(
        IReadOnlyList<Initiative> initiatives,
        IReadOnlyList<User> users,
        DateTime now)
    {
        var definitions = new[]
        {
            ("Announcements", "Student Startup Weekend", new[] { "Mark Startup Founder", "Anna Designer", "Alex Developer", "Mike Product Manager", "Daniel UI Designer" }),
            ("Team Updates", "Game Development Team", new[] { "Nazar Game Developer", "Alex Developer", "Daniel UI Designer", "Sarah Musician" }),
            ("Events", "Volunteer Community", new[] { "Emma Volunteer", "Diana Photographer", "Mike Product Manager", "Sarah Musician", "Anna Designer" })
        };

        var channels = new List<Channel>();
        var members = new List<ChannelMember>();

        foreach (var (title, initiativeTitle, memberNames) in definitions)
        {
            var initiative = initiatives.First(current => current.Title == initiativeTitle);
            var createdAt = now.AddDays(-4).AddHours(channels.Count * 2);
            var channel = new Channel
            {
                Id = Guid.NewGuid(),
                InitiativeId = initiative.Id,
                Title = title,
                CreatedAt = createdAt,
                UpdatedAt = now.AddHours(-channels.Count - 2)
            };

            channels.Add(channel);

            foreach (var memberName in memberNames)
            {
                var userId = UserId(users, memberName);
                members.Add(new ChannelMember
                {
                    Id = Guid.NewGuid(),
                    ChannelId = channel.Id,
                    UserId = userId,
                    JoinedAt = createdAt.AddMinutes(members.Count % 20),
                    Role = userId == initiative.OwnerUserId
                        ? ChatParticipantRole.Owner
                        : ChatParticipantRole.Member
                });
            }
        }

        return new ChannelSeedResult(channels, members);
    }

    private static IEnumerable<ChatMessage> CreateDirectMessages(
        IReadOnlyList<DirectConversation> conversations,
        IReadOnlyList<User> users,
        DateTime now)
    {
        var messages = new Dictionary<int, (string Sender, string Text)[]>
        {
            [0] =
            [
                ("Anna Designer", "Привіт, побачила твою ініціативу."),
                ("Alex Developer", "Дякую, шукаємо ще frontend розробників."),
                ("Anna Designer", "Я можу допомогти з дизайном."),
                ("Alex Developer", "Супер. Давай сьогодні ввечері подивимось flow разом.")
            ],
            [1] =
            [
                ("Mark Startup Founder", "Can you review the pitch flow before Friday?"),
                ("Mike Product Manager", "Yes. I will leave notes around problem framing and user segment."),
                ("Mark Startup Founder", "Perfect, I want the demo to feel more focused.")
            ],
            [2] =
            [
                ("Sarah Musician", "I heard you make game audio experiments."),
                ("Nazar Game Developer", "A bit. I can help with short loops and menu sounds."),
                ("Sarah Musician", "Great, I can record a few melodic sketches tomorrow.")
            ],
            [3] =
            [
                ("Diana Photographer", "The volunteer event needs a few warm photos."),
                ("Emma Volunteer", "That would help a lot. We need something honest, not staged."),
                ("Diana Photographer", "I can come for the first hour and edit a small set.")
            ],
            [4] =
            [
                ("Daniel UI Designer", "Your AI study outline is clear. Want help with slides?"),
                ("Olivia AI Researcher", "Yes, especially the first session. It should feel beginner-friendly."),
                ("Daniel UI Designer", "I will simplify the first three concepts visually.")
            ]
        };

        for (var index = 0; index < conversations.Count; index++)
        {
            var start = now.AddDays(-4).AddHours(index * 5);

            foreach (var (messageIndex, message) in messages[index].Select((message, messageIndex) => (messageIndex, message)))
            {
                yield return new ChatMessage
                {
                    Id = Guid.NewGuid(),
                    ConversationType = ChatConversationType.Direct,
                    DirectConversationId = conversations[index].Id,
                    SenderUserId = UserId(users, message.Sender),
                    Text = message.Text,
                    CreatedAt = start.AddMinutes(messageIndex * 37),
                    IsDeleted = false
                };
            }
        }
    }

    private static IEnumerable<ChatMessage> CreateGroupMessages(
        IReadOnlyList<GroupChat> chats,
        IReadOnlyList<GroupChatParticipant> participants,
        IReadOnlyList<User> users,
        DateTime now)
    {
        var messageTexts = new[]
        {
            "I added a small agenda for our next sync.",
            "Can everyone drop availability for this week?",
            "I can take the first draft and share it tonight.",
            "This is moving well. The next step should be smaller and testable.",
            "I found a useful reference and will post notes after class.",
            "Let's keep the first milestone simple so we can finish it.",
            "I can help with visuals and polish after the core flow works.",
            "Do we want a short demo at the end of the week?",
            "I updated the plan based on yesterday's feedback.",
            "The idea feels clearer now. We should invite one more person.",
            "I can cover the event checklist and logistics.",
            "Let's document decisions so nobody loses context."
        };

        foreach (var (chatIndex, chat) in chats.Select((chat, chatIndex) => (chatIndex, chat)))
        {
            var chatParticipants = participants
                .Where(participant => participant.GroupChatId == chat.Id)
                .ToList();
            var start = now.AddDays(-3).AddHours(chatIndex * 3);

            for (var i = 0; i < messageTexts.Length; i++)
            {
                var sender = chatParticipants[i % chatParticipants.Count];

                yield return new ChatMessage
                {
                    Id = Guid.NewGuid(),
                    ConversationType = ChatConversationType.Group,
                    GroupChatId = chat.Id,
                    SenderUserId = sender.UserId,
                    Text = $"{UserName(users, sender.UserId).Split(' ')[0]}: {messageTexts[(i + chatIndex) % messageTexts.Length]}",
                    CreatedAt = start.AddMinutes(i * 42 + chatIndex),
                    IsDeleted = false
                };
            }
        }
    }

    private static IEnumerable<ChatMessage> CreateChannelMessages(
        IReadOnlyList<Channel> channels,
        IReadOnlyList<ChannelMember> members,
        IReadOnlyList<User> users,
        DateTime now)
    {
        var posts = new Dictionary<string, string[]>
        {
            ["Announcements"] =
            [
                "Welcome to the startup weekend channel. Key updates will stay here.",
                "Team formation closes tomorrow at 18:00.",
                "Demo rehearsal is planned for Friday afternoon.",
                "Please keep your pitch to three minutes and one clear ask.",
                "Mentor office hours are open after lunch.",
                "Final presentations start at 17:30 in the main room."
            ],
            ["Team Updates"] =
            [
                "Prototype scope is locked: one level, one core loop, one menu.",
                "Art direction references are ready for review.",
                "The first playable build should be ready by Thursday night.",
                "Sound placeholders are enough for this milestone.",
                "Please test with keyboard controls before adding extra input.",
                "Friday sync will focus on polish and bugs only."
            ],
            ["Events"] =
            [
                "Volunteer meetup is confirmed for Saturday morning.",
                "Bring comfortable shoes and a reusable water bottle.",
                "Photo consent reminders are printed and ready.",
                "Partnership checklist is updated in the shared notes.",
                "New members can join the briefing call tonight.",
                "After the event we will collect stories and next actions."
            ]
        };

        foreach (var (channelIndex, channel) in channels.Select((channel, channelIndex) => (channelIndex, channel)))
        {
            var owner = members.First(member =>
                member.ChannelId == channel.Id &&
                member.Role == ChatParticipantRole.Owner);
            var start = now.AddDays(-2).AddHours(channelIndex * 4);

            foreach (var (postIndex, text) in posts[channel.Title].Select((text, postIndex) => (postIndex, text)))
            {
                yield return new ChatMessage
                {
                    Id = Guid.NewGuid(),
                    ConversationType = ChatConversationType.Channel,
                    ChannelId = channel.Id,
                    SenderUserId = owner.UserId,
                    Text = text,
                    CreatedAt = start.AddMinutes(postIndex * 58 + UserName(users, owner.UserId).Length),
                    IsDeleted = false
                };
            }
        }
    }

    private static async Task SeedDemoInboxForExistingUsersAsync(
        AppDbContext dbContext,
        ILogger logger)
    {
        var localUsers = await dbContext.Users
            .Where(user => !user.Email.EndsWith($"@{DemoEmailDomain}"))
            .OrderBy(user => user.CreatedAt)
            .ToListAsync();

        if (localUsers.Count == 0)
        {
            return;
        }

        var demoUsers = await dbContext.Users
            .Where(user => user.Email.EndsWith($"@{DemoEmailDomain}"))
            .ToListAsync();

        if (demoUsers.Count == 0)
        {
            return;
        }

        var groupChats = await dbContext.GroupChats
            .AsNoTracking()
            .Where(groupChat =>
                groupChat.Title == "Student Startup Weekend" ||
                groupChat.Title == "AI Study Group" ||
                groupChat.Title == "Volunteer Community")
            .ToListAsync();

        var channels = await dbContext.Channels
            .AsNoTracking()
            .Where(channel =>
                channel.Title == "Announcements" ||
                channel.Title == "Events")
            .ToListAsync();

        var addedDirectConversations = 0;
        var addedMemberships = 0;
        var addedMessages = 0;
        var now = DateTime.UtcNow;

        foreach (var localUser in localUsers)
        {
            var directResult = await SeedDirectInboxForUserAsync(
                dbContext,
                localUser,
                demoUsers,
                now);

            addedDirectConversations += directResult.Conversations;
            addedMessages += directResult.Messages;

            var groupResult = await SeedGroupInboxForUserAsync(
                dbContext,
                localUser,
                demoUsers,
                groupChats,
                now);

            addedMemberships += groupResult.Memberships;
            addedMessages += groupResult.Messages;

            var channelResult = await SeedChannelInboxForUserAsync(
                dbContext,
                localUser,
                demoUsers,
                channels,
                now);

            addedMemberships += channelResult.Memberships;
            addedMessages += channelResult.Messages;
        }

        if (addedDirectConversations == 0 &&
            addedMemberships == 0 &&
            addedMessages == 0)
        {
            logger.LogInformation("Demo inboxes for local users are already seeded.");
            return;
        }

        await dbContext.SaveChangesAsync();

        logger.LogInformation(
            "Seeded demo inboxes for {LocalUsersCount} local users: {DirectCount} direct conversations, {MembershipCount} memberships, {MessagesCount} messages.",
            localUsers.Count,
            addedDirectConversations,
            addedMemberships,
            addedMessages);
    }

    private static async Task<(int Conversations, int Messages)> SeedDirectInboxForUserAsync(
        AppDbContext dbContext,
        User localUser,
        IReadOnlyList<User> demoUsers,
        DateTime now)
    {
        var directSeeds = new[]
        {
            new LocalDirectSeed(
                "Anna Designer",
                [
                    "Привіт! Побачила твій профіль і подумала, що тобі може бути цікавий Student Startup Weekend.",
                    "Ми шукаємо людину, яка може швидко давати фідбек по продукту і дизайну.",
                    "Якщо маєш 20 хвилин сьогодні, я покажу ідею."
                ]),
            new LocalDirectSeed(
                "Alex Developer",
                [
                    "Hey, I noticed you joined InteresMe. Want to test the new chat flow with us?",
                    "We are checking how initiative groups feel with real conversations.",
                    "Drop a message when you are online."
                ]),
            new LocalDirectSeed(
                "Emma Volunteer",
                [
                    "Привіт, ми збираємо невелику volunteer community.",
                    "Було б класно, якби ти подивився наш план подій.",
                    "Я додала тебе в кілька demo чатів, щоб було видно активність."
                ])
        };

        var conversationsAdded = 0;
        var messagesAdded = 0;

        foreach (var seed in directSeeds)
        {
            var sender = demoUsers.FirstOrDefault(user => user.DisplayName == seed.SenderName);

            if (sender is null)
            {
                continue;
            }

            var (userOneId, userTwoId) = OrderUserPair(localUser.Id, sender.Id);
            var exists = await dbContext.DirectConversations
                .AnyAsync(conversation =>
                    conversation.UserOneId == userOneId &&
                    conversation.UserTwoId == userTwoId);

            if (exists)
            {
                continue;
            }

            var createdAt = now.AddDays(-1).AddHours(conversationsAdded * 2);
            var conversation = new DirectConversation
            {
                Id = Guid.NewGuid(),
                UserOneId = userOneId,
                UserTwoId = userTwoId,
                CreatedAt = createdAt,
                UpdatedAt = now.AddMinutes(-15 - conversationsAdded * 17)
            };

            dbContext.DirectConversations.Add(conversation);
            dbContext.DirectConversationParticipants.AddRange(
                new DirectConversationParticipant
                {
                    Id = Guid.NewGuid(),
                    DirectConversationId = conversation.Id,
                    UserId = localUser.Id,
                    JoinedAt = createdAt
                },
                new DirectConversationParticipant
                {
                    Id = Guid.NewGuid(),
                    DirectConversationId = conversation.Id,
                    UserId = sender.Id,
                    JoinedAt = createdAt
                });

            foreach (var (index, text) in seed.Messages.Select((text, index) => (index, text)))
            {
                dbContext.ChatMessages.Add(new ChatMessage
                {
                    Id = Guid.NewGuid(),
                    ConversationType = ChatConversationType.Direct,
                    DirectConversationId = conversation.Id,
                    SenderUserId = sender.Id,
                    Text = text,
                    CreatedAt = createdAt.AddMinutes(index * 23),
                    IsDeleted = false
                });

                messagesAdded += 1;
            }

            conversationsAdded += 1;
        }

        return (conversationsAdded, messagesAdded);
    }

    private static async Task<(int Memberships, int Messages)> SeedGroupInboxForUserAsync(
        AppDbContext dbContext,
        User localUser,
        IReadOnlyList<User> demoUsers,
        IReadOnlyList<GroupChat> groupChats,
        DateTime now)
    {
        var membershipsAdded = 0;
        var messagesAdded = 0;

        foreach (var groupChat in groupChats)
        {
            var isAlreadyParticipant = await dbContext.GroupChatParticipants
                .AnyAsync(participant =>
                    participant.GroupChatId == groupChat.Id &&
                    participant.UserId == localUser.Id);

            if (isAlreadyParticipant)
            {
                continue;
            }

            var senderId = await dbContext.GroupChatParticipants
                .AsNoTracking()
                .Where(participant => participant.GroupChatId == groupChat.Id)
                .OrderBy(participant => participant.Role == ChatParticipantRole.Owner ? 0 : 1)
                .Select(participant => participant.UserId)
                .FirstOrDefaultAsync();

            if (senderId == Guid.Empty)
            {
                senderId = demoUsers.First().Id;
            }

            var joinedAt = now.AddHours(-8).AddMinutes(membershipsAdded * 11);
            dbContext.GroupChatParticipants.Add(new GroupChatParticipant
            {
                Id = Guid.NewGuid(),
                GroupChatId = groupChat.Id,
                UserId = localUser.Id,
                JoinedAt = joinedAt,
                Role = ChatParticipantRole.Member
            });

            dbContext.ChatMessages.Add(new ChatMessage
            {
                Id = Guid.NewGuid(),
                ConversationType = ChatConversationType.Group,
                GroupChatId = groupChat.Id,
                SenderUserId = senderId,
                Text = $"Welcome {FirstName(localUser.DisplayName)}. Glad to have you in {groupChat.Title}.",
                CreatedAt = joinedAt.AddMinutes(19),
                IsDeleted = false
            });

            membershipsAdded += 1;
            messagesAdded += 1;
        }

        return (membershipsAdded, messagesAdded);
    }

    private static async Task<(int Memberships, int Messages)> SeedChannelInboxForUserAsync(
        AppDbContext dbContext,
        User localUser,
        IReadOnlyList<User> demoUsers,
        IReadOnlyList<Channel> channels,
        DateTime now)
    {
        var membershipsAdded = 0;
        var messagesAdded = 0;

        foreach (var channel in channels)
        {
            var isAlreadyMember = await dbContext.ChannelMembers
                .AnyAsync(member =>
                    member.ChannelId == channel.Id &&
                    member.UserId == localUser.Id);

            if (isAlreadyMember)
            {
                continue;
            }

            var ownerId = await dbContext.ChannelMembers
                .AsNoTracking()
                .Where(member => member.ChannelId == channel.Id)
                .OrderBy(member => member.Role == ChatParticipantRole.Owner ? 0 : 1)
                .Select(member => member.UserId)
                .FirstOrDefaultAsync();

            if (ownerId == Guid.Empty)
            {
                ownerId = demoUsers.First().Id;
            }

            var joinedAt = now.AddHours(-5).AddMinutes(membershipsAdded * 13);
            dbContext.ChannelMembers.Add(new ChannelMember
            {
                Id = Guid.NewGuid(),
                ChannelId = channel.Id,
                UserId = localUser.Id,
                JoinedAt = joinedAt,
                Role = ChatParticipantRole.Member
            });

            dbContext.ChatMessages.Add(new ChatMessage
            {
                Id = Guid.NewGuid(),
                ConversationType = ChatConversationType.Channel,
                ChannelId = channel.Id,
                SenderUserId = ownerId,
                Text = $"{FirstName(localUser.DisplayName)} joined the channel. Keep an eye on updates here.",
                CreatedAt = joinedAt.AddMinutes(14),
                IsDeleted = false
            });

            membershipsAdded += 1;
            messagesAdded += 1;
        }

        return (membershipsAdded, messagesAdded);
    }

    private static Guid UserId(IReadOnlyList<User> users, string displayName) =>
        users.First(user => user.DisplayName == displayName).Id;

    private static string UserName(IReadOnlyList<User> users, Guid userId) =>
        users.First(user => user.Id == userId).DisplayName;

    private static string ToEmailPrefix(string displayName) =>
        displayName
            .ToLowerInvariant()
            .Replace(" ", ".", StringComparison.Ordinal)
            .Replace("-", ".", StringComparison.Ordinal);

    private static (Guid UserOneId, Guid UserTwoId) OrderUserPair(Guid firstUserId, Guid secondUserId) =>
        string.CompareOrdinal(firstUserId.ToString("N"), secondUserId.ToString("N")) <= 0
            ? (firstUserId, secondUserId)
            : (secondUserId, firstUserId);

    private static string FirstName(string displayName) =>
        displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "there";

    private sealed record InitiativeSeed(
        string Title,
        string Slug,
        string Description,
        string OwnerName,
        InitiativeGoalType GoalType,
        int TeamSize,
        string[] InterestSlugs,
        string[] Roles);

    private sealed record DirectSeedResult(
        List<DirectConversation> Conversations,
        List<DirectConversationParticipant> Participants);

    private sealed record GroupSeedResult(
        List<GroupChat> Chats,
        List<GroupChatParticipant> Participants);

    private sealed record ChannelSeedResult(
        List<Channel> Channels,
        List<ChannelMember> Members);

    private sealed record LocalDirectSeed(
        string SenderName,
        string[] Messages);
}
