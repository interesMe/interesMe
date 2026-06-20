using System.Globalization;
using System.Text;

namespace InteresMe.API.Modules.Posts.Feed.Services;

internal readonly record struct PostCursor(DateTime CreatedAt, Guid Id)
{
    public string Encode()
    {
        var value = string.Create(
            CultureInfo.InvariantCulture,
            $"{CreatedAt.ToUniversalTime().Ticks}:{Id:N}");

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static bool TryDecode(string value, out PostCursor cursor)
    {
        cursor = default;

        if (string.IsNullOrWhiteSpace(value) || value.Length > PostFeedRules.MaxCursorLength)
        {
            return false;
        }

        try
        {
            var base64 = value.Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight(base64.Length + ((4 - base64.Length % 4) % 4), '=');

            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
            var parts = decoded.Split(':', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2 ||
                !long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks) ||
                !Guid.TryParseExact(parts[1], "N", out var id) ||
                ticks < DateTime.MinValue.Ticks ||
                ticks > DateTime.MaxValue.Ticks)
            {
                return false;
            }

            cursor = new PostCursor(new DateTime(ticks, DateTimeKind.Utc), id);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
