using EShop.Shared.Domain;

namespace EShop.Ordering.Domain.Events;

public class OrderCreatedDomainEvent : IDomainEvent
{
    public Guid OrderId { get; }
    public Guid UserId { get; }
    public decimal TotalAmount { get; }
    public int ItemCount { get; }
    public DateTime OccurredAt { get; }

    public OrderCreatedDomainEvent(Guid orderId, Guid userId, decimal totalAmount, int itemCount)
    {
        OrderId = orderId;
        UserId = userId;
        TotalAmount = totalAmount;
        ItemCount = itemCount;
        OccurredAt = DateTime.UtcNow;
    }
}
