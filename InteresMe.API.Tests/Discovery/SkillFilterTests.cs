using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Modules.Discovery.Services;
using InteresMe.API.Modules.Interests.DTOs;
using InteresMe.API.Tests.Infrastructure;

namespace InteresMe.API.Tests.Discovery;

/// <summary>IME-12: filtering people by skills.</summary>
public sealed class SkillFilterTests(PostgresDatabase database) : IClassFixture<PostgresDatabase>
{
    private async Task<(Guid Me, Guid Designer, Guid Developer, Guid FullStack)> SeedAsync()
    {
        var interests = await database.SeedInterestsAsync("Startups");
        var me = await TestUsers.CreateWithProfileAsync(database, "Searcher");
        var designer = await TestUsers.CreateWithProfileAsync(database, "Designer", ["Figma", "UX Research"]);
        var developer = await TestUsers.CreateWithProfileAsync(database, "Developer", ["Angular", "C#"]);
        var fullStack = await TestUsers.CreateWithProfileAsync(database, "Full Stack", ["angular", "Figma"]);

        foreach (var userId in new[] { me, designer, developer, fullStack })
        {
            await using var context = database.CreateContext();
            var result = await TestServices.CreateProfileService(context).ReplaceMyInterestsAsync(
                userId,
                new UpdateUserInterestsRequest { InterestIds = [interests[0].Id] });
            Assert.True(result.IsSuccess, result.ErrorMessage);
        }

        return (me, designer, developer, fullStack);
    }

    [Fact]
    public async Task Filter_BySkill_ReturnsOnlyPeopleWithThatSkill_CaseInsensitive()
    {
        var (me, designer, _, fullStack) = await SeedAsync();
        await using var context = database.CreateContext();

        var result = await new PeopleSearchService(context).SearchAsync(me, null, ["FIGMA"], null);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(["figma"], result.Response!.SkillFilters);
        Assert.Equal(
            new[] { designer, fullStack }.OrderBy(id => id),
            result.Response.Items.Select(item => item.UserId).OrderBy(id => id));
    }

    [Fact]
    public async Task Filter_ByMultipleSkills_RequiresAllOfThem()
    {
        var (me, _, _, fullStack) = await SeedAsync();
        await using var context = database.CreateContext();

        var result = await new PeopleSearchService(context).SearchAsync(me, null, ["Angular", "figma"], null);

        Assert.Equal([fullStack], result.Response!.Items.Select(item => item.UserId));
    }

    [Fact]
    public async Task ResetFilters_RestoresInitialResults_AndAvailableSkillsComeFromMatches()
    {
        var (me, designer, developer, fullStack) = await SeedAsync();
        await using var context = database.CreateContext();
        var service = new PeopleSearchService(context);

        var initial = await service.SearchAsync(me, null, null, null);
        var filtered = await service.SearchAsync(me, null, ["C#"], null);
        var reset = await service.SearchAsync(me, null, [], null);

        Assert.Equal([developer], filtered.Response!.Items.Select(item => item.UserId));
        Assert.Equal(3, initial.Response!.Items.Count);
        Assert.Equal(
            initial.Response.Items.Select(item => item.UserId),
            reset.Response!.Items.Select(item => item.UserId));
        Assert.Contains(designer, reset.Response.Items.Select(item => item.UserId));
        Assert.Contains(fullStack, reset.Response.Items.Select(item => item.UserId));

        // "Figma" and "angular"/"Angular" are grouped case-insensitively; the most frequent first.
        Assert.Equal(4, initial.Response.AvailableSkills.Count);
        Assert.Contains("Figma", initial.Response.AvailableSkills);
        Assert.Equal(filtered.Response.AvailableSkills, initial.Response.AvailableSkills);
    }

    [Fact]
    public async Task Filter_WithSkillNobodyHas_ReturnsEmptyList()
    {
        var (me, _, _, _) = await SeedAsync();
        await using var context = database.CreateContext();

        var result = await new PeopleSearchService(context).SearchAsync(me, null, ["Cobol"], null);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Response!.Items);
    }

    [Theory]
    [InlineData("  ")]
    [InlineData("this-skill-name-is-definitely-longer-than-forty-characters")]
    public async Task Filter_WithInvalidSkill_ReturnsValidationError(string skill)
    {
        await using var context = database.CreateContext();

        var result = await new PeopleSearchService(context).SearchAsync(Guid.NewGuid(), null, [skill], null);

        Assert.Equal(ApplicationErrorKind.Validation, result.ErrorKind);
    }

    [Fact]
    public async Task Filter_WithTooManySkills_ReturnsValidationError()
    {
        await using var context = database.CreateContext();
        var skills = Enumerable.Range(1, 11).Select(i => $"skill-{i}").ToList();

        var result = await new PeopleSearchService(context).SearchAsync(Guid.NewGuid(), null, skills, null);

        Assert.Equal(ApplicationErrorKind.Validation, result.ErrorKind);
    }
}
