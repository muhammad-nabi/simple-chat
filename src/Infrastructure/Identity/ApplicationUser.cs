using Microsoft.AspNetCore.Identity;
using SimpleChat.Domain.Common.Enums;

namespace SimpleChat.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Member;
    public bool IsActive { get; set; } = true;
}
