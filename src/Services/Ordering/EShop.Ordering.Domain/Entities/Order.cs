using EShop.Ordering.Domain.Enums;
using EShop.Ordering.Domain.Events;
using EShop.Ordering.Domain.Exceptions;
using EShop.Ordering.Domain.ValueObjects;
using EShop.Shared.Domain;

namespace EShop.Ordering.Domain.Entities;

public class Order : BaseEntity
{
    public Guid UserId { get; private set; }
    public OrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public Address ShippingAddress { get; private set; } = default!;
    public DateTime? UpdatedAt { get; private set; }

    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order() { } // EF Core

    public static Order Create(Guid userId, Address address, List<(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity)> items)
    {
        if (userId == Guid.Empty)
            throw new OrderDomainException("UserId cannot be empty.");

        if (items is null || items.Count == 0)
            throw new OrderDomainException("Order must contain at least one item.");

        var order = new Order
        {
            UserId = userId,
            ShippingAddress = address ?? throw new OrderDomainException("Shipping address is required."),
            Status = OrderStatus.Pending
        };

        foreach (var item in items)
        {
            order.AddItem(item.ProductId, item.ProductName, item.UnitPrice, item.Quantity);
        }

        order.AddDomainEvent(new OrderCreatedDomainEvent(
            order.Id,
            order.UserId,
            order.TotalAmount,
            order._items.Count));

        return order;
    }

    public void AddItem(Guid productId, string productName, decimal unitPrice, int quantity)
    {
        var item = OrderItem.Create(productId, productName, unitPrice, quantity);
        _items.Add(item);
        RecalculateTotalAmount();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel(string reason = "Cancelled by user")
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOrderStatusTransitionException(Status, OrderStatus.Cancelled);

        Status = OrderStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new OrderCancelledDomainEvent(Id, UserId, reason));
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOrderStatusTransitionException(Status, OrderStatus.Confirmed);

        Status = OrderStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Ship()
    {
        if (Status != OrderStatus.Confirmed)
            throw new InvalidOrderStatusTransitionException(Status, OrderStatus.Shipped);

        Status = OrderStatus.Shipped;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deliver()
    {
        if (Status != OrderStatus.Shipped)
            throw new InvalidOrderStatusTransitionException(Status, OrderStatus.Delivered);

        Status = OrderStatus.Delivered;
        UpdatedAt = DateTime.UtcNow;
    }

    private void RecalculateTotalAmount()
    {
        TotalAmount = _items.Sum(i => i.TotalPrice);
    }
}
