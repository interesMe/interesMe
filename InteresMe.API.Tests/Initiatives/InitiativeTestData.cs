using InteresMe.API.Modules.Initiatives.Contracts.Requests;
using InteresMe.API.Modules.Initiatives.Contracts.Responses;
using InteresMe.API.Modules.Initiatives.Domain.Enums;
using InteresMe.API.Tests.Infrastructure;

namespace InteresMe.API.Tests.Initiatives;

internal static class InitiativeTestData
{
    public static async Task<Guid> InterestIdAsync(PostgresDatabase database) =>
        (await database.SeedInterestsAsync($"Topic {Guid.NewGuid():N}"[..14]))[0].Id;

    public static CreateInitiativeRequest ValidRequest(Guid interestId, string title = "Student startup") => new()
    {
        Title = title,
        ShortDescription = "We build a study planner for first-year students.",
        GoalType = InitiativeGoalType.Build,
        InterestIds = [interestId],
        Roles = ["Frontend developer", "Designer"],
        University = "NUBiP",
        TeamSize = 4
    };

    public static async Task<InitiativeResponse> CreateAsync(
        PostgresDatabase database,
        Guid ownerId,
        CreateInitiativeRequest request)
    {
        await using var context = database.CreateContext();
        var result = await TestServices.CreateInitiativeManagement(context).CreateAsync(ownerId, request);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        return result.Response!;
    }
}
