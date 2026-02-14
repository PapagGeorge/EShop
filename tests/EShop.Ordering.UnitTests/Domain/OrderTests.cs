using EShop.Ordering.Domain.Entities;
using EShop.Ordering.Domain.Enums;
using EShop.Ordering.Domain.Events;
using EShop.Ordering.Domain.Exceptions;
using EShop.Ordering.Domain.ValueObjects;
using FluentAssertions;

namespace EShop.Ordering.UnitTests.Domain;

public class OrderTests
{
    private readonly Address _validAddress = new("123 Main St", "Athens", "10431", "Greece");
    private readonly Guid _userId = Guid.NewGuid();

    private List<(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity)> CreateItems(int count = 1)
    {
        return Enumerable.Range(1, count)
            .Select(i => (Guid.NewGuid(), $"Product {i}", 10m * i, i))
            .ToList();
    }

    [Fact]
    public void Create_HappyPath_ShouldCreateOrderWithCorrectState()
    {
        var items = CreateItems(2);

        var order = Order.Create(_userId, _validAddress, items);

        order.UserId.Should().Be(_userId);
        order.Status.Should().Be(OrderStatus.Pending);
        order.ShippingAddress.Should().Be(_validAddress);
        order.Items.Should().HaveCount(2);
        // Item 1: 10*1=10, Item 2: 20*2=40 => Total=50
        order.TotalAmount.Should().Be(50m);
    }

    [Fact]
    public void Create_WithNoItems_ShouldThrowOrderDomainException()
    {
        var act = () => Order.Create(_userId, _validAddress, new List<(Guid, string, decimal, int)>());

        act.Should().Throw<OrderDomainException>().WithMessage("*at least one item*");
    }

    [Fact]
    public void Create_ShouldRaiseOrderCreatedDomainEvent()
    {
        var items = CreateItems(2);

        var order = Order.Create(_userId, _validAddress, items);

        order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderCreatedDomainEvent>()
            .Which.OrderId.Should().Be(order.Id);
    }

    [Fact]
    public void AddItem_ShouldRecalculateTotal()
    {
        var items = CreateItems(1); // 10*1=10
        var order = Order.Create(_userId, _validAddress, items);
        order.ClearDomainEvents();

        order.AddItem(Guid.NewGuid(), "New Product", 15m, 2); // 15*2=30

        order.Items.Should().HaveCount(2);
        order.TotalAmount.Should().Be(40m); // 10+30
    }

    [Fact]
    public void Cancel_FromPending_ShouldSucceedAndRaiseEvent()
    {
        var order = Order.Create(_userId, _validAddress, CreateItems());
        order.ClearDomainEvents();

        order.Cancel("Changed my mind");

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderCancelledDomainEvent>();
    }

    [Fact]
    public void Cancel_FromConfirmed_ShouldThrowInvalidOrderStatusTransitionException()
    {
        var order = Order.Create(_userId, _validAddress, CreateItems());
        order.Confirm();

        var act = () => order.Cancel();

        act.Should().Throw<InvalidOrderStatusTransitionException>()
            .WithMessage("*Confirmed*Cancelled*");
    }

    [Fact]
    public void Confirm_FromPending_ShouldSucceed()
    {
        var order = Order.Create(_userId, _validAddress, CreateItems());

        order.Confirm();

        order.Status.Should().Be(OrderStatus.Confirmed);
    }

    [Fact]
    public void Ship_FromConfirmed_ShouldSucceed()
    {
        var order = Order.Create(_userId, _validAddress, CreateItems());
        order.Confirm();

        order.Ship();

        order.Status.Should().Be(OrderStatus.Shipped);
    }

    [Fact]
    public void Ship_FromPending_ShouldThrow()
    {
        var order = Order.Create(_userId, _validAddress, CreateItems());

        var act = () => order.Ship();

        act.Should().Throw<InvalidOrderStatusTransitionException>();
    }

    [Fact]
    public void Deliver_FromShipped_ShouldSucceed()
    {
        var order = Order.Create(_userId, _validAddress, CreateItems());
        order.Confirm();
        order.Ship();

        order.Deliver();

        order.Status.Should().Be(OrderStatus.Delivered);
    }

    [Fact]
    public void FullLifecycle_Create_Confirm_Ship_Deliver()
    {
        var order = Order.Create(_userId, _validAddress, CreateItems(3));

        order.Status.Should().Be(OrderStatus.Pending);

        order.Confirm();
        order.Status.Should().Be(OrderStatus.Confirmed);

        order.Ship();
        order.Status.Should().Be(OrderStatus.Shipped);

        order.Deliver();
        order.Status.Should().Be(OrderStatus.Delivered);
    }
}
