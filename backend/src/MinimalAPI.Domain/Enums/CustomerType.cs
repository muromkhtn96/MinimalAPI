using System.ComponentModel;

namespace MinimalAPI.Domain.Enums;
public enum CustomerType : short
{
    [Description("")]
    Individual = 0,
    [Description("")]
    Company = 1
}