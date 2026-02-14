using EShop.Shared.Exceptions;

namespace EShop.Ordering.Domain.Exceptions;

public class OrderDomainException : BusinessRuleException
{
    public OrderDomainException(string message)
        : base(message)
    {
    }
}
