using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Data;
using InteresMe.API.Modules.Posts.Comments.Contracts.Requests;
using InteresMe.API.Modules.Posts.Comments.Contracts.Responses;
using InteresMe.API.Modules.Posts.Comments.Models;
using Microsoft.EntityFrameworkCore;

namespace InteresMe.API.Modules.Posts.Comments.Services;

public sealed class CommentService(AppDbContext dbContext) : ICommentService
{
    public async Task<ApplicationResult<CommentResponse>> CreateAsync(
        Guid authorId,
        Guid postId,
        CreateCommentRequest? request,
        CancellationToken cancellationToken = default)
    {
        var body = request?.Body?.Trim();
        if (string.IsNullOrWhiteSpace(body))
        {
            return ApplicationResult<CommentResponse>.Failure(
                ApplicationErrorKind.Validation,
                CommentErrorCodes.BodyRequired,
                "Comment body is required.");
        }

        if (body.Length > CommentRules.MaxBodyLength)
        {
            return ApplicationResult<CommentResponse>.Failure(
                ApplicationErrorKind.Validation,
                CommentErrorCodes.BodyTooLong,
                $"Comment body cannot exceed {CommentRules.MaxBodyLength} characters.");
        }

        var postExists = await dbContext.Posts
            .AsNoTracking()
            .AnyAsync(post => post.Id == postId, cancellationToken);

        if (!postExists)
        {
            return ApplicationResult<CommentResponse>.Failure(
                ApplicationErrorKind.NotFound,
                CommentErrorCodes.PostUnavailable,
                "Post is unavailable for commenting.");
        }

        var author = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == authorId)
            .Select(user => new CommentAuthorResponse
            {
                Id = user.Id,
                DisplayName = user.Profile != null ? user.Profile.DisplayName : user.DisplayName,
                AvatarUrl = user.Profile != null ? user.Profile.AvatarUrl : null
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (author is null)
        {
            return ApplicationResult<CommentResponse>.Failure(
                ApplicationErrorKind.NotFound,
                CommentErrorCodes.AuthorUnavailable,
                "Comment author is unavailable.");
        }

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            PostId = postId,
            AuthorId = authorId,
            Body = body,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Comments.Add(comment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ApplicationResult<CommentResponse>.Success(new CommentResponse
        {
            Id = comment.Id,
            PostId = comment.PostId,
            Author = author,
            Body = comment.Body,
            CreatedAt = comment.CreatedAt
        });
    }

    public async Task<ApplicationResult<CommentPageResponse>> GetForPostAsync(
        Guid postId,
        string? cursor,
        int pageSize = CommentRules.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        if (pageSize < 1 || pageSize > CommentRules.MaxPageSize)
        {
            return ApplicationResult<CommentPageResponse>.Failure(
                ApplicationErrorKind.Validation,
                CommentErrorCodes.PageSizeInvalid,
                $"Page size must be between 1 and {CommentRules.MaxPageSize}.");
        }

        CommentCursor? decodedCursor = null;
        if (!string.IsNullOrWhiteSpace(cursor))
        {
            if (!CommentCursor.TryDecode(cursor, out var parsedCursor))
            {
                return ApplicationResult<CommentPageResponse>.Failure(
                    ApplicationErrorKind.Validation,
                    CommentErrorCodes.CursorInvalid,
                    "Comment cursor is invalid.");
            }

            decodedCursor = parsedCursor;
        }

        var postExists = await dbContext.Posts
            .AsNoTracking()
            .AnyAsync(post => post.Id == postId, cancellationToken);

        if (!postExists)
        {
            return ApplicationResult<CommentPageResponse>.Failure(
                ApplicationErrorKind.NotFound,
                CommentErrorCodes.PostUnavailable,
                "Post is unavailable for commenting.");
        }

        var query = dbContext.Comments
            .AsNoTracking()
            .Where(comment => comment.PostId == postId);

        if (decodedCursor is CommentCursor pageCursor)
        {
            query = query.Where(comment =>
                comment.CreatedAt > pageCursor.CreatedAt ||
                (comment.CreatedAt == pageCursor.CreatedAt && comment.Id.CompareTo(pageCursor.Id) > 0));
        }

        var comments = await query
            .OrderBy(comment => comment.CreatedAt)
            .ThenBy(comment => comment.Id)
            .Select(comment => new CommentResponse
            {
                Id = comment.Id,
                PostId = comment.PostId,
                Author = new CommentAuthorResponse
                {
                    Id = comment.AuthorId,
                    DisplayName = comment.Author.Profile != null
                        ? comment.Author.Profile.DisplayName
                        : comment.Author.DisplayName,
                    AvatarUrl = comment.Author.Profile != null
                        ? comment.Author.Profile.AvatarUrl
                        : null
                },
                Body = comment.Body,
                CreatedAt = comment.CreatedAt
            })
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = comments.Count > pageSize;
        var items = hasMore ? comments.Take(pageSize).ToList() : comments;
        var nextCursor = hasMore
            ? new CommentCursor(items[^1].CreatedAt, items[^1].Id).Encode()
            : null;

        return ApplicationResult<CommentPageResponse>.Success(new CommentPageResponse
        {
            Items = items,
            NextCursor = nextCursor
        });
    }

    public async Task<ApplicationResult<bool>> DeleteAsync(
        Guid authorId,
        Guid postId,
        Guid commentId,
        CancellationToken cancellationToken = default)
    {
        var comment = await dbContext.Comments
            .FirstOrDefaultAsync(
                currentComment => currentComment.Id == commentId && currentComment.PostId == postId,
                cancellationToken);

        if (comment is null)
        {
            return ApplicationResult<bool>.Failure(
                ApplicationErrorKind.NotFound,
                CommentErrorCodes.NotFound,
                "Comment was not found.");
        }

        if (comment.AuthorId != authorId)
        {
            return ApplicationResult<bool>.Failure(
                ApplicationErrorKind.Forbidden,
                CommentErrorCodes.DeleteForbidden,
                "You cannot delete this comment.");
        }

        dbContext.Comments.Remove(comment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ApplicationResult<bool>.Success(true);
    }
}
