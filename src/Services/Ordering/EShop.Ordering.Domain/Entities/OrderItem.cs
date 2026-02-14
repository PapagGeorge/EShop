using EShop.Ordering.Domain.Exceptions;
using EShop.Shared.Domain;

namespace EShop.Ordering.Domain.Entities;

public class OrderItem : BaseEntity
{
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = default!;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }

    public decimal TotalPrice => UnitPrice * Quantity;

    private OrderItem() { } // EF Core

    internal static OrderItem Create(Guid productId, string productName, decimal unitPrice, int quantity)
    {
        if (productId == Guid.Empty)
            throw new OrderDomainException("ProductId cannot be empty.");

        if (string.IsNullOrWhiteSpace(productName))
            throw new OrderDomainException("ProductName cannot be empty.");

        if (unitPrice < 0)
            throw new OrderDomainException("UnitPrice cannot be negative.");

        if (quantity <= 0)
            throw new OrderDomainException("Quantity must be greater than zero.");

        return new OrderItem
        {
            ProductId = productId,
            ProductName = productName,
            UnitPrice = unitPrice,
            Quantity = quantity
        };
    }
}
