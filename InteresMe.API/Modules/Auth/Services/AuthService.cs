using InteresMe.API.Data;
using InteresMe.API.Modules.Auth.DTOs;
using InteresMe.API.Modules.Auth.Models;
using InteresMe.API.Security;
using Microsoft.EntityFrameworkCore;

namespace InteresMe.API.Modules.Auth.Services;

public class AuthService(
    AppDbContext dbContext,
    JwtTokenService jwtTokenService)
    : IAuthService
{
    private const int MinPasswordLength = 8;

    public async Task<AuthResult<AuthResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var displayName = request.DisplayName?.Trim() ?? string.Empty;
        var password = request.Password ?? string.Empty;

        if 
        (string.IsNullOrWhiteSpace(email) ||
         string.IsNullOrWhiteSpace(displayName) ||
         string.IsNullOrWhiteSpace(password))
        {
            return AuthResult<AuthResponse>.Failure(
                AuthErrorKind.Validation,
                "Email, display name, and password are required.");
        }

        if (password.Length < MinPasswordLength)
        {
            return AuthResult<AuthResponse>.Failure(
                AuthErrorKind.Validation,
                $"Password must be at least {MinPasswordLength} characters.");
        }

        var emailExists = await dbContext.Users
            .AnyAsync(user => user.Email == email, cancellationToken);

        if (emailExists)
        {
            return AuthResult<AuthResponse>.Failure(
                AuthErrorKind.EmailAlreadyExists,
                "A user with this email already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = displayName,
            PasswordHash = PasswordHasher.Hash(password),
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        var refreshToken =
    await CreateRefreshTokenAsync(
        user,
        cancellationToken);

return AuthResult<AuthResponse>.Success(
    BuildAuthResponse(
        user,
        refreshToken));
        }

    public async Task<AuthResult<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var password = request.Password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            return AuthResult<AuthResponse>.Failure(
                AuthErrorKind.Validation,
                "Email and password are required.");
        }

        var user = await dbContext.Users
            .FirstOrDefaultAsync(
                u => u.Email == email,
                cancellationToken);

        if (user is null ||
            !PasswordHasher.Verify(password, user.PasswordHash))
        {
            return AuthResult<AuthResponse>.Failure(
                AuthErrorKind.InvalidCredentials,
                "Invalid email or password.");
        }

        return AuthResult<AuthResponse>.Success(
            BuildAuthResponse(user));
    }
    public async Task<AuthResult<RefreshResponse>> RefreshTokenAsync(
        RefreshRequest request,
        CancellationToken cancellationToken = default)
    {

        var tokenHash = RefreshTokenHasher.HashToken(request.RefreshToken);
        
        var refreshToken = await dbContext.Set<RefreshToken>()
            .Include(rt => rt.user)
            .FirstOrDefaultAsync(
                rt => rt.TokenHash == tokenHash, 
                cancellationToken);

        if (refreshToken is null ||
            refreshToken.ExpiresAt < DateTime.UtcNow ||
            refreshToken.RevokedAt != null)
        {
            return AuthResult<RefreshResponse>.Failure(
                AuthErrorKind.InvalidCredentials,
                "Invalid or expired refresh token.");
        }

        var now = DateTime.UtcNow;

        refreshToken.RevokedAt = now;

        var ExpiresAt = DateTime.UtcNow.AddDays(7);

        var newRefreshToken = RefreshTokenGenerator.GenerateToken();

        var newRefreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = refreshToken.UserId,
            TokenHash = RefreshTokenHasher.HashToken(newRefreshToken),
            CreatedAt = now,
            ExpiresAt = ExpiresAt
        };

        dbContext.Set<RefreshToken>().Add(newRefreshTokenEntity);

        await dbContext.SaveChangesAsync(cancellationToken);

        return AuthResult<RefreshResponse>.Success(new RefreshResponse
        {
            AccessToken = jwtTokenService.CreateToken(refreshToken.user),
            RefreshToken = newRefreshToken
        });
    }
    private async Task<string> CreateRefreshTokenAsync(
    User user,
    CancellationToken cancellationToken)
{
    var refreshToken =
        RefreshTokenGenerator.GenerateToken();

    var refreshTokenEntity = new RefreshToken
    {
        Id = Guid.NewGuid(),
        UserId = user.Id,
        TokenHash = RefreshTokenHasher.HashToken(
            refreshToken),
        CreatedAt = DateTime.UtcNow,
        ExpiresAt = DateTime.UtcNow.AddDays(7)
    };

    dbContext.Set<RefreshToken>()
        .Add(refreshTokenEntity);

    await dbContext.SaveChangesAsync(
        cancellationToken);

    return refreshToken;
}

    

private AuthResponse BuildAuthResponse(
    User user,
    string refreshToken)
{
    return new AuthResponse
    {
        UserId = user.Id,
        Email = user.Email,
        DisplayName = user.DisplayName,
        Token = jwtTokenService.CreateToken(user),
        RefreshToken = refreshToken
    };
}
}