using FluentValidation;

namespace MinimalAPI.Application.Features.Products.UpdateProductActive;

public class UpdateProductActiveValidator : AbstractValidator<UpdateProductActiveCommand>
{
    /// <summary> Validator cho lệnh cập nhật trạng thái hoạt động của sản phẩm. </summary>
    public UpdateProductActiveValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id sản phẩm không được để trống.");
    }
}
