using FluentValidation;

namespace MinimalAPI.Application.Features.Inventories.CreateInventory;

public class CreateInventoryValidator : AbstractValidator<CreateInventoryCommand>
{
    /// <summary>
    /// Khởi tạo validator cho lệnh tạo tồn kho mới
    /// </summary>
    public CreateInventoryValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Mã tồn kho không được để trống.");

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Số lượng tồn kho không được âm.");
    }
}
