namespace InteresMe.API.BuildingBlocks.Email;

public sealed class EmailTemplateLoader(IWebHostEnvironment webHostEnvironment)
{
    private const string TemplatesDirectory = "Templates";

    public async Task<string> LoadAsync(
        string templateName,
        CancellationToken cancellationToken = default)
    {
        var templatePath = Path.Combine(
            webHostEnvironment.ContentRootPath,
            "BuildingBlocks",
            "Email",
            TemplatesDirectory,
            templateName);

        return await File.ReadAllTextAsync(templatePath, cancellationToken);
    }
}
