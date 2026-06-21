using System.Security.Cryptography;
using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Data;
using InteresMe.API.Modules.Posts.Shares.Contracts.Responses;
using InteresMe.API.Modules.Posts.Feed.Contracts.Responses;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace InteresMe.API.Modules.Posts.Shares.Services;

public sealed class PostShareService(AppDbContext dbContext) : IPostShareService
{
    public async Task<ApplicationResult<ShareLinkResponse>> GetOrCreateAsync(
        Guid postId,
        string publicBaseUrl,
        CancellationToken cancellationToken = default)
    {
        var postExists = await dbContext.Posts
            .AsNoTracking()
            .AnyAsync(post => post.Id == postId, cancellationToken);

        if (!postExists)
        {
            return ApplicationResult<ShareLinkResponse>.Failure(
                ApplicationErrorKind.NotFound,
                PostShareErrorCodes.PostUnavailable,
                "Post is unavailable for sharing.");
        }

        var token = await dbContext.PostShareLinks
            .AsNoTracking()
            .Where(link => link.PostId == postId)
            .Select(link => link.Token)
            .FirstOrDefaultAsync(cancellationToken);

        if (token is null)
        {
            var candidate = WebEncoders.Base64UrlEncode(
                RandomNumberGenerator.GetBytes(PostShareRules.TokenEntropyBytes));

            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO posts.share_links ("PostId", "Token", "CreatedAt")
                VALUES ({postId}, {candidate}, {DateTime.UtcNow})
                ON CONFLICT ("PostId") DO NOTHING
                """,
                cancellationToken);

            token = await dbContext.PostShareLinks
                .AsNoTracking()
                .Where(link => link.PostId == postId)
                .Select(link => link.Token)
                .FirstAsync(cancellationToken);
        }

        return ApplicationResult<ShareLinkResponse>.Success(new ShareLinkResponse
        {
            ShareUrl = $"{publicBaseUrl.TrimEnd('/')}/p/{token}"
        });
    }

    public async Task<ApplicationResult<PublicPostResponse>> GetPublicAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (!PostShareRules.IsValidToken(token))
        {
            return LinkNotFound();
        }

        var post = await dbContext.PostShareLinks
            .AsNoTracking()
            .Where(link => link.Token == token)
            .Select(link => new PublicPostResponse
            {
                Body = link.Post.Body,
                Author = new PublicPostAuthorResponse
                {
                    DisplayName = link.Post.Author.Profile != null
                        ? link.Post.Author.Profile.DisplayName
                        : link.Post.Author.DisplayName,
                    AvatarUrl = link.Post.Author.Profile != null
                        ? link.Post.Author.Profile.AvatarUrl
                        : null
                },
                Initiative = link.Post.Initiative == null
                    ? null
                    : new PublicPostInitiativeResponse
                    {
                        Slug = link.Post.Initiative.Slug,
                        Title = link.Post.Initiative.Title
                    },
                Attachments = link.Post.Attachments
                    .OrderBy(attachment => attachment.SortOrder)
                    .Select(attachment => new PostAttachmentResponse
                    {
                        Id = attachment.Id,
                        Kind = attachment.Kind,
                        Url = attachment.StoragePath,
                        ContentType = attachment.ContentType,
                        SizeBytes = attachment.SizeBytes,
                        Order = attachment.SortOrder
                    })
                    .ToList(),
                CreatedAt = link.Post.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return post is null
            ? LinkNotFound()
            : ApplicationResult<PublicPostResponse>.Success(post);
    }

    private static ApplicationResult<PublicPostResponse> LinkNotFound() =>
        ApplicationResult<PublicPostResponse>.Failure(
            ApplicationErrorKind.NotFound,
            PostShareErrorCodes.LinkNotFound,
            "Shared post was not found.");
}
