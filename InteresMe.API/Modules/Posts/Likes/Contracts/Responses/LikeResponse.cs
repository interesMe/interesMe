namespace InteresMe.API.Modules.Posts.Likes.Contracts.Responses;

public sealed class LikeResponse
{
    public Guid PostId { get; set; }

    public int LikesCount { get; set; }

    public bool IsLikedByCurrentUser { get; set; }
}
