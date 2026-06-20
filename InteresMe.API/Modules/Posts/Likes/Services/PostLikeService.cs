using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Data;
using InteresMe.API.Modules.Posts.Likes.Contracts.Responses;
using Microsoft.EntityFrameworkCore;

namespace InteresMe.API.Modules.Posts.Likes.Services;

public sealed class PostLikeService(AppDbContext dbContext) : IPostLikeService
{
    public async Task<ApplicationResult<LikeResponse>> ToggleAsync(
        Guid userId,
        Guid postId,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(userId, postId, cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            WITH deleted AS (
                DELETE FROM posts.likes
                WHERE "PostId" = {postId} AND "UserId" = {userId}
                RETURNING 1
            )
            INSERT INTO posts.likes ("PostId", "UserId", "CreatedAt")
            SELECT {postId}, {userId}, {DateTime.UtcNow}
            WHERE NOT EXISTS (SELECT 1 FROM deleted)
            ON CONFLICT ("PostId", "UserId") DO NOTHING
            """,
            cancellationToken);

        return ApplicationResult<LikeResponse>.Success(
            await GetStateAsync(userId, postId, cancellationToken));
    }

    public async Task<ApplicationResult<LikeResponse>> UnlikeAsync(
        Guid userId,
        Guid postId,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(userId, postId, cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        await dbContext.PostLikes
            .Where(like => like.PostId == postId && like.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        return ApplicationResult<LikeResponse>.Success(
            await GetStateAsync(userId, postId, cancellationToken));
    }

    private async Task<ApplicationResult<LikeResponse>?> ValidateAsync(
        Guid userId,
        Guid postId,
        CancellationToken cancellationToken)
    {
        var userExists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == userId, cancellationToken);

        if (!userExists)
        {
            return ApplicationResult<LikeResponse>.Failure(
                ApplicationErrorKind.NotFound,
                LikeErrorCodes.UserUnavailable,
                "User is unavailable for liking posts.");
        }

        var postExists = await dbContext.Posts
            .AsNoTracking()
            .AnyAsync(post => post.Id == postId, cancellationToken);

        return postExists
            ? null
            : ApplicationResult<LikeResponse>.Failure(
                ApplicationErrorKind.NotFound,
                LikeErrorCodes.PostUnavailable,
                "Post is unavailable for liking.");
    }

    private async Task<LikeResponse> GetStateAsync(
        Guid userId,
        Guid postId,
        CancellationToken cancellationToken)
    {
        var likesCount = await dbContext.PostLikes
            .AsNoTracking()
            .CountAsync(like => like.PostId == postId, cancellationToken);

        var isLikedByCurrentUser = await dbContext.PostLikes
            .AsNoTracking()
            .AnyAsync(
                like => like.PostId == postId && like.UserId == userId,
                cancellationToken);

        return new LikeResponse
        {
            PostId = postId,
            LikesCount = likesCount,
            IsLikedByCurrentUser = isLikedByCurrentUser
        };
    }
}
