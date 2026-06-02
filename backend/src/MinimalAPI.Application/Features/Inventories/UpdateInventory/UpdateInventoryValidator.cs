using FluentValidation;

namespace MinimalAPI.Application.Features.Inventories.UpdateInventory;

public class UpdateInventoryValidator : AbstractValidator<UpdateInventoryCommand>
{
    /// <summary>
    /// Khởi tạo validator cho lệnh cập nhật thông tin tồn kho
    /// </summary>
    public UpdateInventoryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Mã tồn kho không được để trống.");

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Số lượng tồn kho không được âm.");
    }   
}