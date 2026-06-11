namespace InteresMe.API.Modules.Initiatives.Services.Slugs;

public interface IInitiativeSlugService
{
    Task<string> CreateUniqueSlugAsync(
        string title,
        CancellationToken cancellationToken = default);
}
