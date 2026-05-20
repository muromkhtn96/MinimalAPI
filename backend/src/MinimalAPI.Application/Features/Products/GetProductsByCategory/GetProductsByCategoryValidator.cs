using FluentValidation;

namespace MinimalAPI.Application.Features.Products.GetProductsByCategory;

public class GetProductsByCategoryValidator : AbstractValidator<GetProductsByCategoryQuery>
{
    public GetProductsByCategoryValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Id danh mục không được để trống.");
    }
}