using EShop.Ordering.Domain.ValueObjects;
using FluentAssertions;

namespace EShop.Ordering.UnitTests.Domain;

public class AddressTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var address = new Address("123 Main St", "Athens", "10431", "Greece");

        address.Street.Should().Be("123 Main St");
        address.City.Should().Be("Athens");
        address.ZipCode.Should().Be("10431");
        address.Country.Should().Be("Greece");
    }

    [Theory]
    [InlineData("", "Athens", "10431", "Greece")]
    [InlineData("123 Main St", "", "10431", "Greece")]
    [InlineData("123 Main St", "Athens", "", "Greece")]
    [InlineData("123 Main St", "Athens", "10431", "")]
    [InlineData(null, "Athens", "10431", "Greece")]
    public void Create_WithEmptyFields_ShouldThrowArgumentException(
        string street, string city, string zipCode, string country)
    {
        var act = () => new Address(street, city, zipCode, country);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equals_SameData_ShouldBeEqual()
    {
        var address1 = new Address("123 Main St", "Athens", "10431", "Greece");
        var address2 = new Address("123 Main St", "Athens", "10431", "Greece");

        address1.Should().Be(address2);
        (address1 == address2).Should().BeTrue();
        address1.GetHashCode().Should().Be(address2.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentData_ShouldNotBeEqual()
    {
        var address1 = new Address("123 Main St", "Athens", "10431", "Greece");
        var address2 = new Address("456 Other St", "Thessaloniki", "54321", "Greece");

        address1.Should().NotBe(address2);
        (address1 != address2).Should().BeTrue();
    }
}
