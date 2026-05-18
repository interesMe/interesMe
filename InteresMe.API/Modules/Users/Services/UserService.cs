using System.Collections.Concurrent;
using InteresMe.API.Modules.Users.DTOs;
using InteresMe.API.Modules.Users.Models;
using InteresMe.API.Security;

namespace InteresMe.API.Modules.Users.Services;

public class UserService : IUserService
{
    private readonly ConcurrentDictionary<Guid, User> _users = new();

    public Task<IReadOnlyList<UserResponse>> GetAllAsync()
    {
        var users = _users.Values
            .OrderBy(user => user.DisplayName)
            .Select(ToResponse)
            .ToList();

        return Task.FromResult<IReadOnlyList<UserResponse>>(users);
    }

    public Task<UserResponse?> GetByIdAsync(Guid id)
    {
        _users.TryGetValue(id, out var user);
        return Task.FromResult(user is null ? null : ToResponse(user));
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = _users.Values.FirstOrDefault(u =>
            u.Email.Equals(normalizedEmail, StringComparison.Ordinal));

        return Task.FromResult(user);
    }

    public Task<UserResponse?> CreateAsync(CreateUserRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(request.DisplayName) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return Task.FromResult<UserResponse?>(null);
        }

        if (_users.Values.Any(user => user.Email == email))
        {
            return Task.FromResult<UserResponse?>(null);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            PasswordHash = PasswordHasher.Hash(request.Password),
            CreatedAt = DateTime.UtcNow
        };

        _users[user.Id] = user;
        return Task.FromResult<UserResponse?>(ToResponse(user));
    }

    public Task<bool> DeleteAsync(Guid id) => Task.FromResult(_users.TryRemove(id, out _));

    private static UserResponse ToResponse(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        DisplayName = user.DisplayName,
        CreatedAt = user.CreatedAt
    };
}
