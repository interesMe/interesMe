using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Data;
using InteresMe.API.Modules.History.DTOs;
using InteresMe.API.Modules.History.Services;
using InteresMe.API.Modules.Initiatives.Domain.Enums;
using InteresMe.API.Modules.Posts.Feed.Contracts.Requests;
using InteresMe.API.Modules.Posts.Feed.Contracts.Responses;
using InteresMe.API.Modules.Posts.Feed.Models;
using Microsoft.EntityFrameworkCore;

namespace InteresMe.API.Modules.Posts.Feed.Services;

public sealed class PostFeedService(
    AppDbContext dbContext,
    IUserHistoryService userHistoryService,
    IPostAttachmentValidator attachmentValidator,
    IPostAttachmentStorage attachmentStorage,
    ILogger<PostFeedService> logger) : IPostFeedService
{
    public async Task<ApplicationResult<PostResponse>> CreateAsync(
        Guid authorId,
        CreatePostRequest? request,
        IReadOnlyList<IFormFile> attachments,
        CancellationToken cancellationToken = default)
    {
        var body = request?.Body?.Trim();
        if (string.IsNullOrWhiteSpace(body))
        {
            return ApplicationResult<PostResponse>.Failure(
                ApplicationErrorKind.Validation,
                PostErrorCodes.BodyRequired,
                "Post body is required.");
        }

        if (body.Length > PostFeedRules.MaxBodyLength)
        {
            return ApplicationResult<PostResponse>.Failure(
                ApplicationErrorKind.Validation,
                PostErrorCodes.BodyTooLong,
                $"Post body cannot exceed {PostFeedRules.MaxBodyLength} characters.");
        }

        var attachmentValidation = await attachmentValidator.ValidateAsync(
            attachments,
            cancellationToken);
        if (!attachmentValidation.IsSuccess)
        {
            return ApplicationResult<PostResponse>.Failure(
                attachmentValidation.ErrorKind ?? ApplicationErrorKind.Validation,
                attachmentValidation.ErrorCode ?? PostErrorCodes.RequestInvalid,
                attachmentValidation.ErrorMessage ?? "Post attachments are invalid.");
        }

        var author = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == authorId)
            .Select(user => new
            {
                user.Id,
                DisplayName = user.Profile != null ? user.Profile.DisplayName : user.DisplayName,
                AvatarUrl = user.Profile != null ? user.Profile.AvatarUrl : null
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (author is null)
        {
            return ApplicationResult<PostResponse>.Failure(
                ApplicationErrorKind.NotFound,
                PostErrorCodes.UserNotFound,
                "User was not found.");
        }

        string? initiativeTitle = null;
        if (request?.InitiativeId is Guid initiativeId)
        {
            initiativeTitle = await dbContext.Initiatives
                .AsNoTracking()
                .Where(currentInitiative =>
                    currentInitiative.Id == initiativeId &&
                    (currentInitiative.OwnerUserId == authorId ||
                        currentInitiative.JoinRequests.Any(joinRequest =>
                            joinRequest.UserId == authorId &&
                            joinRequest.Status == InitiativeJoinRequestStatus.Accepted)))
                .Select(currentInitiative => currentInitiative.Title)
                .FirstOrDefaultAsync(cancellationToken);

            if (initiativeTitle is null)
            {
                return ApplicationResult<PostResponse>.Failure(
                    ApplicationErrorKind.NotFound,
                    PostErrorCodes.InitiativeUnavailable,
                    "Initiative is unavailable for publishing.");
            }
        }

        var post = new Post
        {
            Id = Guid.NewGuid(),
            AuthorId = authorId,
            Body = body,
            InitiativeId = request?.InitiativeId,
            CreatedAt = DateTime.UtcNow
        };

        IReadOnlyList<StagedPostAttachment> stagedAttachments = [];
        try
        {
            stagedAttachments = await attachmentStorage.StageAsync(
                authorId,
                post.Id,
                attachmentValidation.Response!,
                cancellationToken);

            foreach (var stagedAttachment in stagedAttachments)
            {
                post.Attachments.Add(new PostAttachment
                {
                    Id = stagedAttachment.Id,
                    PostId = post.Id,
                    Kind = PostAttachmentKinds.Image,
                    StoragePath = stagedAttachment.StoragePath,
                    ContentType = stagedAttachment.ContentType,
                    SizeBytes = stagedAttachment.SizeBytes,
                    SortOrder = stagedAttachment.SortOrder,
                    CreatedAt = post.CreatedAt
                });
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            dbContext.Posts.Add(post);
            await userHistoryService.AddEventAsync(
                new AddUserHistoryEventRequest(
                    UserId: authorId,
                    Type: HistoryEventTypes.CreatedPost,
                    Title: "Published a post",
                    TargetType: "post",
                    TargetId: post.Id,
                    OccurredAt: new DateTimeOffset(post.CreatedAt)),
                cancellationToken);

            attachmentStorage.MoveToFinal(stagedAttachments);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            attachmentStorage.CleanupAll(stagedAttachments);
            throw;
        }
        catch (Exception exception)
        {
            attachmentStorage.CleanupAll(stagedAttachments);
            logger.LogError(exception, "Failed to publish a post with attachments.");

            return ApplicationResult<PostResponse>.Failure(
                ApplicationErrorKind.InternalServerError,
                PostErrorCodes.AttachmentUploadFailed,
                "Post could not be published.");
        }

        return ApplicationResult<PostResponse>.Success(new PostResponse
        {
            Id = post.Id,
            AuthorId = post.AuthorId,
            AuthorDisplayName = author.DisplayName,
            AuthorAvatarUrl = author.AvatarUrl,
            Body = post.Body,
            InitiativeId = post.InitiativeId,
            InitiativeTitle = initiativeTitle,
            LikesCount = 0,
            IsLikedByCurrentUser = false,
            Attachments = post.Attachments
                .OrderBy(attachment => attachment.SortOrder)
                .Select(ToAttachmentResponse)
                .ToList(),
            CreatedAt = post.CreatedAt
        });
    }

    public async Task<ApplicationResult<PostResponse>> GetByIdAsync(
        Guid viewerUserId,
        Guid postId,
        CancellationToken cancellationToken = default)
    {
        var post = await PostResponses(viewerUserId)
            .FirstOrDefaultAsync(currentPost => currentPost.Id == postId, cancellationToken);

        return post is null
            ? ApplicationResult<PostResponse>.Failure(
                ApplicationErrorKind.NotFound,
                PostErrorCodes.NotFound,
                "Post was not found.")
            : ApplicationResult<PostResponse>.Success(post);
    }

    public async Task<ApplicationResult<PostPageResponse>> GetUserPostsAsync(
        Guid viewerUserId,
        Guid userId,
        string? cursor,
        int pageSize = PostFeedRules.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var userExists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == userId, cancellationToken);

        if (!userExists)
        {
            return ApplicationResult<PostPageResponse>.Failure(
                ApplicationErrorKind.NotFound,
                PostErrorCodes.UserNotFound,
                "User was not found.");
        }

        PostCursor? decodedCursor = null;
        if (!string.IsNullOrWhiteSpace(cursor))
        {
            if (!PostCursor.TryDecode(cursor, out var parsedCursor))
            {
                return ApplicationResult<PostPageResponse>.Failure(
                    ApplicationErrorKind.Validation,
                    PostErrorCodes.CursorInvalid,
                    "Post cursor is invalid.");
            }

            decodedCursor = parsedCursor;
        }

        pageSize = Math.Clamp(pageSize, 1, PostFeedRules.MaxPageSize);

        var query = dbContext.Posts
            .AsNoTracking()
            .Where(post => post.AuthorId == userId);

        if (decodedCursor is PostCursor pageCursor)
        {
            query = query.Where(post =>
                post.CreatedAt < pageCursor.CreatedAt ||
                (post.CreatedAt == pageCursor.CreatedAt && post.Id.CompareTo(pageCursor.Id) < 0));
        }

        var posts = await query
            .OrderByDescending(post => post.CreatedAt)
            .ThenByDescending(post => post.Id)
            .Select(post => new PostResponse
            {
                Id = post.Id,
                AuthorId = post.AuthorId,
                AuthorDisplayName = post.Author.Profile != null
                    ? post.Author.Profile.DisplayName
                    : post.Author.DisplayName,
                AuthorAvatarUrl = post.Author.Profile != null
                    ? post.Author.Profile.AvatarUrl
                    : null,
                Body = post.Body,
                InitiativeId = post.InitiativeId,
                InitiativeTitle = post.Initiative != null ? post.Initiative.Title : null,
                LikesCount = dbContext.PostLikes.Count(like => like.PostId == post.Id),
                IsLikedByCurrentUser = dbContext.PostLikes.Any(like =>
                    like.PostId == post.Id && like.UserId == viewerUserId),
                Attachments = post.Attachments
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
                CreatedAt = post.CreatedAt
            })
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = posts.Count > pageSize;
        var items = hasMore ? posts.Take(pageSize).ToList() : posts;
        var nextCursor = hasMore
            ? new PostCursor(items[^1].CreatedAt, items[^1].Id).Encode()
            : null;

        return ApplicationResult<PostPageResponse>.Success(new PostPageResponse
        {
            Items = items,
            NextCursor = nextCursor
        });
    }

    private IQueryable<PostResponse> PostResponses(Guid viewerUserId) =>
        dbContext.Posts
            .AsNoTracking()
            .Select(post => new PostResponse
            {
                Id = post.Id,
                AuthorId = post.AuthorId,
                AuthorDisplayName = post.Author.Profile != null
                    ? post.Author.Profile.DisplayName
                    : post.Author.DisplayName,
                AuthorAvatarUrl = post.Author.Profile != null
                    ? post.Author.Profile.AvatarUrl
                    : null,
                Body = post.Body,
                InitiativeId = post.InitiativeId,
                InitiativeTitle = post.Initiative != null ? post.Initiative.Title : null,
                LikesCount = dbContext.PostLikes.Count(like => like.PostId == post.Id),
                IsLikedByCurrentUser = dbContext.PostLikes.Any(like =>
                    like.PostId == post.Id && like.UserId == viewerUserId),
                Attachments = post.Attachments
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
                CreatedAt = post.CreatedAt
            });

    private static PostAttachmentResponse ToAttachmentResponse(PostAttachment attachment) => new()
    {
        Id = attachment.Id,
        Kind = attachment.Kind,
        Url = attachment.StoragePath,
        ContentType = attachment.ContentType,
        SizeBytes = attachment.SizeBytes,
        Order = attachment.SortOrder
    };
}
