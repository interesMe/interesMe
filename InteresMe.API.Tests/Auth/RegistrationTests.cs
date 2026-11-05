using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Modules.Auth.DTOs;
using InteresMe.API.Modules.Auth.Services;
using InteresMe.API.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InteresMe.API.Tests.Auth;

/// <summary>IME-7: registration acceptance criteria.</summary>
public sealed class RegistrationTests(PostgresDatabase database) : IClassFixture<PostgresDatabase>
{
    [Fact]
    public async Task Register_WithValidData_CreatesAccountAndReturnsTokens()
    {
        await using var context = database.CreateContext();
        var service = TestServices.CreateAuthService(context);

        var result = await service.RegisterAsync(new RegisterRequest
        {
            Email = "  New.User@Example.com ",
            DisplayName = "New User",
            Password = "correct-horse-1"
        });

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal("new.user@example.com", result.Response!.Email);
        Assert.False(string.IsNullOrWhiteSpace(result.Response.Token));
        Assert.False(string.IsNullOrWhiteSpace(result.Response.RefreshToken));

        await using var verify = database.CreateContext();
        var stored = await verify.Users.SingleAsync(user => user.Id == result.Response.UserId);
        Assert.Equal("new user", stored.DisplayName.ToLowerInvariant());
        Assert.NotEqual("correct-horse-1", stored.PasswordHash);
    }

    [Fact]
    public async Task Register_WithExistingEmailInDifferentCase_ReturnsConflict()
    {
        await using var context = database.CreateContext();
        var service = TestServices.CreateAuthService(context);
        var request = new RegisterRequest { Email = "dup@example.com", DisplayName = "Dup", Password = "password-123" };

        Assert.True((await service.RegisterAsync(request)).IsSuccess);

        var second = await service.RegisterAsync(new RegisterRequest
        {
            Email = "DUP@example.com",
            DisplayName = "Dup 2",
            Password = "password-456"
        });

        Assert.False(second.IsSuccess);
        Assert.Equal(ApplicationErrorKind.Conflict, second.ErrorKind);
    }

    [Theory]
    [InlineData("", "Name", "password-123")]
    [InlineData("a@example.com", "   ", "password-123")]
    [InlineData("a@example.com", "Name", "")]
    [InlineData("not-an-email", "Name", "password-123")]
    [InlineData("a@b", "Name", "password-123")]
    [InlineData("a b@example.com", "Name", "password-123")]
    [InlineData("valid@example.com", "Name", "short")]
    public async Task Register_WithInvalidFields_ReturnsValidationError(string email, string displayName, string password)
    {
        await using var context = database.CreateContext();
        var service = TestServices.CreateAuthService(context);

        var result = await service.RegisterAsync(new RegisterRequest
        {
            Email = email,
            DisplayName = displayName,
            Password = password
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorKind.Validation, result.ErrorKind);
    }

    [Fact]
    public async Task Register_WithTooLongDisplayName_ReturnsValidationError()
    {
        await using var context = database.CreateContext();
        var service = TestServices.CreateAuthService(context);

        var result = await service.RegisterAsync(new RegisterRequest
        {
            Email = "long-name@example.com",
            DisplayName = new string('x', 81),
            Password = "password-123"
        });

        Assert.Equal(ApplicationErrorKind.Validation, result.ErrorKind);
    }

    [Theory]
    [InlineData("user@example.com", true)]
    [InlineData("first.last+tag@sub.example.org", true)]
    [InlineData("user@localhost", false)]
    [InlineData("user@.example.com", false)]
    [InlineData("@example.com", false)]
    [InlineData("user@example.", false)]
    public void IsValidEmail_ClassifiesAddresses(string email, bool expected) =>
        Assert.Equal(expected, AuthService.IsValidEmail(email));
}
