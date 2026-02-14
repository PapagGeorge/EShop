using EShop.Ordering.Application.DTOs;
using EShop.Ordering.Application.Interfaces;
using EShop.Ordering.Domain.Entities;
using EShop.Shared.Exceptions;
using MediatR;

namespace EShop.Ordering.Application.Commands.CancelOrder;

public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IEventBus _eventBus;

    public CancelOrderCommandHandler(IOrderRepository orderRepository, IEventBus eventBus)
    {
        _orderRepository = orderRepository;
        _eventBus = eventBus;
    }

    public async Task<OrderDto> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        if (order.UserId != request.UserId)
            throw new BusinessRuleException("You can only cancel your own orders.");

        order.Cancel();

        await _orderRepository.UpdateAsync(order, cancellationToken);

        foreach (var domainEvent in order.DomainEvents)
        {
            await _eventBus.PublishAsync(domainEvent, cancellationToken);
        }
        order.ClearDomainEvents();

        return new OrderDto(
            order.Id,
            order.UserId,
            order.Status.ToString(),
            new AddressDto(
                order.ShippingAddress.Street,
                order.ShippingAddress.City,
                order.ShippingAddress.ZipCode,
                order.ShippingAddress.Country),
            order.Items.Select(i => new OrderItemDto(
                i.ProductId,
                i.ProductName,
                i.UnitPrice,
                i.Quantity,
                i.TotalPrice)).ToList(),
            order.TotalAmount,
            order.CreatedAt,
            order.UpdatedAt);
    }
}
