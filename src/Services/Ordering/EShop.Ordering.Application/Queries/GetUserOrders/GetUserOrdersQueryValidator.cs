using EShop.Ordering.Domain.Enums;
using FluentValidation;

namespace EShop.Ordering.Application.Queries.GetUserOrders;

public class GetUserOrdersQueryValidator : AbstractValidator<GetUserOrdersQuery>
{
    public GetUserOrdersQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Page must be greater than zero.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50).WithMessage("PageSize must be between 1 and 50.");

        RuleFor(x => x.Status)
            .Must(s => s is null || Enum.IsDefined(typeof(OrderStatus), s))
            .WithMessage("Status must be a valid OrderStatus value.");
    }
}
