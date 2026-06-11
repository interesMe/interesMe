using System.Text;
using InteresMe.API.Data;
using Microsoft.EntityFrameworkCore;

namespace InteresMe.API.Modules.Initiatives.Services.Slugs;

public sealed class InitiativeSlugService(AppDbContext dbContext) : IInitiativeSlugService
{
    private const int SlugMaxLength = 140;
    private const int SlugSuffixLength = 8;

    public async Task<string> CreateUniqueSlugAsync(
        string title,
        CancellationToken cancellationToken = default)
    {
        var baseSlug = CreateSlug(title);

        if (string.IsNullOrWhiteSpace(baseSlug))
        {
            baseSlug = "initiative";
        }

        baseSlug = TrimSlug(baseSlug, SlugMaxLength);

        if (!await SlugExistsAsync(baseSlug, cancellationToken))
        {
            return baseSlug;
        }

        while (true)
        {
            var suffix = Guid.NewGuid().ToString("N")[..SlugSuffixLength];
            var prefix = TrimSlug(baseSlug, SlugMaxLength - suffix.Length - 1);
            var candidate = $"{prefix}-{suffix}";

            if (!await SlugExistsAsync(candidate, cancellationToken))
            {
                return candidate;
            }
        }
    }

    private async Task<bool> SlugExistsAsync(
        string slug,
        CancellationToken cancellationToken) =>
        await dbContext.Initiatives
            .AsNoTracking()
            .AnyAsync(initiative => initiative.Slug == slug, cancellationToken);

    private static string CreateSlug(string value)
    {
        var builder = new StringBuilder();
        var previousWasDash = false;

        foreach (var character in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousWasDash = false;
                continue;
            }

            if (previousWasDash)
            {
                continue;
            }

            builder.Append('-');
            previousWasDash = true;
        }

        return builder.ToString().Trim('-');
    }

    private static string TrimSlug(
        string slug,
        int maxLength) =>
        slug.Length <= maxLength
            ? slug
            : slug[..maxLength].Trim('-');
}
