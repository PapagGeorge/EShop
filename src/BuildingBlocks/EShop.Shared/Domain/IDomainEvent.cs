using MediatR;

namespace EShop.Shared.Domain;

public interface IDomainEvent : INotification
{
    DateTime OccurredAt { get; }
}
