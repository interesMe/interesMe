namespace InteresMe.API.Modules.Chat.Shared.Services;

public sealed class LocalChatMessageAttachmentStorage(
    IWebHostEnvironment webHostEnvironment,
    ILogger<LocalChatMessageAttachmentStorage> logger) : IChatMessageAttachmentStorage
{
    public async Task<IReadOnlyList<StagedChatMessageAttachment>> StageAsync(
        Guid conversationId,
        Guid messageId,
        IReadOnlyList<ValidatedChatMessageAttachment> attachments,
        CancellationToken cancellationToken = default)
    {
        if (attachments.Count == 0)
        {
            return [];
        }

        var stagingDirectory = Path.Combine(
            webHostEnvironment.ContentRootPath,
            ".upload-staging",
            "chats",
            messageId.ToString("D"));
        Directory.CreateDirectory(stagingDirectory);

        var staged = new List<StagedChatMessageAttachment>(attachments.Count);
        try
        {
            foreach (var attachment in attachments)
            {
                var attachmentId = Guid.NewGuid();
                var fileName = $"{attachmentId:D}{attachment.CanonicalExtension}";
                var storagePath = $"/uploads/chats/{conversationId:D}/{messageId:D}/{fileName}";
                var stagingPath = Path.Combine(stagingDirectory, fileName);
                var finalPath = Path.Combine(
                    GetWebRootPath(),
                    "uploads",
                    "chats",
                    conversationId.ToString("D"),
                    messageId.ToString("D"),
                    fileName);

                var stagedAttachment = new StagedChatMessageAttachment(
                    attachmentId,
                    stagingPath,
                    finalPath,
                    storagePath,
                    attachment.FileName,
                    attachment.ContentType,
                    attachment.SizeBytes,
                    attachment.SortOrder);
                staged.Add(stagedAttachment);

                await using var output = new FileStream(
                    stagingPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    useAsync: true);
                await attachment.File.CopyToAsync(output, cancellationToken);
            }

            return staged;
        }
        catch
        {
            CleanupAll(staged);
            TryDeleteDirectory(stagingDirectory);
            throw;
        }
    }

    public void MoveToFinal(IReadOnlyList<StagedChatMessageAttachment> attachments)
    {
        if (attachments.Count == 0)
        {
            return;
        }

        var finalDirectory = Path.GetDirectoryName(attachments[0].FinalPath)!;
        Directory.CreateDirectory(finalDirectory);

        try
        {
            foreach (var attachment in attachments)
            {
                File.Move(attachment.StagingPath, attachment.FinalPath);
            }

            TryDeleteDirectory(Path.GetDirectoryName(attachments[0].StagingPath)!);
        }
        catch
        {
            CleanupAll(attachments);
            throw;
        }
    }

    public void CleanupAll(IReadOnlyList<StagedChatMessageAttachment> attachments)
    {
        foreach (var attachment in attachments)
        {
            TryDeleteFile(attachment.StagingPath);
            TryDeleteFile(attachment.FinalPath);
        }

        if (attachments.Count > 0)
        {
            TryDeleteDirectory(Path.GetDirectoryName(attachments[0].StagingPath)!);
            TryDeleteDirectory(Path.GetDirectoryName(attachments[0].FinalPath)!);
        }
    }

    private string GetWebRootPath() =>
        string.IsNullOrWhiteSpace(webHostEnvironment.WebRootPath)
            ? Path.Combine(webHostEnvironment.ContentRootPath, "wwwroot")
            : webHostEnvironment.WebRootPath;

    private void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to clean up a chat message attachment file.");
        }
    }

    private void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path) && !Directory.EnumerateFileSystemEntries(path).Any())
            {
                Directory.Delete(path);
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to clean up a chat message attachment directory.");
        }
    }
}
