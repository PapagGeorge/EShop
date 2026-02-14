using EShop.Ordering.Domain.Entities;
using EShop.Ordering.Domain.Enums;

namespace EShop.Ordering.Application.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Order> AddAsync(Order order, CancellationToken cancellationToken = default);
    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);
    Task<(List<Order> Orders, int TotalCount)> GetUserOrdersAsync(
        Guid userId,
        OrderStatus? status = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);
}
