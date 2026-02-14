using EShop.Identity.Application.Commands.Register;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace EShop.Identity.UnitTests.Validators;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var command = new RegisterCommand("test@test.com", "Password1", "Test User");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void InvalidEmail_ShouldFail(string email)
    {
        var command = new RegisterCommand(email, "Password1", "Test User");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("nouppercase1")]
    [InlineData("NoNumber")]
    public void InvalidPassword_ShouldFail(string password)
    {
        var command = new RegisterCommand("test@test.com", password, "Test User");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void EmptyFullName_ShouldFail()
    {
        var command = new RegisterCommand("test@test.com", "Password1", "");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.FullName);
    }
}
