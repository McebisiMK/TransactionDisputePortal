using System.ComponentModel;
using System.Reflection;

namespace TransactionDisputePortal.Application.Common.Extensions;

public static class EnumExtensions
{
    public static string GetDisplayName<TEnum>(this TEnum value) where TEnum : struct, Enum
    {
        var description = typeof(TEnum).GetField(value.ToString())?.GetCustomAttribute<DescriptionAttribute>();

        return description?.Description ?? value.ToString();
    }
}
