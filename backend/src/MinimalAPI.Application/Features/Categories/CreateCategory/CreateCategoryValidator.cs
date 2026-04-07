using FluentValidation;

namespace MinimalAPI.Application.Features.Categories.CreateCategory;

public class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên danh mục không được để trống.")
            .MaximumLength(100).WithMessage("Tên danh mục tối đa 100 ký tự.");
    }
}
