using EShop.Identity.Application.Commands.Login;
using EShop.Identity.Application.Interfaces;
using EShop.Identity.Domain.Entities;
using EShop.Shared.Exceptions;
using FluentAssertions;
using Moq;

namespace EShop.Identity.UnitTests.Handlers;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IJwtTokenService> _jwtServiceMock = new();
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _handler = new LoginCommandHandler(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _jwtServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ShouldReturnToken()
    {
        var user = User.Create("test@test.com", "hashed", "Test User");
        var command = new LoginCommand("test@test.com", "Password1");
        var expiresAt = DateTime.UtcNow.AddHours(1);

        _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email, default)).ReturnsAsync(user);
        _passwordHasherMock.Setup(h => h.Verify(command.Password, user.PasswordHash)).Returns(true);
        _jwtServiceMock.Setup(j => j.GenerateToken(user)).Returns(("jwt-token", expiresAt));

        var result = await _handler.Handle(command, default);

        result.Token.Should().Be("jwt-token");
        result.User.Email.Should().Be("test@test.com");
        result.ExpiresAt.Should().Be(expiresAt);
    }

    [Fact]
    public async Task Handle_WithInvalidEmail_ShouldThrow()
    {
        var command = new LoginCommand("wrong@test.com", "Password1");
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email, default)).ReturnsAsync((User?)null);

        var act = () => _handler.Handle(command, default);

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("Invalid email or password.");
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ShouldThrow()
    {
        var user = User.Create("test@test.com", "hashed", "Test User");
        var command = new LoginCommand("test@test.com", "WrongPassword");

        _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email, default)).ReturnsAsync(user);
        _passwordHasherMock.Setup(h => h.Verify(command.Password, user.PasswordHash)).Returns(false);

        var act = () => _handler.Handle(command, default);

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("Invalid email or password.");
    }
}
