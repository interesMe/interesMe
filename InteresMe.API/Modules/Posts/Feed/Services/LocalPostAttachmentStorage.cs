namespace InteresMe.API.Modules.Posts.Feed.Services;

public sealed class LocalPostAttachmentStorage(
    IWebHostEnvironment webHostEnvironment,
    ILogger<LocalPostAttachmentStorage> logger) : IPostAttachmentStorage
{
    public async Task<IReadOnlyList<StagedPostAttachment>> StageAsync(
        Guid authorId,
        Guid postId,
        IReadOnlyList<ValidatedPostAttachment> attachments,
        CancellationToken cancellationToken = default)
    {
        if (attachments.Count == 0)
        {
            return [];
        }

        var stagingDirectory = Path.Combine(
            webHostEnvironment.ContentRootPath,
            ".upload-staging",
            "posts",
            postId.ToString("D"));
        Directory.CreateDirectory(stagingDirectory);

        var staged = new List<StagedPostAttachment>(attachments.Count);
        try
        {
            foreach (var attachment in attachments)
            {
                var attachmentId = Guid.NewGuid();
                var fileName = $"{attachmentId:D}{attachment.CanonicalExtension}";
                var storagePath = $"/uploads/posts/{authorId:D}/{postId:D}/{fileName}";
                var stagingPath = Path.Combine(stagingDirectory, fileName);
                var finalPath = Path.Combine(
                    GetWebRootPath(),
                    "uploads",
                    "posts",
                    authorId.ToString("D"),
                    postId.ToString("D"),
                    fileName);

                var stagedAttachment = new StagedPostAttachment(
                    attachmentId,
                    stagingPath,
                    finalPath,
                    storagePath,
                    attachment.ContentType,
                    attachment.SizeBytes,
                    attachment.SortOrder);
                staged.Add(stagedAttachment);

                await using (var output = new FileStream(
                    stagingPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    useAsync: true))
                {
                    await attachment.File.CopyToAsync(output, cancellationToken);
                }

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

    public void MoveToFinal(IReadOnlyList<StagedPostAttachment> attachments)
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

    public void CleanupAll(IReadOnlyList<StagedPostAttachment> attachments)
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
            logger.LogWarning(exception, "Failed to clean up a post attachment file.");
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
            logger.LogWarning(exception, "Failed to clean up a post attachment directory.");
        }
    }
}
