using System.ComponentModel;

namespace MinimalAPI.Domain.Enums;
public enum CustomerType : short
{
    [Description("Khách hàng cá nhân")]
    Individual = 0,
    [Description("Khách hàng doanh nghiệp")]
    Company = 1
}