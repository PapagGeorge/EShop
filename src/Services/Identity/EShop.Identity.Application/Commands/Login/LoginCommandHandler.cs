using EShop.Identity.Application.DTOs;
using EShop.Identity.Application.Interfaces;
using EShop.Shared.Exceptions;
using MediatR;

namespace EShop.Identity.Application.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new BusinessRuleException("Invalid email or password.");

        var (token, expiresAt) = _jwtTokenService.GenerateToken(user);

        var userDto = new UserDto(user.Id, user.Email, user.FullName, user.Role);

        return new AuthResponseDto(token, expiresAt, userDto);
    }
}
