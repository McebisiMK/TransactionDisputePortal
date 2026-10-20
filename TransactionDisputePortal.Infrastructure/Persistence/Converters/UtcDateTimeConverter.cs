using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace TransactionDisputePortal.Infrastructure.Persistence.Converters;

public class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime(),
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
