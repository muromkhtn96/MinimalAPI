using FluentValidation;

namespace MinimalAPI.Application.Features.Products.UpdateProductDeactive;

public class UpdateProductDeactiveValidator : AbstractValidator<UpdateProductDeactiveCommand>
{
    public UpdateProductDeactiveValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("ProductId không được để trống.");
    }
}