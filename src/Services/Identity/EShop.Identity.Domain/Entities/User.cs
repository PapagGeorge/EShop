using EShop.Shared.Domain;

namespace EShop.Identity.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string FullName { get; private set; } = default!;
    public string Role { get; private set; } = default!;
    public bool IsActive { get; private set; }

    private User() { }

    public static User Create(string email, string passwordHash, string fullName)
    {
        return new User
        {
            Email = email,
            PasswordHash = passwordHash,
            FullName = fullName,
            Role = "User",
            IsActive = true
        };
    }
}
