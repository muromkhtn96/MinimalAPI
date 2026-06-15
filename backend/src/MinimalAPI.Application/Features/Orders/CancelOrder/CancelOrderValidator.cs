using FluentValidation;

namespace MinimalAPI.Application.Features.Orders.CancelOrder;

public sealed class CancelOrderValidator : AbstractValidator<CancelOrderCommand>
{
    /// <summary> Validator cho lệnh đóng đơn hàng. </summary>
    public CancelOrderValidator()
    {
        /// Kiểm tra rằng ID đơn hàng không được để trống
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Khách hàng Id không được để trống");
    }
}