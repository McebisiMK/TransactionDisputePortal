namespace TransactionDisputePortal.Application.Common.Models;

public record LookupDTO<TEnum>(TEnum Id, string Name) where TEnum : struct, Enum;
