using FluentValidation;

namespace MinimalAPI.Application.Features.Products.UpdateProductDeactive;

public class UpdateProductDeactiveValidator : AbstractValidator<UpdateProductDeactiveCommand>
{
    public UpdateProductDeactiveValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id không được để trống.");
    }
}
