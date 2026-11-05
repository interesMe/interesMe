using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Modules.Profile.DTOs;
using InteresMe.API.Modules.Profile.Services;
using InteresMe.API.Tests.Infrastructure;

namespace InteresMe.API.Tests.Profile;

/// <summary>IME-9: profile editing (name, description, skills) acceptance criteria.</summary>
public sealed class ProfileEditTests(PostgresDatabase database) : IClassFixture<PostgresDatabase>
{
    [Fact]
    public async Task Update_NameDescriptionAndSkills_IsPersistedAndVisibleToOthers()
    {
        var userId = await TestUsers.CreateWithProfileAsync(database, "Before");
        var viewerId = await TestUsers.CreateWithProfileAsync(database, "Viewer");

        await using (var context = database.CreateContext())
        {
            var result = await TestServices.CreateProfileService(context).UpdateMyProfileAsync(userId, new UpdateProfileRequest
            {
                DisplayName = "  Maksym  ",
                Headline = "Building InteresMe",
                Bio = "I like Angular and teamwork.",
                Skills = [" Angular ", "C#", "angular", "PostgreSQL"]
            });

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.Equal(["Angular", "C#", "PostgreSQL"], result.Response!.Skills);
        }

        await using (var reopened = database.CreateContext())
        {
            var mine = await TestServices.CreateProfileService(reopened).GetMyProfileAsync(userId);
            Assert.Equal("Maksym", mine.Response!.DisplayName);
            Assert.Equal("Building InteresMe", mine.Response.Headline);
            Assert.Equal("I like Angular and teamwork.", mine.Response.Bio);
            Assert.Equal(["Angular", "C#", "PostgreSQL"], mine.Response.Skills);

            var publicView = await new ProfileViewService(reopened).GetAsync(viewerId, userId);
            Assert.True(publicView.IsSuccess, publicView.ErrorMessage);
            Assert.Equal(["Angular", "C#", "PostgreSQL"], publicView.Response!.BasicProfile.Skills);
            Assert.Equal("I like Angular and teamwork.", publicView.Response.BasicProfile.Bio);
        }
    }

    [Fact]
    public async Task Update_WithoutSkillsField_KeepsExistingSkills()
    {
        var userId = await TestUsers.CreateWithProfileAsync(database, "Keeper", ["Figma"]);
        await using var context = database.CreateContext();

        var result = await TestServices.CreateProfileService(context).UpdateMyProfileAsync(userId, new UpdateProfileRequest
        {
            DisplayName = "Keeper",
            Skills = null
        });

        Assert.Equal(["Figma"], result.Response!.Skills);
    }

    public static TheoryData<UpdateProfileRequest> InvalidRequests() => new()
    {
        new UpdateProfileRequest { DisplayName = "   " },
        new UpdateProfileRequest { DisplayName = new string('n', 81) },
        new UpdateProfileRequest { DisplayName = "Ok", Headline = new string('h', 121) },
        new UpdateProfileRequest { DisplayName = "Ok", Bio = new string('b', 501) },
        new UpdateProfileRequest { DisplayName = "Ok", Skills = ["  "] },
        new UpdateProfileRequest { DisplayName = "Ok", Skills = [new string('s', 41)] },
        new UpdateProfileRequest { DisplayName = "Ok", Skills = Enumerable.Range(1, 16).Select(i => $"skill-{i}").ToList() },
        new UpdateProfileRequest { DisplayName = "Ok", BirthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)) }
    };

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task Update_WithInvalidData_ReturnsValidationErrorAndDoesNotSave(UpdateProfileRequest request)
    {
        var userId = await TestUsers.CreateWithProfileAsync(database, "Stable", ["Kept"]);
        await using var context = database.CreateContext();
        var service = TestServices.CreateProfileService(context);

        var result = await service.UpdateMyProfileAsync(userId, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorKind.Validation, result.ErrorKind);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));

        await using var reopened = database.CreateContext();
        var unchanged = await TestServices.CreateProfileService(reopened).GetMyProfileAsync(userId);
        Assert.Equal("Stable", unchanged.Response!.DisplayName);
        Assert.Equal(["Kept"], unchanged.Response.Skills);
    }

    [Fact]
    public void NormalizeSkills_TrimsAndDeduplicatesCaseInsensitively()
    {
        var (skills, error) = ProfileService.NormalizeSkills([" UX ", "ux", "Data Analysis"]);

        Assert.Null(error);
        Assert.Equal(["UX", "Data Analysis"], skills);
    }
}
