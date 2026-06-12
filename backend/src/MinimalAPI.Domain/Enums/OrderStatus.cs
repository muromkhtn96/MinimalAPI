using System.ComponentModel;

namespace MinimalAPI.Domain.Enums;
public enum OrderStatus : short
{
    [Description("Chờ xử lý")]
    Pending = 0,
    [Description("Đã xác nhận")]
    Confirmed = 1,
    [Description("Đã Đóng")]
    Cancelled = 2
}