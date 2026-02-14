using EShop.Ordering.Application.DTOs;
using MediatR;

namespace EShop.Ordering.Application.Commands.CancelOrder;

public record CancelOrderCommand(Guid OrderId, Guid UserId) : IRequest<OrderDto>;
