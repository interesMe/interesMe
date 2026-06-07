using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Google.Apis.Auth;
using InteresMe.API.Modules.Auth.Services.Contracts;

namespace InteresMe.API.Modules.Auth.Services;

public sealed class OAuthProviderService(
    IConfiguration configuration,
    HttpClient httpClient) : IOAuthProviderService
{
    public string BuildGithubAuthorizationUrl(
        string state,
        string redirectUri)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException(
                "GitHub OAuth state is required.",
                nameof(state));
        }

        if (string.IsNullOrWhiteSpace(redirectUri))
        {
            throw new ArgumentException(
                "GitHub OAuth redirect URI is required.",
                nameof(redirectUri));
        }

        var clientId = GetGithubClientId();

        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new InvalidOperationException(
                "GitHub ClientId is not configured.");
        }

        var query = new QueryString()
            .Add("client_id", clientId)
            .Add("redirect_uri", redirectUri)
            .Add("scope", "user:email")
            .Add("state", state);

        return $"https://github.com/login/oauth/authorize{query}";
    }

    public async Task<GoogleJsonWebSignature.Payload> ValidateGoogleTokenAsync(
        string idToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            throw new ArgumentException(
                "Google id token is required.",
                nameof(idToken));
        }

        var clientId = configuration["Google:ClientId"];

        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new InvalidOperationException(
                "Google ClientId is not configured.");
        }

        var payload = await GoogleJsonWebSignature.ValidateAsync(
            idToken,
            new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [clientId]
            });

        if (payload is null)
        {
            throw new InvalidOperationException(
                "Failed to validate Google token.");
        }

        if (string.IsNullOrWhiteSpace(payload.Subject))
        {
            throw new InvalidOperationException(
                "Google token does not contain subject.");
        }

        if (string.IsNullOrWhiteSpace(payload.Email))
        {
            throw new InvalidOperationException(
                "Google token does not contain email.");
        }

        if (!payload.EmailVerified)
        {
            throw new InvalidOperationException(
                "Google email is not verified.");
        }

        return payload;
    }

    public async Task<OAuthUserInfo> ValidateGithubCodeAsync(
        string code,
        string? redirectUri,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(
                "GitHub authorization code is required.",
                nameof(code));
        }

        var clientId = GetGithubClientId();
        var clientSecret = GetGithubClientSecret();

        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new InvalidOperationException(
                "GitHub ClientId is not configured.");
        }

        if (string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new InvalidOperationException(
                "GitHub ClientSecret is not configured.");
        }

        var accessToken = await ExchangeGithubCodeAsync(
            code.Trim(),
            redirectUri,
            clientId,
            clientSecret,
            cancellationToken);

        var githubUser = await GetGithubUserAsync(
            accessToken,
            cancellationToken);

        var email = await GetGithubPrimaryVerifiedEmailAsync(
            accessToken,
            cancellationToken);

        var displayName = string.IsNullOrWhiteSpace(githubUser.Name)
            ? githubUser.Login
            : githubUser.Name;

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = email;
        }

        return new OAuthUserInfo
        {
            ProviderUserId = githubUser.Id.ToString(),
            Email = email,
            DisplayName = displayName
        };
    }

    private async Task<string> ExchangeGithubCodeAsync(
        string code,
        string? redirectUri,
        string clientId,
        string clientSecret,
        CancellationToken cancellationToken)
    {
        var form = new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["code"] = code
        };

        if (!string.IsNullOrWhiteSpace(redirectUri))
        {
            form["redirect_uri"] = redirectUri.Trim();
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://github.com/login/oauth/access_token")
        {
            Content = new FormUrlEncodedContent(form)
        };

        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await httpClient.SendAsync(
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                "Failed to exchange GitHub authorization code.");
        }

        var tokenResponse = await response.Content.ReadFromJsonAsync<GithubTokenResponse>(
            cancellationToken);

        if (tokenResponse is null ||
            !string.IsNullOrWhiteSpace(tokenResponse.Error) ||
            string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
        {
            throw new InvalidOperationException(
                tokenResponse?.ErrorDescription ??
                "GitHub token response did not contain an access token.");
        }

        return tokenResponse.AccessToken;
    }

    private async Task<GithubUserResponse> GetGithubUserAsync(
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var request = CreateGithubApiRequest(
            HttpMethod.Get,
            "https://api.github.com/user",
            accessToken);

        var response = await httpClient.SendAsync(
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                "Failed to get GitHub user profile.");
        }

        var user = await response.Content.ReadFromJsonAsync<GithubUserResponse>(
            cancellationToken);

        if (user is null ||
            user.Id <= 0)
        {
            throw new InvalidOperationException(
                "GitHub user profile is invalid.");
        }

        return user;
    }

    private async Task<string> GetGithubPrimaryVerifiedEmailAsync(
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var request = CreateGithubApiRequest(
            HttpMethod.Get,
            "https://api.github.com/user/emails",
            accessToken);

        var response = await httpClient.SendAsync(
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                "Failed to get GitHub user emails.");
        }

        var emails = await response.Content.ReadFromJsonAsync<List<GithubEmailResponse>>(
            cancellationToken);

        var email = emails?
            .Where(item => item is { Primary: true, Verified: true } &&
                           !string.IsNullOrWhiteSpace(item.Email))
            .Select(item => item.Email.Trim().ToLowerInvariant())
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException(
                "GitHub account does not contain a primary verified email.");
        }

        return email;
    }

    private static HttpRequestMessage CreateGithubApiRequest(
        HttpMethod method,
        string requestUri,
        string accessToken)
    {
        var request = new HttpRequestMessage(method, requestUri);

        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            accessToken);
        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.UserAgent.ParseAdd("InteresMe.API");
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");

        return request;
    }

    private string? GetGithubClientId() =>
        configuration["GitHub:ClientId"] ??
        configuration["GITHUB_CLIENT_ID"];

    private string? GetGithubClientSecret() =>
        configuration["GitHub:ClientSecret"] ??
        configuration["GITHUB_CLIENT_SECRET"];

    private sealed class GithubTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("error_description")]
        public string? ErrorDescription { get; set; }
    }

    private sealed class GithubUserResponse
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("login")]
        public string? Login { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    private sealed class GithubEmailResponse
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("primary")]
        public bool Primary { get; set; }

        [JsonPropertyName("verified")]
        public bool Verified { get; set; }
    }
}
