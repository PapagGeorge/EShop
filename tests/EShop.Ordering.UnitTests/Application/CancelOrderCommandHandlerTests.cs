using EShop.Ordering.Application.Commands.CancelOrder;
using EShop.Ordering.Application.Interfaces;
using EShop.Ordering.Domain.Entities;
using EShop.Ordering.Domain.Exceptions;
using EShop.Ordering.Domain.ValueObjects;
using EShop.Shared.Exceptions;
using FluentAssertions;
using Moq;

namespace EShop.Ordering.UnitTests.Application;

public class CancelOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<IEventBus> _eventBusMock;
    private readonly CancelOrderCommandHandler _handler;

    public CancelOrderCommandHandlerTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _eventBusMock = new Mock<IEventBus>();
        _handler = new CancelOrderCommandHandler(
            _orderRepositoryMock.Object,
            _eventBusMock.Object);
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
    public async Task Handle_ValidRequest_CancelsOrderAndReturnsDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order = CreateTestOrder(userId);
        var command = new CancelOrderCommand(order.Id, userId);

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("Cancelled");
        _orderRepositoryMock.Verify(x => x.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var command = new CancelOrderCommand(Guid.NewGuid(), Guid.NewGuid());

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WrongUser_ThrowsBusinessRuleException()
    {
        // Arrange
        var order = CreateTestOrder();
        var differentUserId = Guid.NewGuid();
        var command = new CancelOrderCommand(order.Id, differentUserId);

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*own orders*");
    }

    [Fact]
    public async Task Handle_OrderAlreadyConfirmed_ThrowsInvalidTransition()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order = CreateTestOrder(userId);
        order.Confirm(); // Move to Confirmed — can't cancel from here
        var command = new CancelOrderCommand(order.Id, userId);

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOrderStatusTransitionException>();
    }
}
