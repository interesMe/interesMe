using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Modules.Interests.DTOs;
using InteresMe.API.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InteresMe.API.Tests.Profile;

/// <summary>IME-10: managing personal interests acceptance criteria.</summary>
public sealed class UserInterestsTests(PostgresDatabase database) : IClassFixture<PostgresDatabase>
{
    [Fact]
    public async Task ReplaceInterests_SavesSelectionFromCatalog_AndSupportsAddAndRemove()
    {
        var interests = await database.SeedInterestsAsync("Hiking", "Robotics", "Chess");
        var userId = await TestUsers.CreateWithProfileAsync(database, "Explorer");

        await using (var context = database.CreateContext())
        {
            var first = await TestServices.CreateProfileService(context).ReplaceMyInterestsAsync(
                userId,
                new UpdateUserInterestsRequest { InterestIds = [interests[0].Id, interests[1].Id] });

            Assert.True(first.IsSuccess, first.ErrorMessage);
            Assert.Equal(2, first.Response!.Count);
        }

        await using (var context = database.CreateContext())
        {
            var second = await TestServices.CreateProfileService(context).ReplaceMyInterestsAsync(
                userId,
                new UpdateUserInterestsRequest { InterestIds = [interests[1].Id, interests[2].Id] });

            Assert.True(second.IsSuccess, second.ErrorMessage);
        }

        await using var verify = database.CreateContext();
        var stored = await verify.UserInterests
            .Where(userInterest => userInterest.UserId == userId)
            .Select(userInterest => userInterest.Interest.Name)
            .OrderBy(name => name)
            .ToListAsync();

        Assert.Equal(["Chess", "Robotics"], stored);
    }

    [Fact]
    public async Task ReplaceInterests_WithEmptyList_RemovesAll()
    {
        var interests = await database.SeedInterestsAsync("Music");
        var userId = await TestUsers.CreateWithProfileAsync(database, "Clearer");

        await using (var context = database.CreateContext())
        {
            await TestServices.CreateProfileService(context).ReplaceMyInterestsAsync(
                userId,
                new UpdateUserInterestsRequest { InterestIds = [interests[0].Id] });
        }

        await using (var context = database.CreateContext())
        {
            var cleared = await TestServices.CreateProfileService(context).ReplaceMyInterestsAsync(
                userId,
                new UpdateUserInterestsRequest { InterestIds = [] });
            Assert.True(cleared.IsSuccess);
            Assert.Empty(cleared.Response!);
        }

        await using var verify = database.CreateContext();
        Assert.False(await verify.UserInterests.AnyAsync(userInterest => userInterest.UserId == userId));
    }

    [Fact]
    public async Task ReplaceInterests_WithUnknownInterest_ReturnsValidationError()
    {
        var userId = await TestUsers.CreateWithProfileAsync(database, "Unknown");
        await using var context = database.CreateContext();

        var result = await TestServices.CreateProfileService(context).ReplaceMyInterestsAsync(
            userId,
            new UpdateUserInterestsRequest { InterestIds = [Guid.NewGuid()] });

        Assert.Equal(ApplicationErrorKind.Validation, result.ErrorKind);
    }
}
