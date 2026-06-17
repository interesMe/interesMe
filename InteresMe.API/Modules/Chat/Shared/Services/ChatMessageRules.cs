namespace InteresMe.API.Modules.Chat.Shared.Services;

public static class ChatMessageRules
{
    public const int MessageMaxLength = 2000;
    public const int MessagesDefaultTake = 50;
    public const int MessagesMaxTake = 100;
    public const int LastMessagePreviewMaxLength = 140;

    public static int NormalizeTake(int take) =>
        take <= 0
            ? MessagesDefaultTake
            : Math.Min(take, MessagesMaxTake);

    public static string NormalizeMessageText(string? text) =>
        text?.Trim() ?? string.Empty;

    public static string ToPreview(string text) =>
        text.Length <= LastMessagePreviewMaxLength
            ? text
            : string.Concat(text.AsSpan(0, LastMessagePreviewMaxLength), "...");
}
