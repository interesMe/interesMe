using InteresMe.API.Modules.Auth.Models;
using InteresMe.API.Modules.Chat.Channels.Models;
using InteresMe.API.Modules.Chat.DirectMessages.Models;
using InteresMe.API.Modules.Chat.Groups.Models;
using InteresMe.API.Modules.Chat.Shared.Enums;
using InteresMe.API.Modules.Chat.Shared.Models;
using InteresMe.API.Modules.Discovery.Models;
using InteresMe.API.Modules.Initiatives.Domain.Entities;
using InteresMe.API.Modules.Initiatives.Domain.Enums;
using InteresMe.API.Modules.History.Models;
using InteresMe.API.Modules.Interests.Models;
using InteresMe.API.Modules.Profile.Models;
using InteresMe.API.Modules.Verification.Models;
using Microsoft.EntityFrameworkCore;

namespace InteresMe.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    public DbSet<InterestCategory> InterestCategories => Set<InterestCategory>();

    public DbSet<Interest> Interests => Set<Interest>();

    public DbSet<Subinterest> Subinterests => Set<Subinterest>();

    public DbSet<UserInterest> UserInterests => Set<UserInterest>();

    public DbSet<UserDiscoveryPreference> UserDiscoveryPreferences => Set<UserDiscoveryPreference>();

    public DbSet<UserHistoryEvent> UserHistoryEvents => Set<UserHistoryEvent>();

    public DbSet<UserFollow> UserFollows => Set<UserFollow>();

    public DbSet<DirectConversation> DirectConversations => Set<DirectConversation>();

    public DbSet<DirectConversationParticipant> DirectConversationParticipants => Set<DirectConversationParticipant>();

    public DbSet<GroupChat> GroupChats => Set<GroupChat>();

    public DbSet<GroupChatParticipant> GroupChatParticipants => Set<GroupChatParticipant>();

    public DbSet<Channel> Channels => Set<Channel>();

    public DbSet<ChannelMember> ChannelMembers => Set<ChannelMember>();

    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    public DbSet<Initiative> Initiatives => Set<Initiative>();

    public DbSet<InitiativeInterest> InitiativeInterests => Set<InitiativeInterest>();

    public DbSet<InitiativeRole> InitiativeRoles => Set<InitiativeRole>();

    public DbSet<InitiativeJoinRequest> InitiativeJoinRequests => Set<InitiativeJoinRequest>();

    public DbSet<UserVerification> UserVerifications => Set<UserVerification>();

    public DbSet<VerificationToken> VerificationTokens => Set<VerificationToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users", "auth");

            entity.HasKey(user => user.Id);

            entity.Property(user => user.Email)
                .HasMaxLength(256)
                .IsRequired();

            entity.Property(user => user.GithubId)
                .HasMaxLength(64);

            entity.HasIndex(user => user.Email)
                .IsUnique();

            entity.HasIndex(user => user.GoogleId)
                .IsUnique();

            entity.HasIndex(user => user.GithubId)
                .IsUnique();

            entity.Property(user => user.DisplayName)
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(user => user.PasswordHash)
                .HasMaxLength(72)
                .IsRequired();

            entity.Property(user => user.CreatedAt)
                .IsRequired();

            entity.HasOne(user => user.Profile)
                .WithOne(profile => profile.User)
                .HasForeignKey<UserProfile>(profile => profile.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.ToTable("user_profiles", "profile");

            entity.HasKey(profile => profile.Id);

            entity.Property(profile => profile.DisplayName)
                .HasMaxLength(80)
                .IsRequired();

            entity.Property(profile => profile.Headline)
                .HasMaxLength(120);

            entity.Property(profile => profile.Bio)
                .HasMaxLength(500);

            entity.Property(profile => profile.City)
                .HasMaxLength(120);

            entity.Property(profile => profile.AvatarUrl)
                .HasMaxLength(2048);

            entity.Property(profile => profile.BirthDate);

            entity.Property(profile => profile.CreatedAt)
                .IsRequired();

            entity.Property(profile => profile.UpdatedAt)
                .IsRequired();

            entity.HasIndex(profile => profile.UserId)
                .IsUnique();
        });

        modelBuilder.Entity<UserInterest>(entity =>
        {
            entity.ToTable("user_interests", "interests");

            entity.HasKey(userInterest => new
            {
                userInterest.UserId,
                userInterest.InterestId
            });

            entity.Property(userInterest => userInterest.CreatedAt)
                .IsRequired();

            entity.HasOne(userInterest => userInterest.User)
                .WithMany(user => user.UserInterests)
                .HasForeignKey(userInterest => userInterest.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(userInterest => userInterest.Interest)
                .WithMany(interest => interest.UserInterests)
                .HasForeignKey(userInterest => userInterest.InterestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserDiscoveryPreference>(entity =>
        {
            entity.ToTable("user_discovery_preferences", "discovery");

            entity.HasKey(preference => preference.UserId);

            entity.Property(preference => preference.Goal)
                .IsRequired();

            entity.Property(preference => preference.CreatedAt)
                .IsRequired();

            entity.Property(preference => preference.UpdatedAt)
                .IsRequired();

            entity.HasIndex(preference => preference.Goal);

            entity.HasOne(preference => preference.User)
                .WithOne()
                .HasForeignKey<UserDiscoveryPreference>(preference => preference.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DirectConversation>(entity =>
        {
            entity.ToTable("direct_conversations", "chat");

            entity.HasKey(conversation => conversation.Id);

            entity.Property(conversation => conversation.CreatedAt)
                .IsRequired();

            entity.Property(conversation => conversation.UpdatedAt)
                .IsRequired();

            entity.HasIndex(conversation => new
                {
                    conversation.UserOneId,
                    conversation.UserTwoId
                })
                .IsUnique();

            entity.HasOne(conversation => conversation.UserOne)
                .WithMany()
                .HasForeignKey(conversation => conversation.UserOneId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(conversation => conversation.UserTwo)
                .WithMany()
                .HasForeignKey(conversation => conversation.UserTwoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DirectConversationParticipant>(entity =>
        {
            entity.ToTable("direct_conversation_participants", "chat");

            entity.HasKey(participant => participant.Id);

            entity.Property(participant => participant.JoinedAt)
                .IsRequired();

            entity.HasIndex(participant => new
                {
                    participant.DirectConversationId,
                    participant.UserId
                })
                .IsUnique();

            entity.HasIndex(participant => participant.UserId);

            entity.HasOne(participant => participant.DirectConversation)
                .WithMany(conversation => conversation.Participants)
                .HasForeignKey(participant => participant.DirectConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(participant => participant.User)
                .WithMany()
                .HasForeignKey(participant => participant.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GroupChat>(entity =>
        {
            entity.ToTable("group_chats", "chat");

            entity.HasKey(groupChat => groupChat.Id);

            entity.Property(groupChat => groupChat.Title)
                .HasMaxLength(160)
                .IsRequired();

            entity.Property(groupChat => groupChat.CreatedAt)
                .IsRequired();

            entity.Property(groupChat => groupChat.UpdatedAt)
                .IsRequired();

            entity.HasIndex(groupChat => groupChat.InitiativeId)
                .IsUnique();

            entity.HasOne(groupChat => groupChat.Initiative)
                .WithMany()
                .HasForeignKey(groupChat => groupChat.InitiativeId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<GroupChatParticipant>(entity =>
        {
            entity.ToTable("group_chat_participants", "chat");

            entity.HasKey(participant => participant.Id);

            entity.Property(participant => participant.JoinedAt)
                .IsRequired();

            entity.Property(participant => participant.Role)
                .IsRequired();

            entity.HasIndex(participant => new
                {
                    participant.GroupChatId,
                    participant.UserId
                })
                .IsUnique();

            entity.HasIndex(participant => participant.UserId);

            entity.HasOne(participant => participant.GroupChat)
                .WithMany(groupChat => groupChat.Participants)
                .HasForeignKey(participant => participant.GroupChatId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(participant => participant.User)
                .WithMany()
                .HasForeignKey(participant => participant.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Channel>(entity =>
        {
            entity.ToTable("channels", "chat");

            entity.HasKey(channel => channel.Id);

            entity.Property(channel => channel.Title)
                .HasMaxLength(160)
                .IsRequired();

            entity.Property(channel => channel.CreatedAt)
                .IsRequired();

            entity.Property(channel => channel.UpdatedAt)
                .IsRequired();

            entity.HasIndex(channel => channel.InitiativeId);

            entity.HasOne(channel => channel.Initiative)
                .WithMany()
                .HasForeignKey(channel => channel.InitiativeId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ChannelMember>(entity =>
        {
            entity.ToTable("channel_members", "chat");

            entity.HasKey(member => member.Id);

            entity.Property(member => member.JoinedAt)
                .IsRequired();

            entity.Property(member => member.Role)
                .IsRequired();

            entity.HasIndex(member => new
                {
                    member.ChannelId,
                    member.UserId
                })
                .IsUnique();

            entity.HasIndex(member => member.UserId);

            entity.HasOne(member => member.Channel)
                .WithMany(channel => channel.Members)
                .HasForeignKey(member => member.ChannelId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(member => member.User)
                .WithMany()
                .HasForeignKey(member => member.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.ToTable("chat_messages", "chat");

            entity.HasKey(message => message.Id);

            entity.Property(message => message.ConversationType)
                .IsRequired();

            entity.Property(message => message.Text)
                .HasMaxLength(2000)
                .IsRequired();

            entity.Property(message => message.CreatedAt)
                .IsRequired();

            entity.Property(message => message.IsDeleted)
                .IsRequired();

            entity.HasIndex(message => new
            {
                message.DirectConversationId,
                message.CreatedAt
            });

            entity.HasIndex(message => new
            {
                message.GroupChatId,
                message.CreatedAt
            });

            entity.HasIndex(message => new
            {
                message.ChannelId,
                message.CreatedAt
            });

            entity.HasIndex(message => message.SenderUserId);

            entity.ToTable(table => table.HasCheckConstraint(
                "CK_chat_messages_single_conversation",
                $"""
                ("ConversationType" = {(int)ChatConversationType.Direct} AND "DirectConversationId" IS NOT NULL AND "GroupChatId" IS NULL AND "ChannelId" IS NULL)
                OR ("ConversationType" = {(int)ChatConversationType.Group} AND "DirectConversationId" IS NULL AND "GroupChatId" IS NOT NULL AND "ChannelId" IS NULL)
                OR ("ConversationType" = {(int)ChatConversationType.Channel} AND "DirectConversationId" IS NULL AND "GroupChatId" IS NULL AND "ChannelId" IS NOT NULL)
                """));

            entity.HasOne(message => message.DirectConversation)
                .WithMany(conversation => conversation.Messages)
                .HasForeignKey(message => message.DirectConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(message => message.GroupChat)
                .WithMany(groupChat => groupChat.Messages)
                .HasForeignKey(message => message.GroupChatId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(message => message.Channel)
                .WithMany(channel => channel.Messages)
                .HasForeignKey(message => message.ChannelId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(message => message.SenderUser)
                .WithMany()
                .HasForeignKey(message => message.SenderUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Initiative>(entity =>
        {
            entity.ToTable("initiatives", "initiatives");

            entity.HasKey(initiative => initiative.Id);

            entity.Property(initiative => initiative.Title)
                .HasMaxLength(120)
                .IsRequired();

            entity.Property(initiative => initiative.Slug)
                .HasMaxLength(140)
                .IsRequired();

            entity.Property(initiative => initiative.ShortDescription)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(initiative => initiative.GoalType)
                .IsRequired();

            entity.Property(initiative => initiative.University)
                .HasMaxLength(160);

            entity.Property(initiative => initiative.Status)
                .IsRequired();

            entity.Property(initiative => initiative.Visibility)
                .HasDefaultValue(InitiativeVisibility.Public)
                .HasSentinel(InitiativeVisibility.Public)
                .IsRequired();

            entity.Property(initiative => initiative.CreatedAt)
                .IsRequired();

            entity.Property(initiative => initiative.UpdatedAt)
                .IsRequired();

            entity.HasIndex(initiative => initiative.OwnerUserId);

            entity.HasIndex(initiative => initiative.Slug)
                .IsUnique();

            entity.HasIndex(initiative => initiative.Status);

            entity.HasIndex(initiative => initiative.Visibility);

            entity.HasIndex(initiative => initiative.GoalType);

            entity.HasOne(initiative => initiative.OwnerUser)
                .WithMany()
                .HasForeignKey(initiative => initiative.OwnerUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InitiativeInterest>(entity =>
        {
            entity.ToTable("initiative_interests", "initiatives");

            entity.HasKey(initiativeInterest => new
            {
                initiativeInterest.InitiativeId,
                initiativeInterest.InterestId
            });

            entity.HasIndex(initiativeInterest => initiativeInterest.InterestId);

            entity.HasOne(initiativeInterest => initiativeInterest.Initiative)
                .WithMany(initiative => initiative.InitiativeInterests)
                .HasForeignKey(initiativeInterest => initiativeInterest.InitiativeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(initiativeInterest => initiativeInterest.Interest)
                .WithMany()
                .HasForeignKey(initiativeInterest => initiativeInterest.InterestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InitiativeRole>(entity =>
        {
            entity.ToTable("initiative_roles", "initiatives");

            entity.HasKey(role => role.Id);

            entity.Property(role => role.Name)
                .HasMaxLength(80)
                .IsRequired();

            entity.HasIndex(role => role.InitiativeId);

            entity.HasOne(role => role.Initiative)
                .WithMany(initiative => initiative.Roles)
                .HasForeignKey(role => role.InitiativeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InitiativeJoinRequest>(entity =>
        {
            entity.ToTable("initiative_join_requests", "initiatives");

            entity.HasKey(joinRequest => joinRequest.Id);

            entity.Property(joinRequest => joinRequest.Message)
                .HasMaxLength(500);

            entity.Property(joinRequest => joinRequest.Motivation);

            entity.Property(joinRequest => joinRequest.Experience);

            entity.Property(joinRequest => joinRequest.Contribution);

            entity.Property(joinRequest => joinRequest.Availability);

            entity.Property(joinRequest => joinRequest.Status)
                .IsRequired();

            entity.Property(joinRequest => joinRequest.CreatedAt)
                .IsRequired();

            entity.Property(joinRequest => joinRequest.UpdatedAt)
                .IsRequired();

            entity.HasIndex(joinRequest => joinRequest.InitiativeId);

            entity.HasIndex(joinRequest => joinRequest.UserId);

            entity.HasIndex(joinRequest => joinRequest.RoleId);

            entity.HasOne(joinRequest => joinRequest.Initiative)
                .WithMany(initiative => initiative.JoinRequests)
                .HasForeignKey(joinRequest => joinRequest.InitiativeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(joinRequest => joinRequest.User)
                .WithMany()
                .HasForeignKey(joinRequest => joinRequest.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(joinRequest => joinRequest.Role)
                .WithMany()
                .HasForeignKey(joinRequest => joinRequest.RoleId)
                .OnDelete(DeleteBehavior.SetNull);
        });


        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens", "auth");

            entity.HasKey(token => token.Id);

            entity.Property(token => token.TokenHash)
                .HasMaxLength(512)
                .IsRequired();

            entity.Property(token => token.TokenHash)
                .IsRequired();

            entity.Property(token => token.CreatedAt)
                .IsRequired();

            entity.Property(token => token.ExpiresAt)
                .IsRequired();

            entity.HasOne(token => token.user)
                .WithMany(user => user.RefreshTokens)
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserVerification>(entity =>
        {
            entity.ToTable("user_verifications", "verification");

            entity.HasKey(verification => verification.UserId);

            entity.Property(verification => verification.TrustScore)
                .IsRequired();

            entity.Property(verification => verification.PhoneNumber)
                .HasMaxLength(32);

            entity.Property(verification => verification.CreatedAt)
                .IsRequired();

            entity.Property(verification => verification.UpdatedAt)
                .IsRequired();

            entity.HasOne(verification => verification.User)
                .WithOne()
                .HasForeignKey<UserVerification>(verification => verification.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VerificationToken>(entity =>
        {
            entity.ToTable("verification_tokens", "verification");

            entity.HasKey(token => token.Id);

            entity.Property(token => token.Type)
                .IsRequired();

            entity.Property(token => token.Target)
                .HasMaxLength(256);

            entity.Property(token => token.TokenHash)
                .HasMaxLength(512)
                .IsRequired();

            entity.Property(token => token.ExpiresAt)
                .IsRequired();

            entity.Property(token => token.CreatedAt)
                .IsRequired();

            entity.HasIndex(token => token.TokenHash);

            entity.HasIndex(token => token.UserId);

            entity.HasOne(token => token.User)
                .WithMany()
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
