using EShop.Ordering.Application.Interfaces;
using EShop.Ordering.Application.Queries.GetUserOrders;
using EShop.Ordering.Domain.Entities;
using EShop.Ordering.Domain.Enums;
using EShop.Ordering.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace EShop.Ordering.UnitTests.Application;

public class GetUserOrdersQueryHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly GetUserOrdersQueryHandler _handler;

    public GetUserOrdersQueryHandlerTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _handler = new GetUserOrdersQueryHandler(_orderRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithOrders_ReturnsPaginatedResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var address = new Address("123 Main St", "Athens", "10431", "Greece");
        var orders = new List<Order>
        {
            Order.Create(userId, address, new List<(Guid, string, decimal, int)>
            {
                (Guid.NewGuid(), "Product 1", 10.00m, 1)
            }),
            Order.Create(userId, address, new List<(Guid, string, decimal, int)>
            {
                (Guid.NewGuid(), "Product 2", 20.00m, 3)
            })
        };

        var query = new GetUserOrdersQuery(userId, null, 1, 10);

        _orderRepositoryMock
            .Setup(x => x.GetUserOrdersAsync(userId, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((orders, 2));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task Handle_NoOrders_ReturnsEmptyResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserOrdersQuery(userId, null, 1, 10);

        _orderRepositoryMock
            .Setup(x => x.GetUserOrdersAsync(userId, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Order>(), 0));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }
}
