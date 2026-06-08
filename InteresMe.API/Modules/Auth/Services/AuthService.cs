using System.Data;
using System.Security.Cryptography;
using InteresMe.API.BuildingBlocks.Email;
using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Data;
using InteresMe.API.Modules.Auth.DTOs;
using InteresMe.API.Modules.Auth.Models;
using InteresMe.API.Modules.Auth.Services.Contracts;
using InteresMe.API.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Npgsql;

namespace InteresMe.API.Modules.Auth.Services;

public class AuthService(
    AppDbContext dbContext,
    JwtTokenService jwtTokenService,
    IOAuthProviderService oAuthProviderService,
    IMemoryCache memoryCache,
    IEmailService emailService,
    ILogger<AuthService> logger)
    : IAuthService
{
    private const int MinPasswordLength = 8;
    private const int RefreshTokenLifetimeDays = 7;
    private const int GithubSessionLifetimeMinutes = 5;
    private const string GithubSessionCachePrefix = "github-session:";
    private const string PostgresUniqueViolation = "23505";

    public async Task<ApplicationResult<AuthResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var displayName = request.DisplayName?.Trim() ?? string.Empty;
        var password = request.Password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(displayName) ||
            string.IsNullOrWhiteSpace(password))
        {
            return ApplicationResult<AuthResponse>.Failure(
                ApplicationErrorKind.Validation,
                "Email, display name, and password are required.");
        }

        if (password.Length < MinPasswordLength)
        {
            return ApplicationResult<AuthResponse>.Failure(
                ApplicationErrorKind.Validation,
                $"Password must be at least {MinPasswordLength} characters.");
        }

        var emailExists = await dbContext.Users
            .AnyAsync(user => user.Email == email, cancellationToken);

        if (emailExists)
        {
            return ApplicationResult<AuthResponse>.Failure(
                ApplicationErrorKind.Conflict,
                "A user with this email already exists.");
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = displayName,
            PasswordHash = PasswordHasher.Hash(password),
            CreatedAt = now
        };

        var (refreshToken, refreshTokenEntity) = CreateRefreshToken(user.Id, now);

        dbContext.Users.Add(user);
        dbContext.Set<RefreshToken>().Add(refreshTokenEntity);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            return ApplicationResult<AuthResponse>.Failure(
                ApplicationErrorKind.Conflict,
                "A user with this email already exists.");
        }

        await SendWelcomeEmailIfPossibleAsync(
            user.Email,
            user.DisplayName,
            cancellationToken);

        return ApplicationResult<AuthResponse>.Success(
            BuildAuthResponse(user, refreshToken));
    }

    public async Task<ApplicationResult<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var password = request.Password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            return ApplicationResult<AuthResponse>.Failure(
                ApplicationErrorKind.Validation,
                "Email and password are required.");
        }

        var user = await dbContext.Users
            .FirstOrDefaultAsync(
                u => u.Email == email,
                cancellationToken);

        if (user is null ||
            !PasswordHasher.Verify(password, user.PasswordHash))
        {
            return ApplicationResult<AuthResponse>.Failure(
                ApplicationErrorKind.Unauthorized,
                "Invalid email or password.");
        }

        var refreshToken = await CreateRefreshTokenAsync(
            user.Id,
            cancellationToken);

        return ApplicationResult<AuthResponse>.Success(
            BuildAuthResponse(user, refreshToken));
    }

    public async Task<ApplicationResult<RefreshResponse>> RefreshTokenAsync(
        RefreshRequest request,
        CancellationToken cancellationToken = default)
    {
        var requestRefreshToken = request.RefreshToken?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(requestRefreshToken))
        {
            return ApplicationResult<RefreshResponse>.Failure(
                ApplicationErrorKind.Validation,
                "Refresh token is required.");
        }

        var tokenHash = RefreshTokenHasher.HashToken(requestRefreshToken);
        var now = DateTime.UtcNow;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        var refreshToken = await dbContext.Set<RefreshToken>()
            .Include(rt => rt.user)
            .FirstOrDefaultAsync(
                rt => rt.TokenHash == tokenHash,
                cancellationToken);

        if (refreshToken is null ||
            refreshToken.ExpiresAt <= now ||
            refreshToken.RevokedAt != null)
        {
            return ApplicationResult<RefreshResponse>.Failure(
                ApplicationErrorKind.Unauthorized,
                "Invalid or expired refresh token.");
        }

        var revokedRows = await dbContext.Set<RefreshToken>()
            .Where(rt =>
                rt.Id == refreshToken.Id &&
                rt.RevokedAt == null &&
                rt.ExpiresAt > now)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(rt => rt.RevokedAt, now),
                cancellationToken);

        if (revokedRows != 1)
        {
            return ApplicationResult<RefreshResponse>.Failure(
                ApplicationErrorKind.Unauthorized,
                "Invalid or expired refresh token.");
        }

        var (newRefreshToken, newRefreshTokenEntity) = CreateRefreshToken(
            refreshToken.UserId,
            now);

        dbContext.Set<RefreshToken>().Add(newRefreshTokenEntity);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ApplicationResult<RefreshResponse>.Success(new RefreshResponse
        {
            AccessToken = jwtTokenService.CreateToken(refreshToken.user),
            RefreshToken = newRefreshToken
        });
    }

    public async Task LogoutAsync(
        RefreshRequest? request,
        CancellationToken cancellationToken = default)
    {
        var requestRefreshToken = request?.RefreshToken?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(requestRefreshToken))
        {
            return;
        }

        var tokenHash = RefreshTokenHasher.HashToken(requestRefreshToken);
        var now = DateTime.UtcNow;

        await dbContext.Set<RefreshToken>()
            .Where(refreshToken =>
                refreshToken.TokenHash == tokenHash &&
                refreshToken.RevokedAt == null &&
                refreshToken.ExpiresAt > now)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    refreshToken => refreshToken.RevokedAt,
                    now),
                cancellationToken);
    }

    public async Task DeleteMyAccountAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(
                user => user.Id == userId,
                cancellationToken);

        if (user is null)
        {
            return;
        }

        var now = DateTime.UtcNow;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        await dbContext.Set<RefreshToken>()
            .Where(refreshToken =>
                refreshToken.UserId == userId &&
                refreshToken.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    refreshToken => refreshToken.RevokedAt,
                    now),
                cancellationToken);

        dbContext.Users.Remove(user);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<ApplicationResult<AuthResponse>> GoogleLoginAsync(
        GoogleAuthRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload =
                await oAuthProviderService.ValidateGoogleTokenAsync(
                    request.IdToken,
                    cancellationToken);

            var email = payload.Email
                .Trim()
                .ToLowerInvariant();
            var googleId = payload.Subject.Trim();

            var user = await dbContext.Users
                .FirstOrDefaultAsync(
                    u => u.GoogleId == googleId,
                    cancellationToken);

            if (user is null)
            {
                user = await dbContext.Users
                    .FirstOrDefaultAsync(
                        u => u.Email == email,
                        cancellationToken);

                if (user is null)
                {
                    user = new User
                    {
                        Id = Guid.NewGuid(),
                        Email = email,
                        DisplayName = payload.Name ?? email,
                        GoogleId = googleId,
                        CreatedAt = DateTime.UtcNow
                    };

                    dbContext.Users.Add(user);

                    await dbContext.SaveChangesAsync(
                        cancellationToken);
                }
                else if (string.IsNullOrWhiteSpace(user.GoogleId))
                {
                    user.GoogleId = googleId;

                    await dbContext.SaveChangesAsync(
                        cancellationToken);
                }
                else if (user.GoogleId != googleId)
                {
                    return ApplicationResult<AuthResponse>.Failure(
                        ApplicationErrorKind.Unauthorized,
                        "This email is already linked to another Google account.");
                }
            }
            var refreshToken = await CreateRefreshTokenAsync(
                user.Id,
                cancellationToken);

            return ApplicationResult<AuthResponse>.Success(
                BuildAuthResponse(
                    user,
                    refreshToken));
        }
        catch (Exception)
        {
            return ApplicationResult<AuthResponse>.Failure(
                ApplicationErrorKind.Unauthorized,
                "Invalid Google token.");
        }
    }

    public async Task<ApplicationResult<AuthResponse>> GithubLoginAsync(
        GithubAuthRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var githubUser = await oAuthProviderService.ValidateGithubCodeAsync(
                request.Code,
                request.RedirectUri,
                cancellationToken);

            var email = githubUser.Email
                .Trim()
                .ToLowerInvariant();

            var githubId = githubUser.ProviderUserId.Trim();

            var user = await dbContext.Users
                .FirstOrDefaultAsync(
                    u => u.GithubId == githubId,
                    cancellationToken);

            if (user is null)
            {
                user = await dbContext.Users
                    .FirstOrDefaultAsync(
                        u => u.Email == email,
                        cancellationToken);
            }

            if (user is null)
            {
                user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = email,
                    DisplayName = githubUser.DisplayName,
                    GithubId = githubId,
                    CreatedAt = DateTime.UtcNow
                };

                dbContext.Users.Add(user);

                await dbContext.SaveChangesAsync(
                    cancellationToken);
            }
            else if (string.IsNullOrWhiteSpace(user.GithubId))
            {
                user.GithubId = githubId;

                await dbContext.SaveChangesAsync(
                    cancellationToken);
            }
            else if (user.GithubId != githubId)
            {
                return ApplicationResult<AuthResponse>.Failure(
                    ApplicationErrorKind.Unauthorized,
                    "This email is already linked to another GitHub account.");
            }

            var refreshToken = await CreateRefreshTokenAsync(
                user.Id,
                cancellationToken);

            return ApplicationResult<AuthResponse>.Success(
                BuildAuthResponse(
                    user,
                    refreshToken));
        }
        catch (Exception)
        {
            return ApplicationResult<AuthResponse>.Failure(
                ApplicationErrorKind.Unauthorized,
                "Invalid GitHub authorization code.");
        }
    }

    public ApplicationResult<GithubAuthorizationStart> StartGithubLogin(
        string redirectUri)
    {
        try
        {
            var state = GenerateSecureToken();
            var authorizationUrl = oAuthProviderService.BuildGithubAuthorizationUrl(
                state,
                redirectUri);

            return ApplicationResult<GithubAuthorizationStart>.Success(
                new GithubAuthorizationStart
                {
                    AuthorizationUrl = authorizationUrl,
                    State = state
                });
        }
        catch (Exception)
        {
            return ApplicationResult<GithubAuthorizationStart>.Failure(
                ApplicationErrorKind.Validation,
                "GitHub sign in is not configured.");
        }
    }

    public async Task<ApplicationResult<string>> CompleteGithubCallbackAsync(
        GithubAuthRequest request,
        string expectedState,
        string actualState,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(expectedState) ||
            string.IsNullOrWhiteSpace(actualState) ||
            !string.Equals(expectedState, actualState, StringComparison.Ordinal))
        {
            return ApplicationResult<string>.Failure(
                ApplicationErrorKind.Unauthorized,
                "Invalid GitHub OAuth state.");
        }

        var authResult = await GithubLoginAsync(
            request,
            cancellationToken);

        if (!authResult.IsSuccess || authResult.Response is null)
        {
            return ApplicationResult<string>.Failure(
                authResult.ErrorKind ?? ApplicationErrorKind.Unauthorized,
                authResult.ErrorMessage ?? "GitHub sign in failed.");
        }

        var sessionCode = GenerateSecureToken();
        memoryCache.Set(
            GetGithubSessionCacheKey(sessionCode),
            authResult.Response,
            TimeSpan.FromMinutes(GithubSessionLifetimeMinutes));

        return ApplicationResult<string>.Success(sessionCode);
    }

    public ApplicationResult<AuthResponse> CompleteGithubSession(
        GithubSessionRequest request)
    {
        var sessionCode = request.SessionCode?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(sessionCode))
        {
            return ApplicationResult<AuthResponse>.Failure(
                ApplicationErrorKind.Validation,
                "GitHub session code is required.");
        }

        var cacheKey = GetGithubSessionCacheKey(sessionCode);

        if (!memoryCache.TryGetValue(cacheKey, out AuthResponse? response) ||
            response is null)
        {
            return ApplicationResult<AuthResponse>.Failure(
                ApplicationErrorKind.Unauthorized,
                "Invalid or expired GitHub session code.");
        }

        memoryCache.Remove(cacheKey);

        return ApplicationResult<AuthResponse>.Success(response);
    }

    private async Task<string> CreateRefreshTokenAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var (refreshToken, refreshTokenEntity) = CreateRefreshToken(
            userId,
            DateTime.UtcNow);

        dbContext.Set<RefreshToken>().Add(refreshTokenEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return refreshToken;
    }

    private static (string Token, RefreshToken Entity) CreateRefreshToken(
        Guid userId,
        DateTime now)
    {
        var refreshToken = RefreshTokenGenerator.GenerateToken();

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = RefreshTokenHasher.HashToken(refreshToken),
            CreatedAt = now,
            ExpiresAt = now.AddDays(RefreshTokenLifetimeDays)
        };

        return (refreshToken, refreshTokenEntity);
    }

    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);

        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string GetGithubSessionCacheKey(string sessionCode) =>
        $"{GithubSessionCachePrefix}{sessionCode}";


    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresUniqueViolation
        };

    private async Task SendWelcomeEmailIfPossibleAsync(
        string email,
        string displayName,
        CancellationToken cancellationToken)
    {
        try
        {
            await emailService.SendWelcomeEmailAsync(
                email,
                displayName,
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Failed to send welcome email to {Email}.",
                email);
        }
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
