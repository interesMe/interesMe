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
    public const string RequestInvalid = "post.request.invalid";
    public const string AttachmentCountExceeded = "post.attachment.count_exceeded";
    public const string AttachmentEmpty = "post.attachment.empty";
    public const string AttachmentTooLarge = "post.attachment.too_large";
    public const string AttachmentTotalSizeExceeded = "post.attachment.total_size_exceeded";
    public const string AttachmentExtensionUnsupported = "post.attachment.extension_unsupported";
    public const string AttachmentContentTypeUnsupported = "post.attachment.content_type_unsupported";
    public const string AttachmentSignatureInvalid = "post.attachment.signature_invalid";
    public const string AttachmentUploadFailed = "post.attachment.upload_failed";
}
