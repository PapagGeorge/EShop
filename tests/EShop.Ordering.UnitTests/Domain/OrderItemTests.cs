using EShop.Ordering.Domain.Entities;
using EShop.Ordering.Domain.Exceptions;
using EShop.Ordering.Domain.ValueObjects;
using FluentAssertions;

namespace EShop.Ordering.UnitTests.Domain;

public class OrderItemTests
{
    private static Order CreateTestOrder()
    {
        var address = new Address("123 Main St", "Athens", "10431", "Greece");
        return Order.Create(Guid.NewGuid(), address, new List<(Guid, string, decimal, int)>
        {
            (Guid.NewGuid(), "Test Product", 10m, 1)
        });
    }

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var order = CreateTestOrder();
        var item = order.Items.First();

        item.ProductName.Should().Be("Test Product");
        item.UnitPrice.Should().Be(10m);
        item.Quantity.Should().Be(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_WithInvalidQuantity_ShouldThrow(int quantity)
    {
        var address = new Address("123 Main St", "Athens", "10431", "Greece");

        var act = () => Order.Create(Guid.NewGuid(), address, new List<(Guid, string, decimal, int)>
        {
            (Guid.NewGuid(), "Test Product", 10m, quantity)
        });

        act.Should().Throw<OrderDomainException>().WithMessage("*Quantity*");
    }

    [Fact]
    public void Create_WithNegativeUnitPrice_ShouldThrow()
    {
        var address = new Address("123 Main St", "Athens", "10431", "Greece");

        var act = () => Order.Create(Guid.NewGuid(), address, new List<(Guid, string, decimal, int)>
        {
            (Guid.NewGuid(), "Test Product", -5m, 1)
        });

        act.Should().Throw<OrderDomainException>().WithMessage("*UnitPrice*");
    }

    [Fact]
    public void Create_WithEmptyProductId_ShouldThrow()
    {
        var address = new Address("123 Main St", "Athens", "10431", "Greece");

        var act = () => Order.Create(Guid.NewGuid(), address, new List<(Guid, string, decimal, int)>
        {
            (Guid.Empty, "Test Product", 10m, 1)
        });

        act.Should().Throw<OrderDomainException>().WithMessage("*ProductId*");
    }

    [Fact]
    public void TotalPrice_ShouldBeComputedCorrectly()
    {
        var address = new Address("123 Main St", "Athens", "10431", "Greece");
        var order = Order.Create(Guid.NewGuid(), address, new List<(Guid, string, decimal, int)>
        {
            (Guid.NewGuid(), "Test Product", 25.50m, 3)
        });

        var item = order.Items.First();
        item.TotalPrice.Should().Be(76.50m);
    }
}
