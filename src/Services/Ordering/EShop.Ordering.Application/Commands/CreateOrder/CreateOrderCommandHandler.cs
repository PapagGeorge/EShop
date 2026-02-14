using EShop.Ordering.Application.DTOs;
using EShop.Ordering.Application.Interfaces;
using EShop.Ordering.Domain.Entities;
using EShop.Ordering.Domain.ValueObjects;
using EShop.Shared.Exceptions;
using MediatR;

namespace EShop.Ordering.Application.Commands.CreateOrder;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICatalogService _catalogService;
    private readonly IEventBus _eventBus;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        ICatalogService catalogService,
        IEventBus eventBus)
    {
        _orderRepository = orderRepository;
        _catalogService = catalogService;
        _eventBus = eventBus;
    }

    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var productIds = request.Items.Select(i => i.ProductId).ToList();
        var products = await _catalogService.GetProductsByIdsAsync(productIds, cancellationToken);

        var missingIds = productIds.Except(products.Select(p => p.Id)).ToList();
        if (missingIds.Count != 0)
            throw new BusinessRuleException($"Products not found: {string.Join(", ", missingIds)}");

        var address = new Address(
            request.ShippingAddress.Street,
            request.ShippingAddress.City,
            request.ShippingAddress.ZipCode,
            request.ShippingAddress.Country);

        var items = request.Items.Select(item =>
        {
            var product = products.First(p => p.Id == item.ProductId);
            return (item.ProductId, product.Name, product.Price, item.Quantity);
        }).ToList();

        var order = Order.Create(request.UserId, address, items);

        await _orderRepository.AddAsync(order, cancellationToken);

        foreach (var domainEvent in order.DomainEvents)
        {
            await _eventBus.PublishAsync(domainEvent, cancellationToken);
        }
        order.ClearDomainEvents();

        return MapToDto(order);
    }

    private static OrderDto MapToDto(Order order)
    {
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
