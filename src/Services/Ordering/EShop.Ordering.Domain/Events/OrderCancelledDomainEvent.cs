using EShop.Shared.Domain;

namespace EShop.Ordering.Domain.Events;

public class OrderCancelledDomainEvent : IDomainEvent
{
    public Guid OrderId { get; }
    public Guid UserId { get; }
    public string Reason { get; }
    public DateTime OccurredAt { get; }

    public OrderCancelledDomainEvent(Guid orderId, Guid userId, string reason)
    {
        OrderId = orderId;
        UserId = userId;
        Reason = reason;
        OccurredAt = DateTime.UtcNow;
    }
}
