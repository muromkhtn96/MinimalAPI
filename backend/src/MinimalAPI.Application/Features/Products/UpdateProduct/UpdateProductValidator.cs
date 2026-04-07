using FluentValidation;

namespace MinimalAPI.Application.Features.Products.UpdateProduct;

public class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id sản phẩm không được để trống.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên sản phẩm không được để trống.")
            .MaximumLength(200).WithMessage("Tên sản phẩm tối đa 200 ký tự.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Giá phải lớn hơn 0.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Mã tiền tệ không được để trống.")
            .Length(3).WithMessage("Mã tiền tệ phải đúng 3 ký tự.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Danh mục không được để trống.");
    }
}
