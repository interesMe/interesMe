using InteresMe.API.Modules.Profile.DTOs;

namespace InteresMe.API.Modules.Profile.Services;

public sealed class ProfileRequestReader : IProfileRequestReader
{
    public async Task<ProfileRequestBinding<TRequest>> ReadAsync<TRequest>(
        HttpRequest httpRequest,
        CancellationToken cancellationToken)
        where TRequest : class, new()
    {
        if (!httpRequest.HasFormContentType)
        {
            var jsonRequest = await httpRequest.ReadFromJsonAsync<TRequest>(
                cancellationToken: cancellationToken);

            return jsonRequest is null
                ? new ProfileRequestBinding<TRequest>(null, null, "Profile request body is required.")
                : new ProfileRequestBinding<TRequest>(jsonRequest, null, null);
        }

        var form = await httpRequest.ReadFormAsync(cancellationToken);
        var birthDateValue = form["birthDate"].ToString();
        DateOnly? birthDate = null;

        if (!string.IsNullOrWhiteSpace(birthDateValue))
        {
            if (!DateOnly.TryParse(birthDateValue, out var parsedBirthDate))
            {
                return new ProfileRequestBinding<TRequest>(
                    null,
                    null,
                    "Birth date must be a valid date.");
            }

            birthDate = parsedBirthDate;
        }

        var request = new TRequest();

        switch (request)
        {
            case CreateProfileRequest createRequest:
                createRequest.DisplayName = form["displayName"].ToString();
                createRequest.Headline = NormalizeOptional(form["headline"].ToString());
                createRequest.Bio = NormalizeOptional(form["bio"].ToString());
                createRequest.City = NormalizeOptional(form["city"].ToString());
                createRequest.BirthDate = birthDate;
                break;

            case UpdateProfileRequest updateRequest:
                updateRequest.DisplayName = form["displayName"].ToString();
                updateRequest.Headline = NormalizeOptional(form["headline"].ToString());
                updateRequest.Bio = NormalizeOptional(form["bio"].ToString());
                updateRequest.City = NormalizeOptional(form["city"].ToString());
                updateRequest.BirthDate = birthDate;
                break;
        }

        return new ProfileRequestBinding<TRequest>(
            request,
            form.Files.GetFile("avatarFile"),
            null);
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
