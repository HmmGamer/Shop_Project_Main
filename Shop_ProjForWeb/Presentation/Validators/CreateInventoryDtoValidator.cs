using FluentValidation;
using Shop_ProjForWeb.Core.Application.DTOs;

namespace Shop_ProjForWeb.Presentation.Validators;

public class CreateInventoryDtoValidator : AbstractValidator<CreateInventoryDto>
{
    public CreateInventoryDtoValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required");

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0).WithMessage("Quantity must be greater than or equal to 0");
    }
}
