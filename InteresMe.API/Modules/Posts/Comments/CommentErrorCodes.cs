namespace InteresMe.API.Modules.Posts.Comments;

public static class CommentErrorCodes
{
    public const string NotFound = "comment.not_found";
    public const string AuthorUnavailable = "comment.author.unavailable";
    public const string BodyRequired = "comment.body.required";
    public const string BodyTooLong = "comment.body.too_long";
    public const string PostUnavailable = "comment.post.unavailable";
    public const string ParentUnavailable = "comment.parent.unavailable";
    public const string ReplyToReplyNotSupported = "comment.reply_to_reply.not_supported";
    public const string DeleteForbidden = "comment.delete.forbidden";
    public const string PageSizeInvalid = "comment.page_size.invalid";
    public const string CursorInvalid = "comment.cursor.invalid";
}
