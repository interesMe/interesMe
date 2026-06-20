namespace InteresMe.API.Modules.Posts.Shares;

internal static class PostShareRules
{
    public const int TokenEntropyBytes = 32;
    public const int TokenLength = 43;
    public const int MaxTokenLength = 64;

    public static bool IsValidToken(string? token) =>
        token is { Length: TokenLength } &&
        token.All(character =>
            char.IsAsciiLetterOrDigit(character) || character is '-' or '_');
}
