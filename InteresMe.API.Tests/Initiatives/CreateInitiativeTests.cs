using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Modules.Initiatives.Contracts.Requests;
using InteresMe.API.Modules.Initiatives.Domain.Enums;
using InteresMe.API.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InteresMe.API.Tests.Initiatives;

/// <summary>IME-13: creating an initiative.</summary>
public sealed class CreateInitiativeTests(PostgresDatabase database) : IClassFixture<PostgresDatabase>
{
    [Fact]
    public async Task Create_WithValidData_PersistsAndLinksToAuthor()
    {
        var interestId = await InitiativeTestData.InterestIdAsync(database);
        var owner = await TestUsers.CreateWithProfileAsync(database, "Olena Founder");

        var created = await InitiativeTestData.CreateAsync(
            database,
            owner,
            InitiativeTestData.ValidRequest(interestId, "  Study planner  "));

        Assert.Equal("Study planner", created.Title);
        Assert.Equal(owner, created.OwnerUserId);
        Assert.Equal("Olena Founder", created.OwnerDisplayName);
        Assert.Equal(InitiativeStatus.Idea, created.Status);
        Assert.Equal(InitiativeVisibility.Public, created.Visibility);
        Assert.Equal(["Designer", "Frontend developer"], created.Roles.Select(role => role.Name));
        Assert.Single(created.Interests);
        Assert.False(string.IsNullOrWhiteSpace(created.Slug));

        await using var verify = database.CreateContext();
        var stored = await verify.Initiatives.SingleAsync(initiative => initiative.Id == created.Id);
        Assert.Equal(owner, stored.OwnerUserId);

        await using var queries = database.CreateContext();
        var mine = await TestServices.CreateInitiativeQueries(queries).GetMineAsync(owner);
        Assert.Contains(mine, initiative => initiative.Id == created.Id);
    }

    public static TheoryData<string, Action<CreateInitiativeRequest>> InvalidCases() => new()
    {
        { "empty title", request => request.Title = "   " },
        { "long title", request => request.Title = new string('t', 121) },
        { "empty description", request => request.ShortDescription = "" },
        { "long description", request => request.ShortDescription = new string('d', 501) },
        { "no interests", request => request.InterestIds = [] },
        { "unknown interest", request => request.InterestIds = [Guid.NewGuid()] },
        { "no roles", request => request.Roles = ["  "] },
        { "too many roles", request => request.Roles = Enumerable.Range(1, 11).Select(i => $"Role {i}").ToList() },
        { "long role", request => request.Roles = [new string('r', 81)] },
        { "zero team size", request => request.TeamSize = 0 },
        { "huge team size", request => request.TeamSize = 101 },
        { "invalid goal", request => request.GoalType = (InitiativeGoalType)99 }
    };

    [Theory]
    [MemberData(nameof(InvalidCases))]
    public async Task Create_WithInvalidData_IsRejectedAndNothingIsSaved(string caseName, Action<CreateInitiativeRequest> mutate)
    {
        var interestId = await InitiativeTestData.InterestIdAsync(database);
        var owner = await TestUsers.CreateWithProfileAsync(database, $"Owner {caseName}");
        var request = InitiativeTestData.ValidRequest(interestId);
        mutate(request);

        await using var context = database.CreateContext();
        var result = await TestServices.CreateInitiativeManagement(context).CreateAsync(owner, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorKind.Validation, result.ErrorKind);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));

        await using var verify = database.CreateContext();
        Assert.False(await verify.Initiatives.AnyAsync(initiative => initiative.OwnerUserId == owner));
    }
}
