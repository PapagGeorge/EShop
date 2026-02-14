using EShop.Identity.Application.Commands.Register;
using EShop.Identity.Application.Interfaces;
using EShop.Identity.Domain.Entities;
using EShop.Shared.Exceptions;
using FluentAssertions;
using Moq;

namespace EShop.Identity.UnitTests.Handlers;

public class RegisterCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        _handler = new RegisterCommandHandler(_userRepositoryMock.Object, _passwordHasherMock.Object);
    }

    [Fact]
    public async Task Handle_WithNewEmail_ShouldRegisterUser()
    {
        var command = new RegisterCommand("test@test.com", "Password1", "Test User");
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email, default)).ReturnsAsync((User?)null);
        _passwordHasherMock.Setup(h => h.Hash(command.Password)).Returns("hashed");
        _userRepositoryMock.Setup(r => r.AddAsync(It.IsAny<User>(), default))
            .ReturnsAsync((User u, CancellationToken _) => u);

        var result = await _handler.Handle(command, default);

        result.Email.Should().Be("test@test.com");
        result.FullName.Should().Be("Test User");
        result.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_WithExistingEmail_ShouldThrowBusinessRuleException()
    {
        var command = new RegisterCommand("existing@test.com", "Password1", "Test User");
        var existingUser = User.Create("existing@test.com", "hash", "Existing");
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email, default)).ReturnsAsync(existingUser);

        var act = () => _handler.Handle(command, default);

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("Email is already registered.");
    }
}
