namespace InteresMe.API.Modules.Profile.Services;

public interface IProfileRequestReader
{
    Task<ProfileRequestBinding<TRequest>> ReadAsync<TRequest>(
        HttpRequest httpRequest,
        CancellationToken cancellationToken)
        where TRequest : class, new();
}
