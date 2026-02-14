using EShop.Identity.Application.DTOs;
using EShop.Identity.Application.Interfaces;
using EShop.Identity.Domain.Entities;
using EShop.Shared.Exceptions;
using MediatR;

namespace EShop.Identity.Application.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResponseDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<RegisterResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (existingUser is not null)
            throw new BusinessRuleException("Email is already registered.");

        string passwordHash = _passwordHasher.Hash(request.Password);
        var user = User.Create(request.Email, passwordHash, request.FullName);

        await _userRepository.AddAsync(user, cancellationToken);

        return new RegisterResponseDto(user.Id, user.Email, user.FullName);
    }
}
