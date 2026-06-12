using FluentValidation;
using MinimalAPI.Application.Features.Orders.UpdateOrder;

public sealed class UpdateOrderValidator : AbstractValidator<UpdateOrderCommand>
{
    /// <summary> Validator cho lệnh cập nhật đơn hàng. </summary>
    public UpdateOrderValidator()
    {
        /// Kiểm tra rằng ID đơn hàng không được để trống
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("ID đơn hàng không được để trống.");
        /// Kiểm tra rằng ghi chú không được để trống
        RuleFor(x => x.Details)
            .NotEmpty().WithMessage("Đơn hàng phải có ít nhất một dòng.");
        /// Kiểm tra từng chi tiết đơn hàng trong danh sách
        RuleForEach(x => x.Details).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId)
                .NotEmpty().WithMessage("Sản phẩm không được để trống.");

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("Số lượng phải lớn hơn 0.");
        });
    }
}