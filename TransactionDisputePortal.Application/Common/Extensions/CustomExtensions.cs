using System.Diagnostics.CodeAnalysis;

namespace TransactionDisputePortal.Application.Common.Extensions;

public static class CustomExtensions
{
    public static bool IsNull<T>([NotNullWhen(false)] this T? value) where T : class => value is null;

    public static bool IsNotNull<T>([NotNullWhen(true)] this T? value) where T : class => value is not null;

    public static bool IsBlank([NotNullWhen(false)] this string? value) => string.IsNullOrWhiteSpace(value);

    public static bool IsNotBlank([NotNullWhen(true)] this string? value) => !string.IsNullOrWhiteSpace(value);
}
