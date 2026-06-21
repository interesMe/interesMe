using System.Text.Json;
using InteresMe.API.Modules.Posts.Feed.Contracts.Requests;

namespace InteresMe.API.Modules.Posts.Feed.Services;

public sealed class PostCreateRequestReader : IPostCreateRequestReader
{
    private static readonly HashSet<string> AllowedFormFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "body",
        "initiativeId"
    };

    public async Task<PostCreateRequestBinding> ReadAsync(
        HttpRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.HasJsonContentType())
        {
            try
            {
                var value = await request.ReadFromJsonAsync<CreatePostRequest>(
                    cancellationToken: cancellationToken);

                return new PostCreateRequestBinding(value, []);
            }
            catch (JsonException)
            {
                return Invalid("Post request body is invalid.");
            }
            catch (BadHttpRequestException)
            {
                return Invalid("Post request body is invalid.");
            }
        }

        if (!request.HasFormContentType)
        {
            return Invalid("Content type must be application/json or multipart/form-data.");
        }

        try
        {
            var form = await request.ReadFormAsync(cancellationToken);
            if (form.Keys.Any(key => !AllowedFormFields.Contains(key)) ||
                form.Files.Any(file => !string.Equals(
                    file.Name,
                    "attachments",
                    StringComparison.OrdinalIgnoreCase)))
            {
                return Invalid("Post request contains unsupported fields.");
            }

            Guid? initiativeId = null;
            var initiativeIdValue = form["initiativeId"].ToString();
            if (!string.IsNullOrWhiteSpace(initiativeIdValue))
            {
                if (!Guid.TryParse(initiativeIdValue, out var parsedInitiativeId))
                {
                    return Invalid("Initiative id is invalid.");
                }

                initiativeId = parsedInitiativeId;
            }

            return new PostCreateRequestBinding(
                new CreatePostRequest
                {
                    Body = form["body"].ToString(),
                    InitiativeId = initiativeId
                },
                form.Files.GetFiles("attachments"));
        }
        catch (InvalidDataException)
        {
            return new PostCreateRequestBinding(
                null,
                [],
                PostErrorCodes.AttachmentTotalSizeExceeded,
                "Post attachments exceed the allowed total size.");
        }
        catch (BadHttpRequestException)
        {
            return Invalid("Post multipart request is invalid.");
        }
    }

    private static PostCreateRequestBinding Invalid(string message) =>
        new(null, [], PostErrorCodes.RequestInvalid, message);
}
