using EShop.Ordering.Application.DTOs;
using MediatR;

namespace EShop.Ordering.Application.Commands.CreateOrder;

public record CreateOrderCommand(
    Guid UserId,
    AddressDto ShippingAddress,
    List<OrderItemRequest> Items) : IRequest<OrderDto>;

public record OrderItemRequest(Guid ProductId, int Quantity);
