using FluentValidation;
using Shop_ProjForWeb.Core.Application.DTOs;

namespace Shop_ProjForWeb.Presentation.Validators;

public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserDtoValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .Length(2, 100).WithMessage("Full name must be between 2 and 100 characters");
    }
}
