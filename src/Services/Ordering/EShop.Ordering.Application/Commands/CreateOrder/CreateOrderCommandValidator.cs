using FluentValidation;

namespace EShop.Ordering.Application.Commands.CreateOrder;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.ShippingAddress)
            .NotNull().WithMessage("Shipping address is required.");

        When(x => x.ShippingAddress is not null, () =>
        {
            RuleFor(x => x.ShippingAddress.Street)
                .NotEmpty().WithMessage("Street is required.")
                .MaximumLength(200).WithMessage("Street must not exceed 200 characters.");

            RuleFor(x => x.ShippingAddress.City)
                .NotEmpty().WithMessage("City is required.")
                .MaximumLength(100).WithMessage("City must not exceed 100 characters.");

            RuleFor(x => x.ShippingAddress.ZipCode)
                .NotEmpty().WithMessage("ZipCode is required.")
                .MaximumLength(20).WithMessage("ZipCode must not exceed 20 characters.");

            RuleFor(x => x.ShippingAddress.Country)
                .NotEmpty().WithMessage("Country is required.")
                .MaximumLength(100).WithMessage("Country must not exceed 100 characters.");
        });

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Order must contain at least one item.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage("ProductId is required.");

            item.RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than zero.");
        });
    }
}
