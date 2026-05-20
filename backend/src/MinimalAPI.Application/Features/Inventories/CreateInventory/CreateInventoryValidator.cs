using FluentValidation;

namespace MinimalAPI.Application.Features.Inventories.CreateInventory;

public class CreateInventoryValidator : AbstractValidator<CreateInventoryCommand>
{
    public CreateInventoryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Mã tồn kho không được để trống.");

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Số lượng tồn kho không được âm.");
    }
}
