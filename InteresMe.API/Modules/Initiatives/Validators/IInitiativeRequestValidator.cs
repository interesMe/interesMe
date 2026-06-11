namespace InteresMe.API.Modules.Initiatives.Validators;

public interface IInitiativeRequestValidator
{
    Task<string?> ValidateAsync(
        NormalizedInitiativeRequest request,
        CancellationToken cancellationToken = default);
}
