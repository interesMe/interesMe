using InteresMe.API.Modules.Feed.DTOs;

namespace InteresMe.API.Modules.Feed.Services;

public interface IFeedService
{
    Task<IReadOnlyList<PostResponse>> GetFeedAsync();

    Task<PostResponse?> GetByIdAsync(Guid id);

    Task<PostResponse?> CreateAsync(CreatePostRequest request);

    Task<bool> DeleteAsync(Guid id);
}
