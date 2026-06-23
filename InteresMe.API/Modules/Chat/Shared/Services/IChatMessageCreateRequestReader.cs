namespace InteresMe.API.Modules.Chat.Shared.Services;

public interface IChatMessageCreateRequestReader
{
    Task<ChatMessageCreateRequestBinding> ReadAsync(
        HttpRequest request,
        CancellationToken cancellationToken = default);
}
