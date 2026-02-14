using EShop.Ordering.Application.DTOs;
using MediatR;

namespace EShop.Ordering.Application.Queries.GetOrderById;

public record GetOrderByIdQuery(Guid OrderId, Guid UserId) : IRequest<OrderDto>;
