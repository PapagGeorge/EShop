using EShop.Ordering.Application.Commands.CreateOrder;
using EShop.Ordering.Application.DTOs;
using EShop.Ordering.Application.Interfaces;
using EShop.Ordering.Domain.Entities;
using EShop.Shared.Exceptions;
using FluentAssertions;
using Moq;

namespace EShop.Ordering.UnitTests.Application;

public class CreateOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<ICatalogService> _catalogServiceMock;
    private readonly Mock<IEventBus> _eventBusMock;
    private readonly CreateOrderCommandHandler _handler;

    public CreateOrderCommandHandlerTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _catalogServiceMock = new Mock<ICatalogService>();
        _eventBusMock = new Mock<IEventBus>();
        _handler = new CreateOrderCommandHandler(
            _orderRepositoryMock.Object,
            _catalogServiceMock.Object,
            _eventBusMock.Object);
    }

    private static CreateOrderCommand CreateValidCommand()
    {
        var productId = Guid.NewGuid();
        return new CreateOrderCommand(
            Guid.NewGuid(),
            new AddressDto("123 Main St", "Athens", "10431", "Greece"),
            new List<OrderItemRequest> { new(productId, 2) });
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsOrderDto()
    {
        // Arrange
        var command = CreateValidCommand();
        var productId = command.Items[0].ProductId;

        _catalogServiceMock
            .Setup(x => x.GetProductsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductDto> { new(productId, "Test Product", 29.99m) });

        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order o, CancellationToken _) => o);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(command.UserId);
        result.Status.Should().Be("Pending");
        result.Items.Should().HaveCount(1);
        result.Items[0].ProductName.Should().Be("Test Product");
        result.Items[0].UnitPrice.Should().Be(29.99m);
        result.Items[0].Quantity.Should().Be(2);
        result.TotalAmount.Should().Be(59.98m);
        result.ShippingAddress.Street.Should().Be("123 Main St");
    }

    [Fact]
    public async Task Handle_ProductsNotFound_ThrowsBusinessRuleException()
    {
        // Arrange
        var command = CreateValidCommand();

        _catalogServiceMock
            .Setup(x => x.GetProductsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductDto>());

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("Products not found*");
    }

    [Fact]
    public async Task Handle_ValidCommand_PublishesDomainEvents()
    {
        // Arrange
        var command = CreateValidCommand();
        var productId = command.Items[0].ProductId;

        _catalogServiceMock
            .Setup(x => x.GetProductsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductDto> { new(productId, "Test Product", 10.00m) });

        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order o, CancellationToken _) => o);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _eventBusMock.Verify(
            x => x.PublishAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }
}
