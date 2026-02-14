namespace EShop.Ordering.Application.DTOs;

public record OrderSummaryDto(
    Guid Id,
    string Status,
    decimal TotalAmount,
    int ItemCount,
    DateTime CreatedAt);
