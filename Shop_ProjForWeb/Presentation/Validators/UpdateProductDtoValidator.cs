using FluentValidation;
using Shop_ProjForWeb.Core.Application.DTOs;

namespace Shop_ProjForWeb.Presentation.Validators;

public class UpdateProductDtoValidator : AbstractValidator<UpdateProductDto>
{
    public UpdateProductDtoValidator()
    {
        RuleFor(x => x.Name)
            .Length(1, 200).WithMessage("Product name must be between 1 and 200 characters")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.BasePrice)
            .GreaterThan(0).WithMessage("Base price must be greater than 0")
            .When(x => x.BasePrice.HasValue);

        RuleFor(x => x.DiscountPercent)
            .InclusiveBetween(0, 100).WithMessage("Discount percent must be between 0 and 100")
            .When(x => x.DiscountPercent.HasValue);
    }
}
