using System.Data;
using System.Security.Cryptography;
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
    IMemoryCache memoryCache)
    : IAuthService
{
    private const int MinPasswordLength = 8;
    private const int RefreshTokenLifetimeDays = 7;
    private const int GithubSessionLifetimeMinutes = 5;
    private const string GithubSessionCachePrefix = "github-session:";
    private const string PostgresUniqueViolation = "23505";

    public async Task<AuthResult<AuthResponse>> RegisterAsync(
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
            return AuthResult<AuthResponse>.Failure(
                AuthErrorKind.EmailAlreadyExists,
                "A user with this email already exists.");
        }

        return AuthResult<AuthResponse>.Success(
            BuildAuthResponse(user, refreshToken));
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

        var refreshToken = await CreateRefreshTokenAsync(
            user.Id,
            cancellationToken);

        return AuthResult<AuthResponse>.Success(
            BuildAuthResponse(user, refreshToken));
    }

    public async Task<AuthResult<RefreshResponse>> RefreshTokenAsync(
        RefreshRequest request,
        CancellationToken cancellationToken = default)
    {
        var requestRefreshToken = request.RefreshToken?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(requestRefreshToken))
        {
            return AuthResult<RefreshResponse>.Failure(
                AuthErrorKind.Validation,
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
            return AuthResult<RefreshResponse>.Failure(
                AuthErrorKind.InvalidCredentials,
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
            return AuthResult<RefreshResponse>.Failure(
                AuthErrorKind.InvalidCredentials,
                "Invalid or expired refresh token.");
        }

        var (newRefreshToken, newRefreshTokenEntity) = CreateRefreshToken(
            refreshToken.UserId,
            now);

        dbContext.Set<RefreshToken>().Add(newRefreshTokenEntity);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return AuthResult<RefreshResponse>.Success(new RefreshResponse
        {
            AccessToken = jwtTokenService.CreateToken(refreshToken.user),
            RefreshToken = newRefreshToken
        });
    }
    public async Task<AuthResult<AuthResponse>> GoogleLoginAsync(
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

            var user = await dbContext.Users
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
                    GoogleId = payload.Subject,
                    CreatedAt = DateTime.UtcNow
                };

                dbContext.Users.Add(user);

                await dbContext.SaveChangesAsync(
                    cancellationToken);
            }
            else if (string.IsNullOrWhiteSpace(user.GoogleId))
            {
                user.GoogleId = payload.Subject;

                await dbContext.SaveChangesAsync(
                    cancellationToken);
            }

            var refreshToken = await CreateRefreshTokenAsync(
                user.Id,
                cancellationToken);

            return AuthResult<AuthResponse>.Success(
                BuildAuthResponse(
                    user,
                    refreshToken));
        }
        catch (Exception)
        {
            return AuthResult<AuthResponse>.Failure(
                AuthErrorKind.InvalidCredentials,
                "Invalid Google token.");
        }
    }

    public async Task<AuthResult<AuthResponse>> GithubLoginAsync(
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
                return AuthResult<AuthResponse>.Failure(
                    AuthErrorKind.InvalidCredentials,
                    "This email is already linked to another GitHub account.");
            }

            var refreshToken = await CreateRefreshTokenAsync(
                user.Id,
                cancellationToken);

            return AuthResult<AuthResponse>.Success(
                BuildAuthResponse(
                    user,
                    refreshToken));
        }
        catch (Exception)
        {
            return AuthResult<AuthResponse>.Failure(
                AuthErrorKind.InvalidCredentials,
                "Invalid GitHub authorization code.");
        }
    }

    public AuthResult<GithubAuthorizationStart> StartGithubLogin(
        string redirectUri)
    {
        try
        {
            var state = GenerateSecureToken();
            var authorizationUrl = oAuthProviderService.BuildGithubAuthorizationUrl(
                state,
                redirectUri);

            return AuthResult<GithubAuthorizationStart>.Success(
                new GithubAuthorizationStart
                {
                    AuthorizationUrl = authorizationUrl,
                    State = state
                });
        }
        catch (Exception)
        {
            return AuthResult<GithubAuthorizationStart>.Failure(
                AuthErrorKind.Validation,
                "GitHub sign in is not configured.");
        }
    }

    public async Task<AuthResult<string>> CompleteGithubCallbackAsync(
        GithubAuthRequest request,
        string expectedState,
        string actualState,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(expectedState) ||
            string.IsNullOrWhiteSpace(actualState) ||
            !string.Equals(expectedState, actualState, StringComparison.Ordinal))
        {
            return AuthResult<string>.Failure(
                AuthErrorKind.InvalidCredentials,
                "Invalid GitHub OAuth state.");
        }

        var authResult = await GithubLoginAsync(
            request,
            cancellationToken);

        if (!authResult.IsSuccess || authResult.Response is null)
        {
            return AuthResult<string>.Failure(
                authResult.ErrorKind ?? AuthErrorKind.InvalidCredentials,
                authResult.ErrorMessage ?? "GitHub sign in failed.");
        }

        var sessionCode = GenerateSecureToken();
        memoryCache.Set(
            GetGithubSessionCacheKey(sessionCode),
            authResult.Response,
            TimeSpan.FromMinutes(GithubSessionLifetimeMinutes));

        return AuthResult<string>.Success(sessionCode);
    }

    public AuthResult<AuthResponse> CompleteGithubSession(
        GithubSessionRequest request)
    {
        var sessionCode = request.SessionCode?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(sessionCode))
        {
            return AuthResult<AuthResponse>.Failure(
                AuthErrorKind.Validation,
                "GitHub session code is required.");
        }

        var cacheKey = GetGithubSessionCacheKey(sessionCode);

        if (!memoryCache.TryGetValue(cacheKey, out AuthResponse? response) ||
            response is null)
        {
            return AuthResult<AuthResponse>.Failure(
                AuthErrorKind.InvalidCredentials,
                "Invalid or expired GitHub session code.");
        }

        memoryCache.Remove(cacheKey);

        return AuthResult<AuthResponse>.Success(response);
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
