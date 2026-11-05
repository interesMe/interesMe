using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Modules.Initiatives;
using InteresMe.API.Modules.Initiatives.Contracts.Requests;
using InteresMe.API.Modules.Initiatives.Domain.Enums;
using InteresMe.API.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InteresMe.API.Tests.Initiatives;

/// <summary>IME-15: applying to an initiative.</summary>
public sealed class JoinRequestTests(PostgresDatabase database) : IClassFixture<PostgresDatabase>
{
    private async Task<(Guid Owner, Guid Applicant, Guid InitiativeId)> SeedAsync(Action<CreateInitiativeRequest>? configure = null)
    {
        var interestId = await InitiativeTestData.InterestIdAsync(database);
        var owner = await TestUsers.CreateWithProfileAsync(database, "Join Owner");
        var applicant = await TestUsers.CreateWithProfileAsync(database, "Join Applicant");
        var request = InitiativeTestData.ValidRequest(interestId);
        configure?.Invoke(request);
        var initiative = await InitiativeTestData.CreateAsync(database, owner, request);
        return (owner, applicant, initiative.Id);
    }

    [Fact]
    public async Task Apply_SavesPendingRequest_AndOwnerSeesItWithApplicantName()
    {
        var (owner, applicant, initiativeId) = await SeedAsync();
        await using var context = database.CreateContext();
        var service = TestServices.CreateJoinRequests(context);

        var created = await service.CreateJoinRequestAsync(applicant, initiativeId, new CreateInitiativeJoinRequestRequest
        {
            Message = "  I would like to help with design. ",
            Motivation = "Learn teamwork"
        });

        Assert.True(created.IsSuccess, created.ErrorMessage);
        Assert.Equal(InitiativeJoinRequestStatus.Pending, created.Response!.Status);
        Assert.Equal("I would like to help with design.", created.Response.Message);

        var ownerList = await service.GetJoinRequestsAsync(owner, initiativeId);
        var listed = Assert.Single(ownerList.Response!);
        Assert.Equal("Join Applicant", listed.UserDisplayName);

        var strangerList = await service.GetJoinRequestsAsync(applicant, initiativeId);
        Assert.Equal(ApplicationErrorKind.Forbidden, strangerList.ErrorKind);
    }

    [Fact]
    public async Task Apply_Twice_WhilePendingOrAccepted_IsBlocked_ButAllowedAfterRejection()
    {
        var (owner, applicant, initiativeId) = await SeedAsync();
        await using var context = database.CreateContext();
        var service = TestServices.CreateJoinRequests(context);

        var first = await service.CreateJoinRequestAsync(applicant, initiativeId, new CreateInitiativeJoinRequestRequest());
        var duplicate = await service.CreateJoinRequestAsync(applicant, initiativeId, new CreateInitiativeJoinRequestRequest());
        Assert.Equal(ApplicationErrorKind.Conflict, duplicate.ErrorKind);
        Assert.Equal(InitiativeErrorCodes.ApplyAlreadyExists, duplicate.ErrorCode);

        await service.RejectJoinRequestAsync(owner, initiativeId, first.Response!.Id);
        var afterRejection = await service.CreateJoinRequestAsync(applicant, initiativeId, new CreateInitiativeJoinRequestRequest());
        Assert.True(afterRejection.IsSuccess, afterRejection.ErrorMessage);

        var accepted = await service.AcceptJoinRequestAsync(owner, initiativeId, afterRejection.Response!.Id);
        Assert.Equal(InitiativeJoinRequestStatus.Accepted, accepted.Response!.Status);
        Assert.Equal("Join Applicant", accepted.Response.UserDisplayName);

        var asMember = await service.CreateJoinRequestAsync(applicant, initiativeId, new CreateInitiativeJoinRequestRequest());
        Assert.Equal(InitiativeErrorCodes.ApplyAlreadyMember, asMember.ErrorCode);

        await using var queries = database.CreateContext();
        var details = await TestServices.CreateInitiativeQueries(queries).GetByIdAsync(applicant, initiativeId);
        Assert.Equal(InitiativeJoinRequestStatus.Accepted, details.Response!.MyJoinRequest!.Status);
    }

    [Fact]
    public async Task Database_RejectsConcurrentDuplicateActiveRequest()
    {
        var (_, applicant, initiativeId) = await SeedAsync();

        await using (var context = database.CreateContext())
        {
            var first = await TestServices.CreateJoinRequests(context).CreateJoinRequestAsync(applicant, initiativeId, new CreateInitiativeJoinRequestRequest());
            Assert.True(first.IsSuccess);
        }

        // Bypass the service check to simulate a race: the partial unique index must stop the duplicate.
        await using var raw = database.CreateContext();
        raw.InitiativeJoinRequests.Add(new InteresMe.API.Modules.Initiatives.Domain.Entities.InitiativeJoinRequest
        {
            Id = Guid.NewGuid(),
            InitiativeId = initiativeId,
            UserId = applicant,
            Status = InitiativeJoinRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => raw.SaveChangesAsync());
    }

    [Fact]
    public async Task Apply_ToOwnPrivateClosedOrMissingInitiative_IsRejected()
    {
        var (owner, applicant, initiativeId) = await SeedAsync();
        var (_, _, privateId) = await SeedAsync(request => request.Visibility = InitiativeVisibility.Private);
        var (_, _, archivedId) = await SeedAsync(request => request.Status = InitiativeStatus.Archived);

        await using var context = database.CreateContext();
        var service = TestServices.CreateJoinRequests(context);

        Assert.Equal(InitiativeErrorCodes.ApplyOwnerForbidden, (await service.CreateJoinRequestAsync(owner, initiativeId, new())).ErrorCode);
        Assert.Equal(ApplicationErrorKind.NotFound, (await service.CreateJoinRequestAsync(applicant, privateId, new())).ErrorKind);
        Assert.Equal(InitiativeErrorCodes.ApplyClosed, (await service.CreateJoinRequestAsync(applicant, archivedId, new())).ErrorCode);
        Assert.Equal(ApplicationErrorKind.NotFound, (await service.CreateJoinRequestAsync(applicant, Guid.NewGuid(), new())).ErrorKind);
    }

    [Fact]
    public async Task Apply_WithInvalidFields_IsRejected()
    {
        var (_, applicant, initiativeId) = await SeedAsync();
        await using var context = database.CreateContext();
        var service = TestServices.CreateJoinRequests(context);

        var longMessage = await service.CreateJoinRequestAsync(applicant, initiativeId, new() { Message = new string('m', 501) });
        var longExperience = await service.CreateJoinRequestAsync(applicant, initiativeId, new() { Experience = new string('e', 1001) });
        var foreignRole = await service.CreateJoinRequestAsync(applicant, initiativeId, new() { RoleId = Guid.NewGuid() });

        Assert.Equal(ApplicationErrorKind.Validation, longMessage.ErrorKind);
        Assert.Equal(InitiativeErrorCodes.ApplyFieldTooLong, longExperience.ErrorCode);
        Assert.Equal(InitiativeErrorCodes.ApplyRoleInvalid, foreignRole.ErrorCode);
    }

    [Fact]
    public async Task Accept_WhenTeamIsFull_IsRejected()
    {
        var (owner, applicant, initiativeId) = await SeedAsync(request => request.TeamSize = 2);
        var second = await TestUsers.CreateWithProfileAsync(database, "Second Applicant");
        await using var context = database.CreateContext();
        var service = TestServices.CreateJoinRequests(context);

        var first = await service.CreateJoinRequestAsync(applicant, initiativeId, new());
        var other = await service.CreateJoinRequestAsync(second, initiativeId, new());

        Assert.True((await service.AcceptJoinRequestAsync(owner, initiativeId, first.Response!.Id)).IsSuccess);
        var full = await service.AcceptJoinRequestAsync(owner, initiativeId, other.Response!.Id);

        Assert.Equal(InitiativeErrorCodes.TeamFull, full.ErrorCode);
    }
}
