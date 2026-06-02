using FluentValidation;
using MinimalAPI.Domain.Enums;

namespace MinimalAPI.Application.Features.Customers.CreateCustomer;

public sealed class CreateCustomerValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerValidator()
    {
        // FullName: Required, 2–200 ký tự
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ và tên / Tên doanh nghiệp không được để trống.")
            .Length(2, 200).WithMessage("Họ và tên phải từ 2 đến 200 ký tự.");

        // Phone: Optional, đúng định dạng SĐT VN (10–11 số, bắt đầu bằng 0 hoặc +84)
        RuleFor(x => x.Phone)
            .Matches(@"^(0|\+84)[3|5|7|8|9][0-9]{8,9}$").WithMessage("Định dạng số điện thoại không hợp lệ (phải là SĐT Việt Nam).")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));

        // Type: Required, thuộc enum CustomerType
        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Loại khách hàng không hợp lệ.");

        // TaxCode: Required nếu Type = Company, 10–13 số
        RuleFor(x => x.TaxCode)
            .NotEmpty().WithMessage("Mã số thuế bắt buộc nhập đối với khách hàng doanh nghiệp.")
            .Matches(@"^[0-9]{10,13}$").WithMessage("Mã số thuế phải là số và dài từ 10 đến 13 ký tự.")
            // Rule này chỉ kích hoạt khi Type là Company
            .When(x => x.Type == CustomerType.Company); 
    }
}