namespace InteresMe.API.Modules.Posts.Feed.Services;

public interface IPostCreateRequestReader
{
    Task<PostCreateRequestBinding> ReadAsync(
        HttpRequest request,
        CancellationToken cancellationToken = default);
}
