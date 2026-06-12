using FluentValidation;

namespace MinimalAPI.Application.Features.Orders.CreateOrder;

public sealed class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    /// <summary> Validator cho lệnh tạo đơn hàng mới. </summary>
    public CreateOrderValidator()
    {
        /// Kiểm tra rằng ID khách hàng không được để trống
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Khách hàng không được để trống.");
        /// Kiểm tra rằng danh sách mặt hàng không được để trống
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Đơn hàng phải có ít nhất một dòng.");
        /// Kiểm tra từng mặt hàng trong danh sách
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId)
                .NotEmpty().WithMessage("Sản phẩm không được để trống.");

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("Số lượng phải lớn hơn 0.");
        });
    }
}