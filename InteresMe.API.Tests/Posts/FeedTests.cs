using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Modules.Initiatives.Contracts.Requests;
using InteresMe.API.Modules.Initiatives.Domain.Enums;
using InteresMe.API.Modules.Interests.DTOs;
using InteresMe.API.Modules.Posts.Feed.Contracts.Requests;
using InteresMe.API.Modules.Profile.Models;
using InteresMe.API.Tests.Infrastructure;
using InteresMe.API.Tests.Initiatives;

namespace InteresMe.API.Tests.Posts;

/// <summary>IME-16: personalised feed.</summary>
public sealed class FeedTests(PostgresDatabase database) : IClassFixture<PostgresDatabase>
{
    private async Task<Guid> PostAsync(Guid authorId, string body, Guid? initiativeId = null)
    {
        await using var context = database.CreateContext();
        var result = await TestServices.CreatePostFeed(context).CreateAsync(
            authorId,
            new CreatePostRequest { Body = body, InitiativeId = initiativeId },
            []);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        return result.Response!.Id;
    }

    private async Task SetInterestsAsync(Guid userId, params Guid[] interestIds)
    {
        await using var context = database.CreateContext();
        var result = await TestServices.CreateProfileService(context).ReplaceMyInterestsAsync(
            userId,
            new UpdateUserInterestsRequest { InterestIds = interestIds.ToList() });
        Assert.True(result.IsSuccess, result.ErrorMessage);
    }

    [Fact]
    public async Task Feed_ContainsOwnFollowedAndSharedInterestPosts_NewestFirst_WithAuthorInfo()
    {
        var interests = await database.SeedInterestsAsync("Photography", "Rowing");
        var viewer = await TestUsers.CreateWithProfileAsync(database, "Feed Viewer");
        var followed = await TestUsers.CreateWithProfileAsync(database, "Followed Author");
        var likeMinded = await TestUsers.CreateWithProfileAsync(database, "Like Minded");
        var stranger = await TestUsers.CreateWithProfileAsync(database, "Stranger Author");

        await SetInterestsAsync(viewer, interests[0].Id);
        await SetInterestsAsync(likeMinded, interests[0].Id);
        await SetInterestsAsync(stranger, interests[1].Id);
        await using (var context = database.CreateContext())
        {
            context.UserFollows.Add(new UserFollow { FollowerId = viewer, FollowedId = followed, CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();
        }

        var own = await PostAsync(viewer, "My own post");
        var fromFollowed = await PostAsync(followed, "Followed post");
        var fromLikeMinded = await PostAsync(likeMinded, "Shared interest post");
        var fromStranger = await PostAsync(stranger, "Stranger post");

        await using var queries = database.CreateContext();
        var feed = await TestServices.CreatePostFeed(queries).GetFeedAsync(viewer, null);

        Assert.True(feed.IsSuccess, feed.ErrorMessage);
        Assert.Equal([fromLikeMinded, fromFollowed, own], feed.Response!.Items.Select(post => post.Id));
        Assert.DoesNotContain(fromStranger, feed.Response.Items.Select(post => post.Id));
        var first = feed.Response.Items[0];
        Assert.Equal("Like Minded", first.AuthorDisplayName);
        Assert.Equal("Shared interest post", first.Body);
        Assert.NotEqual(default, first.CreatedAt);
    }

    [Fact]
    public async Task Feed_IsPaginatedWithCursor()
    {
        var viewer = await TestUsers.CreateWithProfileAsync(database, "Pager");
        var ids = new List<Guid>();
        for (var i = 0; i < 5; i++)
        {
            ids.Add(await PostAsync(viewer, $"Post {i}"));
        }

        await using var context = database.CreateContext();
        var service = TestServices.CreatePostFeed(context);
        var page1 = await service.GetFeedAsync(viewer, null, pageSize: 2);
        var page2 = await service.GetFeedAsync(viewer, page1.Response!.NextCursor, pageSize: 2);
        var page3 = await service.GetFeedAsync(viewer, page2.Response!.NextCursor, pageSize: 2);
        var invalid = await service.GetFeedAsync(viewer, "not-a-cursor");

        var all = page1.Response.Items.Concat(page2.Response.Items).Concat(page3.Response!.Items).Select(post => post.Id).ToList();
        Assert.Equal(Enumerable.Reverse(ids).ToList(), all);
        Assert.Null(page3.Response.NextCursor);
        Assert.Equal(ApplicationErrorKind.Validation, invalid.ErrorKind);
    }

    [Fact]
    public async Task Feed_ForNewUserWithoutConnections_IsEmptyWithoutErrors()
    {
        var lonely = await TestUsers.CreateWithProfileAsync(database, "Lonely Newcomer");
        await using var context = database.CreateContext();

        var feed = await TestServices.CreatePostFeed(context).GetFeedAsync(lonely, null);

        Assert.True(feed.IsSuccess);
        Assert.Empty(feed.Response!.Items);
        Assert.Null(feed.Response.NextCursor);
    }

    [Fact]
    public async Task PostsOfPrivateInitiative_AreHiddenFromNonMembers_InFeedProfileAndDetails()
    {
        var interestId = await InitiativeTestData.InterestIdAsync(database);
        var owner = await TestUsers.CreateWithProfileAsync(database, "Private Poster");
        var follower = await TestUsers.CreateWithProfileAsync(database, "Curious Follower");
        await using (var context = database.CreateContext())
        {
            context.UserFollows.Add(new UserFollow { FollowerId = follower, FollowedId = owner, CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();
        }

        var request = InitiativeTestData.ValidRequest(interestId);
        request.Visibility = InitiativeVisibility.Private;
        var privateInitiative = await InitiativeTestData.CreateAsync(database, owner, request);
        var publicPost = await PostAsync(owner, "Public update");
        var privatePost = await PostAsync(owner, "Team-only update", privateInitiative.Id);

        await using var queries = database.CreateContext();
        var service = TestServices.CreatePostFeed(queries);

        var followerFeed = await service.GetFeedAsync(follower, null);
        var followerProfile = await service.GetUserPostsAsync(follower, owner, null);
        var followerDetails = await service.GetByIdAsync(follower, privatePost);
        var ownerFeed = await service.GetFeedAsync(owner, null);

        Assert.Equal([publicPost], followerFeed.Response!.Items.Select(post => post.Id));
        Assert.Equal([publicPost], followerProfile.Response!.Items.Select(post => post.Id));
        Assert.Equal(ApplicationErrorKind.NotFound, followerDetails.ErrorKind);
        Assert.Equal([privatePost, publicPost], ownerFeed.Response!.Items.Select(post => post.Id));
    }
}
