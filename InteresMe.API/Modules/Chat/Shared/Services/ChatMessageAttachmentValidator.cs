using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Modules.Chat.Shared;

namespace InteresMe.API.Modules.Chat.Shared.Services;

public sealed class ChatMessageAttachmentValidator : IChatMessageAttachmentValidator
{
    private static readonly Dictionary<string, AttachmentFormat> FormatsByExtension =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = new(".jpg", ["image/jpeg"], AttachmentSignature.Jpeg),
            [".jpeg"] = new(".jpg", ["image/jpeg"], AttachmentSignature.Jpeg),
            [".png"] = new(".png", ["image/png"], AttachmentSignature.Png),
            [".webp"] = new(".webp", ["image/webp"], AttachmentSignature.WebP),
            [".pdf"] = new(".pdf", ["application/pdf"], AttachmentSignature.Pdf),
            [".docx"] = new(
                ".docx",
                ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"],
                AttachmentSignature.Zip),
            [".xlsx"] = new(
                ".xlsx",
                ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"],
                AttachmentSignature.Zip),
            [".txt"] = new(".txt", ["text/plain"], AttachmentSignature.Text),
            [".zip"] = new(".zip", ["application/zip", "application/x-zip-compressed"], AttachmentSignature.Zip)
        };

    public async Task<ApplicationResult<IReadOnlyList<ValidatedChatMessageAttachment>>> ValidateAsync(
        IReadOnlyList<IFormFile> files,
        CancellationToken cancellationToken = default)
    {
        if (files.Count > ChatMessageRules.MaxAttachmentCount)
        {
                return Failure(
                    ChatErrorCodes.AttachmentTooMany,
                    $"A message can contain at most {ChatMessageRules.MaxAttachmentCount} files.");
        }

        long totalSize = 0;
        var validated = new List<ValidatedChatMessageAttachment>(files.Count);

        for (var index = 0; index < files.Count; index++)
        {
            var file = files[index];
            if (file.Length <= 0)
            {
                return Failure(ChatErrorCodes.AttachmentEmpty, "Message attachment cannot be empty.");
            }

            if (file.Length > ChatMessageRules.MaxAttachmentSizeBytes)
            {
                return Failure(ChatErrorCodes.AttachmentTooLarge, "Each message attachment must be 25 MiB or smaller.");
            }

            totalSize += file.Length;
            if (totalSize > ChatMessageRules.MaxTotalAttachmentSizeBytes)
            {
                return Failure(ChatErrorCodes.AttachmentTotalTooLarge, "Message attachments must be 50 MiB or smaller in total.");
            }

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension) ||
                !FormatsByExtension.TryGetValue(extension, out var expectedFormat))
            {
                return Failure(ChatErrorCodes.AttachmentInvalidType, "Message attachments must be JPEG, PNG, WebP, PDF, DOCX, XLSX, TXT, or ZIP files.");
            }

            if (!expectedFormat.ContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            {
                return Failure(ChatErrorCodes.AttachmentInvalidType, "Message attachment content type does not match its extension.");
            }

            var detectedSignature = await ReadSignatureAsync(file, cancellationToken);
            if (detectedSignature != expectedFormat.Signature)
            {
                return Failure(ChatErrorCodes.AttachmentInvalidType, "Message attachment content does not match its extension and content type.");
            }

            validated.Add(new ValidatedChatMessageAttachment(
                file,
                expectedFormat.CanonicalExtension,
                SanitizeFileName(file.FileName, expectedFormat.CanonicalExtension),
                expectedFormat.ContentType,
                file.Length,
                index));
        }

        return ApplicationResult<IReadOnlyList<ValidatedChatMessageAttachment>>.Success(validated);
    }

    private static async Task<AttachmentSignature> ReadSignatureAsync(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var header = new byte[(int)Math.Min(file.Length, 512)];
        await using var stream = file.OpenReadStream();
        var bytesRead = await stream.ReadAtLeastAsync(
            header,
            header.Length,
            throwOnEndOfStream: false,
            cancellationToken);

        if (bytesRead >= 8 && header.AsSpan(0, 8).SequenceEqual(
            new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
        {
            return AttachmentSignature.Png;
        }

        if (bytesRead >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return AttachmentSignature.Jpeg;
        }

        if (bytesRead >= 12 &&
            header.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
            header.AsSpan(8, 4).SequenceEqual("WEBP"u8))
        {
            return AttachmentSignature.WebP;
        }

        if (bytesRead >= 5 && header.AsSpan(0, 5).SequenceEqual("%PDF-"u8))
        {
            return AttachmentSignature.Pdf;
        }

        if (bytesRead >= 4 &&
            header[0] == 0x50 &&
            header[1] == 0x4B &&
            (header[2] == 0x03 || header[2] == 0x05 || header[2] == 0x07) &&
            (header[3] == 0x04 || header[3] == 0x06 || header[3] == 0x08))
        {
            return AttachmentSignature.Zip;
        }

        if (LooksLikeText(header.AsSpan(0, bytesRead)))
        {
            return AttachmentSignature.Text;
        }

        return AttachmentSignature.Unknown;
    }

    private static bool LooksLikeText(ReadOnlySpan<byte> bytes)
    {
        foreach (var value in bytes)
        {
            if (value == 0)
            {
                return false;
            }

            if (value < 0x09 || (value > 0x0D && value < 0x20))
            {
                return false;
            }
        }

        return true;
    }

    private static string SanitizeFileName(string fileName, string canonicalExtension)
    {
        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName))
        {
            return $"attachment{canonicalExtension}";
        }

        foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
        {
            safeName = safeName.Replace(invalidCharacter, '_');
        }

        if (!safeName.EndsWith(canonicalExtension, StringComparison.OrdinalIgnoreCase))
        {
            safeName = $"{Path.GetFileNameWithoutExtension(safeName)}{canonicalExtension}";
        }

        const int maxLength = 255;
        if (safeName.Length <= maxLength)
        {
            return safeName;
        }

        var nameWithoutExtension = Path.GetFileNameWithoutExtension(safeName);
        var maxNameLength = Math.Max(1, maxLength - canonicalExtension.Length);
        return string.Concat(nameWithoutExtension.AsSpan(0, Math.Min(nameWithoutExtension.Length, maxNameLength)), canonicalExtension);
    }

    private static ApplicationResult<IReadOnlyList<ValidatedChatMessageAttachment>> Failure(
        string code,
        string message) =>
        ApplicationResult<IReadOnlyList<ValidatedChatMessageAttachment>>.Failure(
            ApplicationErrorKind.Validation,
            code,
            message);

    private sealed record AttachmentFormat(
        string CanonicalExtension,
        IReadOnlyList<string> ContentTypes,
        AttachmentSignature Signature)
    {
        public string ContentType => ContentTypes.First();
    }

    private enum AttachmentSignature
    {
        Unknown,
        Jpeg,
        Png,
        WebP,
        Pdf,
        Zip,
        Text
    }
}
