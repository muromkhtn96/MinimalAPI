using FluentValidation;

namespace MinimalAPI.Application.Features.Orders.ConfirmOrder;

public sealed class ConfirmOrderValidator : AbstractValidator<ConfirmOrderCommand>
{
    /// <summary> Validator cho lệnh xác nhận đơn hàng. </summary>
    public ConfirmOrderValidator()
    {
        /// Kiểm tra rằng ID đơn hàng không được để trống
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Khách hàng Id không được để trống");
    }
}