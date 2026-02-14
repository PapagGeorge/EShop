namespace EShop.Ordering.Application.DTOs;

public record OrderDto(
    Guid Id,
    Guid UserId,
    string Status,
    AddressDto ShippingAddress,
    List<OrderItemDto> Items,
    decimal TotalAmount,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
