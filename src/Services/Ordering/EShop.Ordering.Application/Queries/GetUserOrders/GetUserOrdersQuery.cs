using EShop.Ordering.Application.DTOs;
using EShop.Ordering.Domain.Enums;
using MediatR;

namespace EShop.Ordering.Application.Queries.GetUserOrders;

public record GetUserOrdersQuery(
    Guid UserId,
    OrderStatus? Status,
    int Page,
    int PageSize) : IRequest<PaginatedResult<OrderSummaryDto>>;
