namespace EShop.Identity.Application.DTOs;

public record AuthResponseDto(string Token, DateTime ExpiresAt, UserDto User);
