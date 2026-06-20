using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Modules.Posts.Shares.Contracts.Responses;

namespace InteresMe.API.Modules.Posts.Shares.Services;

public interface IPostShareService
{
    Task<ApplicationResult<ShareLinkResponse>> GetOrCreateAsync(
        Guid postId,
        string publicBaseUrl,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<PublicPostResponse>> GetPublicAsync(
        string token,
        CancellationToken cancellationToken = default);
}
