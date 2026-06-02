using FluentValidation;

namespace MinimalAPI.Application.Features.Customers.UpdateCustomer;

public sealed class UpdateCustomerValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerValidator()
    {
        /// <summary>
        /// Id: Bắt buộc, phải tồn tại trong hệ thống.
        /// </summary>
        /// <returns></returns>
        RuleFor(x => x.Id).NotEmpty();
        /// <summary>
        /// FullName: Bắt buộc, từ 2 đến 200 ký tự.
        /// </summary>
        /// <returns></returns>
        RuleFor(x => x.FullName).NotEmpty().Length(2, 200);
        /// <summary>
        /// Phone: Optional, đúng định dạng SĐT VN (10–11 số, bắt đầu bằng 0 hoặc +84)
        /// </summary>
        /// <returns></returns>
        RuleFor(x => x.Phone).Matches(@"^(0|\+84)[3|5|7|8|9][0-9]{8,9}$").When(x => !string.IsNullOrWhiteSpace(x.Phone));
        /// <summary>
        /// TaxCode: Optional, nếu có thì phải là số và dài từ 10 đến 13 ký tự.         
        /// </summary>
        /// <returns></returns>
        RuleFor(x => x.TaxCode).Matches(@"^[0-9]{10,13}$").When(x => !string.IsNullOrWhiteSpace(x.TaxCode));
    }
}