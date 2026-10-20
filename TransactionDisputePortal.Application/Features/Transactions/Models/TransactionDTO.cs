using AutoMapper;
using TransactionDisputePortal.Application.Common.Mappings;
using TransactionDisputePortal.Domain.Entities;
using TransactionDisputePortal.Domain.Enums;

namespace TransactionDisputePortal.Application.Features.Transactions.Models;

public record TransactionDTO : IMapFrom<Transaction>
{
    public const string AccountNumberMask = "******";
    public const int VisibleAccountNumberDigits = 4;

    public Guid Id { get; init; }
    public Guid AccountId { get; init; }
    public string Reference { get; init; } = string.Empty;
    public string AccountNumber { get; init; } = string.Empty;
    public AccountType AccountType { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string Merchant { get; init; } = string.Empty;
    public TransactionCategory Category { get; init; }
    public DateTime TransactionDate { get; init; }
    public TransactionStatus Status { get; init; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Transaction, TransactionDTO>()
            .ForMember(dto => dto.AccountNumber, options => options.MapFrom(transaction =>
                transaction.Account.AccountNumber.Length > VisibleAccountNumberDigits
                    ? AccountNumberMask + transaction.Account.AccountNumber.Substring(transaction.Account.AccountNumber.Length - VisibleAccountNumberDigits, VisibleAccountNumberDigits)
                    : AccountNumberMask))
            .ForMember(dto => dto.AccountType, options => options.MapFrom(transaction => transaction.Account.Type));
    }
}
