using InteresMe.API.Modules.Users.DTOs;
using InteresMe.API.Modules.Users.Models;

namespace InteresMe.API.Modules.Users.Services;

public interface IUserService
{
    Task<IReadOnlyList<UserResponse>> GetAllAsync();

    Task<UserResponse?> GetByIdAsync(Guid id);

    Task<User?> GetByEmailAsync(string email);

    Task<UserResponse?> CreateAsync(CreateUserRequest request);

    Task<bool> DeleteAsync(Guid id);
}
