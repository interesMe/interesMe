namespace InteresMe.API.BuildingBlocks.Email;

public sealed class EmailTemplateRenderer
{
    public string Render(
        string template,
        IReadOnlyDictionary<string, string> values)
    {
        var rendered = template;

        foreach (var value in values)
        {
            rendered = rendered.Replace(
                $"{{{{{value.Key}}}}}",
                value.Value,
                StringComparison.Ordinal);
        }

        return rendered;
    }
}
