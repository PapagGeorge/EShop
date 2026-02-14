using EShop.Identity.Infrastructure.Services;
using FluentAssertions;

namespace EShop.Identity.UnitTests.Services;

public class BcryptPasswordHasherTests
{
    private readonly BcryptPasswordHasher _hasher = new();

    [Fact]
    public void Hash_ShouldReturnHashedPassword()
    {
        var hash = _hasher.Hash("MyPassword123");

        hash.Should().NotBeNullOrEmpty();
        hash.Should().NotBe("MyPassword123");
    }

    [Fact]
    public void Verify_WithCorrectPassword_ShouldReturnTrue()
    {
        string password = "MyPassword123";
        string hash = _hasher.Hash(password);

        _hasher.Verify(password, hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_WithWrongPassword_ShouldReturnFalse()
    {
        string hash = _hasher.Hash("MyPassword123");

        _hasher.Verify("WrongPassword", hash).Should().BeFalse();
    }
}
