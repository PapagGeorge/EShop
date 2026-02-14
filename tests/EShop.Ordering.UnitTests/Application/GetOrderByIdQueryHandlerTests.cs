using EShop.Ordering.Application.Interfaces;
using EShop.Ordering.Application.Queries.GetOrderById;
using EShop.Ordering.Domain.Entities;
using EShop.Ordering.Domain.ValueObjects;
using EShop.Shared.Exceptions;
using FluentAssertions;
using Moq;

namespace EShop.Ordering.UnitTests.Application;

public class GetOrderByIdQueryHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly GetOrderByIdQueryHandler _handler;

    public GetOrderByIdQueryHandlerTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _handler = new GetOrderByIdQueryHandler(_orderRepositoryMock.Object);
    }

    private static Order CreateTestOrder(Guid? userId = null)
    {
        var uid = userId ?? Guid.NewGuid();
        var address = new Address("123 Main St", "Athens", "10431", "Greece");
        var items = new List<(Guid, string, decimal, int)>
        {
            (Guid.NewGuid(), "Product 1", 25.00m, 2)
        };
        return Order.Create(uid, address, items);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsOrderDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order = CreateTestOrder(userId);
        var query = new GetOrderByIdQuery(order.Id, userId);

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(order.Id);
        result.UserId.Should().Be(userId);
        result.Status.Should().Be("Pending");
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var query = new GetOrderByIdQuery(Guid.NewGuid(), Guid.NewGuid());

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act
        var act = () => _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WrongUser_ThrowsBusinessRuleException()
    {
        // Arrange
        var order = CreateTestOrder();
        var differentUserId = Guid.NewGuid();
        var query = new GetOrderByIdQuery(order.Id, differentUserId);

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var act = () => _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*own orders*");
    }
}
