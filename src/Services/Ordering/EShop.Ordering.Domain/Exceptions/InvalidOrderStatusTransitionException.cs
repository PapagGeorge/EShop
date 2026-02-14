using EShop.Ordering.Domain.Enums;

namespace EShop.Ordering.Domain.Exceptions;

public class InvalidOrderStatusTransitionException : OrderDomainException
{
    public InvalidOrderStatusTransitionException(OrderStatus current, OrderStatus target)
        : base($"Cannot transition from {current} to {target}")
    {
    }
}
