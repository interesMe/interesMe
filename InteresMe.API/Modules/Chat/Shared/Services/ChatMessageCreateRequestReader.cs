using System.Text.Json;
using InteresMe.API.Modules.Chat.Shared;
using InteresMe.API.Modules.Chat.Shared.DTOs;

namespace InteresMe.API.Modules.Chat.Shared.Services;

public sealed class ChatMessageCreateRequestReader : IChatMessageCreateRequestReader
{
    private static readonly HashSet<string> AllowedFormFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "body",
        "text"
    };

    public async Task<ChatMessageCreateRequestBinding> ReadAsync(
        HttpRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.HasJsonContentType())
        {
            try
            {
                var value = await request.ReadFromJsonAsync<SendMessageRequest>(
                    cancellationToken: cancellationToken);

                return new ChatMessageCreateRequestBinding(value?.Text, []);
            }
            catch (JsonException)
            {
                return Invalid("Message request body is invalid.");
            }
            catch (BadHttpRequestException)
            {
                return Invalid("Message request body is invalid.");
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
                form.Files.Any(file => !string.Equals(file.Name, "attachments", StringComparison.OrdinalIgnoreCase)))
            {
                return Invalid("Message request contains unsupported fields.");
            }

            var body = form["body"].ToString();
            var text = form["text"].ToString();

            return new ChatMessageCreateRequestBinding(
                string.IsNullOrWhiteSpace(body) ? text : body,
                form.Files.GetFiles("attachments"));
        }
        catch (InvalidDataException)
        {
            return new ChatMessageCreateRequestBinding(
                null,
                [],
                ChatErrorCodes.AttachmentTotalTooLarge,
                "Message attachments exceed the allowed total size.");
        }
        catch (BadHttpRequestException)
        {
            return Invalid("Message multipart request is invalid.");
        }
    }

    private static ChatMessageCreateRequestBinding Invalid(string message) =>
        new(null, [], ChatErrorCodes.MessageValidationFailed, message);
}
