namespace InteresMe.API.Modules.Profile.Services;

public sealed record ProfileRequestBinding<TRequest>(
    TRequest? Value,
    IFormFile? AvatarFile,
    string? ErrorMessage);
