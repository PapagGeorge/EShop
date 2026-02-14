using EShop.Ordering.Application.DTOs;
using EShop.Ordering.Application.Interfaces;
using MediatR;

namespace EShop.Ordering.Application.Queries.GetUserOrders;

public class GetUserOrdersQueryHandler : IRequestHandler<GetUserOrdersQuery, PaginatedResult<OrderSummaryDto>>
{
    private readonly IOrderRepository _orderRepository;

    public GetUserOrdersQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<PaginatedResult<OrderSummaryDto>> Handle(
        GetUserOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var (orders, totalCount) = await _orderRepository.GetUserOrdersAsync(
            request.UserId,
            request.Status,
            request.Page,
            request.PageSize,
            cancellationToken);

        var items = orders.Select(o => new OrderSummaryDto(
            o.Id,
            o.Status.ToString(),
            o.TotalAmount,
            o.Items.Count,
            o.CreatedAt)).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new PaginatedResult<OrderSummaryDto>(
            items,
            request.Page,
            request.PageSize,
            totalCount,
            totalPages);
    }
}
