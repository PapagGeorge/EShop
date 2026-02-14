using EShop.Ordering.Application.Queries.GetUserOrders;
using EShop.Ordering.Domain.Enums;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace EShop.Ordering.UnitTests.Application;

public class GetUserOrdersQueryValidatorTests
{
    private readonly GetUserOrdersQueryValidator _validator;

    public GetUserOrdersQueryValidatorTests()
    {
        _validator = new GetUserOrdersQueryValidator();
    }

    [Fact]
    public async Task Validate_ValidQuery_ShouldNotHaveErrors()
    {
        var query = new GetUserOrdersQuery(Guid.NewGuid(), null, 1, 10);
        var result = await _validator.TestValidateAsync(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_ValidQueryWithStatus_ShouldNotHaveErrors()
    {
        var query = new GetUserOrdersQuery(Guid.NewGuid(), OrderStatus.Pending, 1, 10);
        var result = await _validator.TestValidateAsync(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_PageZero_ShouldHaveError()
    {
        var query = new GetUserOrdersQuery(Guid.NewGuid(), null, 0, 10);
        var result = await _validator.TestValidateAsync(query);
        result.ShouldHaveValidationErrorFor(x => x.Page);
    }

    [Fact]
    public async Task Validate_PageSizeZero_ShouldHaveError()
    {
        var query = new GetUserOrdersQuery(Guid.NewGuid(), null, 1, 0);
        var result = await _validator.TestValidateAsync(query);
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public async Task Validate_PageSizeTooLarge_ShouldHaveError()
    {
        var query = new GetUserOrdersQuery(Guid.NewGuid(), null, 1, 51);
        var result = await _validator.TestValidateAsync(query);
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public async Task Validate_EmptyUserId_ShouldHaveError()
    {
        var query = new GetUserOrdersQuery(Guid.Empty, null, 1, 10);
        var result = await _validator.TestValidateAsync(query);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }
}
