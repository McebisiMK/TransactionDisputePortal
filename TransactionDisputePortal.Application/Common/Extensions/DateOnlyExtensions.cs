namespace TransactionDisputePortal.Application.Common.Extensions;

public static class DateOnlyExtensions
{
    // Customers are in South Africa, so a date they pick means that calendar day in South African time, not in UTC.
    public static readonly TimeZoneInfo BusinessTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Johannesburg");

    public static DateTime ToUtcStartOfBusinessDay(this DateOnly date) => TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(TimeOnly.MinValue), BusinessTimeZone);
}
