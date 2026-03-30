using SimpleChat.Application.Common.Models;
using SimpleChat.Domain.Common.Enums;

namespace SimpleChat.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<string?> GetUserNameAsync(string userId);

    Task<bool> IsInRoleAsync(string userId, string role);

    Task<bool> AuthorizeAsync(string userId, string policyName);

    Task<(Result Result, string UserId)> CreateUserAsync(string userName, string password);

    Task<(Result Result, string UserId)> CreateUserAsync(
        string email, string displayName, string password, UserRole role);

    Task<bool> EmailExistsAsync(string email);

    Task<bool> AnyUsersExistAsync();

    Task<Result> DeleteUserAsync(string userId);

    Task<(string UserId, string DisplayName, string Email, UserRole Role, bool IsActive)?>
        FindUserByEmailAsync(string email);

    Task<bool> CheckPasswordAsync(string userId, string password);

    Task VerifyDummyPasswordAsync(string password);
}
