using System.ComponentModel;

namespace MinimalAPI.Domain.Enums;
public enum Gender : short
{
    [Description("Nam")]
    Male = 0,
    [Description("Nữ")]
    Female = 1,
    [Description("Khác")]
    Other = 2
}