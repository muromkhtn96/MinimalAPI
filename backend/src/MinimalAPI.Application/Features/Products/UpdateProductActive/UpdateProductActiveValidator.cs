using FluentValidation;

namespace MinimalAPI.Application.Features.Products.UpdateProductActive;

public class UpdateProductActiveValidator : AbstractValidator<UpdateProductActiveCommand>
{
    public UpdateProductActiveValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id sản phẩm không được để trống.");
    }
}
