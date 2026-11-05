using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Modules.Initiatives.Contracts.Requests;
using InteresMe.API.Modules.Initiatives.Domain.Enums;
using InteresMe.API.Tests.Infrastructure;

namespace InteresMe.API.Tests.Initiatives;

/// <summary>IME-14: browsing initiatives (list, details, empty/no-result states, access rules).</summary>
public sealed class BrowseInitiativesTests(PostgresDatabase database) : IClassFixture<PostgresDatabase>
{
    [Fact]
    public async Task List_ShowsPublicAndOwn_HidesOthersPrivateAndClosed()
    {
        var interestId = await InitiativeTestData.InterestIdAsync(database);
        var viewer = await TestUsers.CreateWithProfileAsync(database, "Viewer");
        var other = await TestUsers.CreateWithProfileAsync(database, "Other");
        var tag = Guid.NewGuid().ToString("N")[..8];

        var publicOne = await InitiativeTestData.CreateAsync(database, other, InitiativeTestData.ValidRequest(interestId, $"Public {tag}"));
        var otherPrivate = await InitiativeTestData.CreateAsync(database, other, Private(InitiativeTestData.ValidRequest(interestId, $"Secret {tag}")));
        var myPrivate = await InitiativeTestData.CreateAsync(database, viewer, Private(InitiativeTestData.ValidRequest(interestId, $"Mine {tag}")));
        var archived = InitiativeTestData.ValidRequest(interestId, $"Archived {tag}");
        archived.Status = InitiativeStatus.Archived;
        await InitiativeTestData.CreateAsync(database, other, archived);

        await using var context = database.CreateContext();
        var result = await TestServices.CreateInitiativeQueries(context).GetAllAsync(viewer, search: tag);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        var ids = result.Response!.Select(initiative => initiative.Id).ToHashSet();
        Assert.Contains(publicOne.Id, ids);
        Assert.Contains(myPrivate.Id, ids);
        Assert.DoesNotContain(otherPrivate.Id, ids);
        Assert.Equal(2, ids.Count);
        Assert.All(result.Response!, initiative => Assert.False(string.IsNullOrEmpty(initiative.OwnerDisplayName)));
    }

    [Fact]
    public async Task List_SearchAndGoalFilter_AndNoResultsIsAnEmptyList()
    {
        var interestId = await InitiativeTestData.InterestIdAsync(database);
        var owner = await TestUsers.CreateWithProfileAsync(database, "Searcher");
        var tag = Guid.NewGuid().ToString("N")[..8];
        var learn = InitiativeTestData.ValidRequest(interestId, $"Chess club {tag}");
        learn.GoalType = InitiativeGoalType.Learn;
        await InitiativeTestData.CreateAsync(database, owner, learn);
        await InitiativeTestData.CreateAsync(database, owner, InitiativeTestData.ValidRequest(interestId, $"Robot build {tag}"));

        await using var context = database.CreateContext();
        var queries = TestServices.CreateInitiativeQueries(context);

        var byText = await queries.GetAllAsync(owner, search: $"CHESS CLUB {tag}");
        var byGoal = await queries.GetAllAsync(owner, search: tag, goalType: InitiativeGoalType.Learn);
        var none = await queries.GetAllAsync(owner, search: $"nothing-matches-{tag}");
        var wildcard = await queries.GetAllAsync(owner, search: "%_%");

        Assert.Equal([$"Chess club {tag}"], byText.Response!.Select(initiative => initiative.Title));
        Assert.Equal([$"Chess club {tag}"], byGoal.Response!.Select(initiative => initiative.Title));
        Assert.True(none.IsSuccess);
        Assert.Empty(none.Response!);
        Assert.Empty(wildcard.Response!);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task List_WithInvalidLimit_ReturnsValidationError(int limit)
    {
        await using var context = database.CreateContext();
        var result = await TestServices.CreateInitiativeQueries(context).GetAllAsync(Guid.NewGuid(), limit: limit);

        Assert.Equal(ApplicationErrorKind.Validation, result.ErrorKind);
    }

    [Fact]
    public async Task Details_ShowOwnerTeamAndViewerRequest()
    {
        var interestId = await InitiativeTestData.InterestIdAsync(database);
        var owner = await TestUsers.CreateWithProfileAsync(database, "Owner Details");
        var member = await TestUsers.CreateWithProfileAsync(database, "Member Details");
        var applicant = await TestUsers.CreateWithProfileAsync(database, "Applicant Details");
        var initiative = await InitiativeTestData.CreateAsync(database, owner, InitiativeTestData.ValidRequest(interestId));
        var roleId = initiative.Roles.First(role => role.Name == "Designer").Id;

        await using (var context = database.CreateContext())
        {
            var joinRequests = TestServices.CreateJoinRequests(context);
            var memberRequest = await joinRequests.CreateJoinRequestAsync(member, initiative.Id, new CreateInitiativeJoinRequestRequest { RoleId = roleId });
            await joinRequests.AcceptJoinRequestAsync(owner, initiative.Id, memberRequest.Response!.Id);
            await joinRequests.CreateJoinRequestAsync(applicant, initiative.Id, new CreateInitiativeJoinRequestRequest { Message = "Hi" });
        }

        await using var queries = database.CreateContext();
        var forApplicant = await TestServices.CreateInitiativeQueries(queries).GetByIdAsync(applicant, initiative.Id);

        Assert.True(forApplicant.IsSuccess, forApplicant.ErrorMessage);
        Assert.Equal("Owner Details", forApplicant.Response!.OwnerDisplayName);
        Assert.Equal(1, forApplicant.Response.MemberCount);
        var teamMember = Assert.Single(forApplicant.Response.Members);
        Assert.Equal("Member Details", teamMember.DisplayName);
        Assert.Equal("Designer", teamMember.RoleName);
        Assert.Equal(InitiativeJoinRequestStatus.Pending, forApplicant.Response.MyJoinRequest!.Status);

        var forOwner = await TestServices.CreateInitiativeQueries(queries).GetByIdAsync(owner, initiative.Id);
        Assert.Null(forOwner.Response!.MyJoinRequest);
    }

    [Fact]
    public async Task Details_OfPrivateInitiative_AreHiddenFromStrangers_ButVisibleToOwner()
    {
        var interestId = await InitiativeTestData.InterestIdAsync(database);
        var owner = await TestUsers.CreateWithProfileAsync(database, "Private Owner");
        var stranger = await TestUsers.CreateWithProfileAsync(database, "Stranger");
        var initiative = await InitiativeTestData.CreateAsync(database, owner, Private(InitiativeTestData.ValidRequest(interestId)));

        await using var context = database.CreateContext();
        var queries = TestServices.CreateInitiativeQueries(context);

        var strangerView = await queries.GetByIdAsync(stranger, initiative.Id);
        var ownerView = await queries.GetByIdAsync(owner, initiative.Id);
        var missing = await queries.GetByIdAsync(owner, Guid.NewGuid());

        Assert.Equal(ApplicationErrorKind.NotFound, strangerView.ErrorKind);
        Assert.True(ownerView.IsSuccess);
        Assert.Equal(ApplicationErrorKind.NotFound, missing.ErrorKind);
    }

    private static CreateInitiativeRequest Private(CreateInitiativeRequest request)
    {
        request.Visibility = InitiativeVisibility.Private;
        return request;
    }
}
