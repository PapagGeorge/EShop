using EShop.Ordering.Application.Commands.CreateOrder;
using EShop.Ordering.Application.DTOs;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace EShop.Ordering.UnitTests.Application;

public class CreateOrderCommandValidatorTests
{
    private readonly CreateOrderCommandValidator _validator;

    public CreateOrderCommandValidatorTests()
    {
        _validator = new CreateOrderCommandValidator();
    }

    private static CreateOrderCommand CreateValidCommand()
    {
        return new CreateOrderCommand(
            Guid.NewGuid(),
            new AddressDto("123 Main St", "Athens", "10431", "Greece"),
            new List<OrderItemRequest> { new(Guid.NewGuid(), 2) });
    }

    [Fact]
    public async Task Validate_ValidCommand_ShouldNotHaveErrors()
    {
        var command = CreateValidCommand();
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_EmptyUserId_ShouldHaveError()
    {
        var command = CreateValidCommand() with { UserId = Guid.Empty };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public async Task Validate_NullAddress_ShouldHaveError()
    {
        var command = CreateValidCommand() with { ShippingAddress = null! };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.ShippingAddress);
    }

    [Fact]
    public async Task Validate_EmptyStreet_ShouldHaveError()
    {
        var command = CreateValidCommand() with
        {
            ShippingAddress = new AddressDto("", "Athens", "10431", "Greece")
        };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.ShippingAddress.Street);
    }

    [Fact]
    public async Task Validate_EmptyItems_ShouldHaveError()
    {
        var command = CreateValidCommand() with { Items = new List<OrderItemRequest>() };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Items);
    }

    [Fact]
    public async Task Validate_ItemWithEmptyProductId_ShouldHaveError()
    {
        var command = CreateValidCommand() with
        {
            Items = new List<OrderItemRequest> { new(Guid.Empty, 2) }
        };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public async Task Validate_ItemWithZeroQuantity_ShouldHaveError()
    {
        var command = CreateValidCommand() with
        {
            Items = new List<OrderItemRequest> { new(Guid.NewGuid(), 0) }
        };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public async Task Validate_StreetTooLong_ShouldHaveError()
    {
        var command = CreateValidCommand() with
        {
            ShippingAddress = new AddressDto(new string('A', 201), "Athens", "10431", "Greece")
        };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.ShippingAddress.Street);
    }
}
