using InteresMe.API.BuildingBlocks.Results;

namespace InteresMe.API.Modules.Posts.Feed.Services;

public sealed class PostAttachmentValidator : IPostAttachmentValidator
{
    private static readonly Dictionary<string, AttachmentFormat> FormatsByExtension =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = new(".jpg", "image/jpeg", ImageSignature.Jpeg),
            [".jpeg"] = new(".jpg", "image/jpeg", ImageSignature.Jpeg),
            [".png"] = new(".png", "image/png", ImageSignature.Png),
            [".webp"] = new(".webp", "image/webp", ImageSignature.WebP)
        };

    public async Task<ApplicationResult<IReadOnlyList<ValidatedPostAttachment>>> ValidateAsync(
        IReadOnlyList<IFormFile> files,
        CancellationToken cancellationToken = default)
    {
        if (files.Count > PostFeedRules.MaxAttachmentCount)
        {
            return Failure(
                PostErrorCodes.AttachmentCountExceeded,
                $"A post can contain at most {PostFeedRules.MaxAttachmentCount} images.");
        }

        long totalSize = 0;
        var validated = new List<ValidatedPostAttachment>(files.Count);

        for (var index = 0; index < files.Count; index++)
        {
            var file = files[index];
            if (file.Length <= 0)
            {
                return Failure(PostErrorCodes.AttachmentEmpty, "Post attachment cannot be empty.");
            }

            if (file.Length > PostFeedRules.MaxAttachmentSizeBytes)
            {
                return Failure(
                    PostErrorCodes.AttachmentTooLarge,
                    "Each post image must be 5 MiB or smaller.");
            }

            totalSize += file.Length;
            if (totalSize > PostFeedRules.MaxTotalAttachmentSizeBytes)
            {
                return Failure(
                    PostErrorCodes.AttachmentTotalSizeExceeded,
                    "Post images must be 20 MiB or smaller in total.");
            }

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension) ||
                !FormatsByExtension.TryGetValue(extension, out var expectedFormat))
            {
                return Failure(
                    PostErrorCodes.AttachmentExtensionUnsupported,
                    "Post images must be JPEG, PNG, or WebP files.");
            }

            if (!string.Equals(
                file.ContentType,
                expectedFormat.ContentType,
                StringComparison.OrdinalIgnoreCase))
            {
                return Failure(
                    PostErrorCodes.AttachmentContentTypeUnsupported,
                    "Post image content type does not match its extension.");
            }

            var detectedSignature = await ReadSignatureAsync(file, cancellationToken);
            if (detectedSignature != expectedFormat.Signature)
            {
                return Failure(
                    PostErrorCodes.AttachmentSignatureInvalid,
                    "Post image content does not match its extension and content type.");
            }

            validated.Add(new ValidatedPostAttachment(
                file,
                expectedFormat.CanonicalExtension,
                expectedFormat.ContentType,
                file.Length,
                index));
        }

        return ApplicationResult<IReadOnlyList<ValidatedPostAttachment>>.Success(validated);
    }

    private static async Task<ImageSignature> ReadSignatureAsync(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var header = new byte[12];
        await using var stream = file.OpenReadStream();
        var bytesRead = await stream.ReadAtLeastAsync(
            header,
            header.Length,
            throwOnEndOfStream: false,
            cancellationToken);

        if (bytesRead >= 8 && header.AsSpan(0, 8).SequenceEqual(
            new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
        {
            return ImageSignature.Png;
        }

        if (bytesRead >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return ImageSignature.Jpeg;
        }

        if (bytesRead >= 12 &&
            header.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
            header.AsSpan(8, 4).SequenceEqual("WEBP"u8))
        {
            return ImageSignature.WebP;
        }

        return ImageSignature.Unknown;
    }

    private static ApplicationResult<IReadOnlyList<ValidatedPostAttachment>> Failure(
        string code,
        string message) =>
        ApplicationResult<IReadOnlyList<ValidatedPostAttachment>>.Failure(
            ApplicationErrorKind.Validation,
            code,
            message);

    private sealed record AttachmentFormat(
        string CanonicalExtension,
        string ContentType,
        ImageSignature Signature);

    private enum ImageSignature
    {
        Unknown,
        Jpeg,
        Png,
        WebP
    }
}
