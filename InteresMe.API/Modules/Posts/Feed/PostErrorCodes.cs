namespace InteresMe.API.Modules.Posts.Feed;

public static class PostErrorCodes
{
    public const string NotFound = "post.not_found";
    public const string UserNotFound = "post.user_not_found";
    public const string BodyRequired = "post.body.required";
    public const string BodyTooLong = "post.body.too_long";
    public const string InitiativeUnavailable = "post.initiative.unavailable";
    public const string PageSizeInvalid = "post.page_size.invalid";
    public const string CursorInvalid = "post.cursor.invalid";
}
