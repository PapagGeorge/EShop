using EShop.Identity.Domain.Entities;

namespace EShop.Identity.Application.Interfaces;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAt) GenerateToken(User user);
}
