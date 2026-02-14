using System.IdentityModel.Tokens.Jwt;
using EShop.Identity.Domain.Entities;
using EShop.Identity.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace EShop.Identity.UnitTests.Services;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _service;

    public JwtTokenServiceTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
                ["Jwt:Issuer"] = "EShop.Identity",
                ["Jwt:Audience"] = "EShop",
                ["Jwt:ExpirationInHours"] = "1"
            })
            .Build();

        _service = new JwtTokenService(configuration);
    }

    [Fact]
    public void GenerateToken_ShouldReturnValidToken()
    {
        var user = User.Create("test@test.com", "hashedpw", "Test User");

        var (token, expiresAt) = _service.GenerateToken(user);

        token.Should().NotBeNullOrEmpty();
        expiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void GenerateToken_ShouldContainCorrectClaims()
    {
        var user = User.Create("test@test.com", "hashedpw", "Test User");

        var (token, _) = _service.GenerateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "test@test.com");
        jwt.Issuer.Should().Be("EShop.Identity");
    }

    [Fact]
    public void GenerateToken_ShouldExpireInOneHour()
    {
        var user = User.Create("test@test.com", "hashedpw", "Test User");

        var (_, expiresAt) = _service.GenerateToken(user);

        expiresAt.Should().BeCloseTo(DateTime.UtcNow.AddHours(1), TimeSpan.FromSeconds(5));
    }
}
