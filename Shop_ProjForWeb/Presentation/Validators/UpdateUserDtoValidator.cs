using FluentValidation;
using Shop_ProjForWeb.Core.Application.DTOs;

namespace Shop_ProjForWeb.Presentation.Validators;

public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
{
    public UpdateUserDtoValidator()
    {
        RuleFor(x => x.FullName)
            .Length(2, 100).WithMessage("Full name must be between 2 and 100 characters")
            .When(x => !string.IsNullOrEmpty(x.FullName));
    }
}
