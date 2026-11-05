using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Modules.Discovery.Services;
using InteresMe.API.Modules.Interests.DTOs;
using InteresMe.API.Modules.Interests.Models;
using InteresMe.API.Tests.Infrastructure;

namespace InteresMe.API.Tests.Discovery;

/// <summary>IME-11: finding like-minded people by shared interests.</summary>
public sealed class PeopleSearchTests(PostgresDatabase database) : IClassFixture<PostgresDatabase>
{
    private async Task SetInterestsAsync(Guid userId, params Interest[] interests)
    {
        await using var context = database.CreateContext();
        var result = await TestServices.CreateProfileService(context).ReplaceMyInterestsAsync(
            userId,
            new UpdateUserInterestsRequest { InterestIds = interests.Select(interest => interest.Id).ToList() });
        Assert.True(result.IsSuccess, result.ErrorMessage);
    }

    [Fact]
    public async Task Search_ByMyInterests_RanksBySharedCount_ExcludesMe_AndReturnsProfileLinkData()
    {
        var interests = await database.SeedInterestsAsync("Football", "Startups", "Photography", "Gardening");
        var me = await TestUsers.CreateWithProfileAsync(database, "Me");
        var strong = await TestUsers.CreateWithProfileAsync(database, "Strong Match", ["Pitching"]);
        var weak = await TestUsers.CreateWithProfileAsync(database, "Weak Match");
        var none = await TestUsers.CreateWithProfileAsync(database, "No Match");

        await SetInterestsAsync(me, interests[0], interests[1], interests[2]);
        await SetInterestsAsync(strong, interests[0], interests[1]);
        await SetInterestsAsync(weak, interests[2]);
        await SetInterestsAsync(none, interests[3]);

        await using var context = database.CreateContext();
        var result = await new PeopleSearchService(context).SearchByInterestsAsync(me, interestIds: null, limit: null);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.True(result.Response!.UsedMyInterests);
        Assert.Equal([strong, weak], result.Response.Items.Select(item => item.UserId));
        Assert.DoesNotContain(result.Response.Items, item => item.UserId == me || item.UserId == none);

        var top = result.Response.Items[0];
        Assert.Equal("Strong Match", top.DisplayName);
        Assert.Equal(2, top.SharedInterestCount);
        Assert.Equal(["Football", "Startups"], top.SharedInterests.Select(interest => interest.Name));
        Assert.Equal(["Pitching"], top.Skills);
    }

    [Fact]
    public async Task Search_ByExplicitInterest_UsesOnlyThatInterest()
    {
        var interests = await database.SeedInterestsAsync("Painting", "Climbing");
        var me = await TestUsers.CreateWithProfileAsync(database, "Searcher");
        var painter = await TestUsers.CreateWithProfileAsync(database, "Painter");
        var climber = await TestUsers.CreateWithProfileAsync(database, "Climber");
        await SetInterestsAsync(painter, interests[0]);
        await SetInterestsAsync(climber, interests[1]);

        await using var context = database.CreateContext();
        var result = await new PeopleSearchService(context).SearchByInterestsAsync(me, [interests[1].Id], limit: 10);

        Assert.False(result.Response!.UsedMyInterests);
        Assert.Equal([climber], result.Response.Items.Select(item => item.UserId));
    }

    [Fact]
    public async Task Search_WithNoMatches_ReturnsEmptyList()
    {
        var interests = await database.SeedInterestsAsync("Origami");
        var me = await TestUsers.CreateWithProfileAsync(database, "Lonely");
        await SetInterestsAsync(me, interests[0]);

        await using var context = database.CreateContext();
        var result = await new PeopleSearchService(context).SearchByInterestsAsync(me, null, null);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Response!.Items);
    }

    [Fact]
    public async Task Search_WhenViewerHasNoInterests_ReturnsEmptyListWithoutError()
    {
        var me = await TestUsers.CreateWithProfileAsync(database, "Newcomer");
        await using var context = database.CreateContext();

        var result = await new PeopleSearchService(context).SearchByInterestsAsync(me, null, null);

        Assert.True(result.IsSuccess);
        Assert.True(result.Response!.UsedMyInterests);
        Assert.Empty(result.Response.InterestIds);
        Assert.Empty(result.Response.Items);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public async Task Search_WithInvalidLimit_ReturnsValidationError(int limit)
    {
        await using var context = database.CreateContext();
        var result = await new PeopleSearchService(context).SearchByInterestsAsync(Guid.NewGuid(), null, limit);

        Assert.Equal(ApplicationErrorKind.Validation, result.ErrorKind);
    }

    [Fact]
    public async Task Search_WithUnknownInterest_ReturnsValidationError()
    {
        await using var context = database.CreateContext();
        var result = await new PeopleSearchService(context).SearchByInterestsAsync(Guid.NewGuid(), [Guid.NewGuid()], null);

        Assert.Equal(ApplicationErrorKind.Validation, result.ErrorKind);
    }

    [Fact]
    public async Task Search_WithTooManyInterests_ReturnsValidationError()
    {
        await using var context = database.CreateContext();
        var ids = Enumerable.Range(0, 21).Select(_ => Guid.NewGuid()).ToList();

        var result = await new PeopleSearchService(context).SearchByInterestsAsync(Guid.NewGuid(), ids, null);

        Assert.Equal(ApplicationErrorKind.Validation, result.ErrorKind);
    }
}
