using InteresMe.API.Data;
using InteresMe.API.Modules.Interests.DTOs;
using InteresMe.API.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace InteresMe.API.Tests.Profile;

/// <summary>Regression: the development demo seeder must not overwrite real users' choices (IME-9, IME-10).</summary>
public sealed class DemoSeederTests(PostgresDatabase database) : IClassFixture<PostgresDatabase>
{
    [Fact]
    public async Task Seeder_ByDefault_LeavesRealUsersInterestsAndBioUntouched()
    {
        Assert.False(DemoProfileDataSeeder.ShouldSeedLocalUsers());
        var interests = await database.SeedInterestsAsync("Knitting");
        var userId = await TestUsers.CreateWithProfileAsync(database, "Real Person");

        await using (var context = database.CreateContext())
        {
            await TestServices.CreateProfileService(context).ReplaceMyInterestsAsync(
                userId,
                new UpdateUserInterestsRequest { InterestIds = [interests[0].Id] });
        }

        await using (var context = database.CreateContext())
        {
            await DemoProfileDataSeeder.SeedAsync(context, NullLogger.Instance);
        }

        await using var verify = database.CreateContext();
        var stored = await verify.UserInterests
            .Where(userInterest => userInterest.UserId == userId)
            .Select(userInterest => userInterest.InterestId)
            .ToListAsync();
        var profile = await verify.UserProfiles.SingleAsync(userProfile => userProfile.UserId == userId);
        var posts = await verify.Posts.CountAsync(post => post.AuthorId == userId);

        Assert.Equal([interests[0].Id], stored);
        Assert.Null(profile.Bio);
        Assert.Equal(0, posts);
    }
}
