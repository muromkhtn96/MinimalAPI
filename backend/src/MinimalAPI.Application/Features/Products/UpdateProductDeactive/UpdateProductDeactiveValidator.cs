using FluentValidation;

namespace MinimalAPI.Application.Features.Products.UpdateProductDeactive;

public class UpdateProductDeactiveValidator : AbstractValidator<UpdateProductDeactiveCommand>
{
    /// <summary> Validator cho lệnh cập nhật trạng thái không hoạt động của sản phẩm. </summary>
    public UpdateProductDeactiveValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id không được để trống.");
    }
}
