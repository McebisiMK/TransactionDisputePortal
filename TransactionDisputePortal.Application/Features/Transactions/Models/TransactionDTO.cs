using System.Linq.Expressions;
using AutoMapper;
using TransactionDisputePortal.Application.Common.Mappings;
using TransactionDisputePortal.Domain.Entities;
using TransactionDisputePortal.Domain.Enums;

namespace TransactionDisputePortal.Application.Features.Transactions.Models;

public record TransactionDTO : IMapFrom<Transaction>
{
    public const string AccountNumberMask = "******";
    public const int VisibleAccountNumberDigits = 4;

    // Name of the ProjectTo parameter that carries the start of the dispute window, see Dispute.GetDisputeWindowStart.
    public const string DisputeWindowStartParameter = "disputeWindowStart";

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
    public bool IsDisputable { get; init; }
    public Guid? OpenDisputeId { get; init; }

    // Shared with DisputeTransactionDTO so a full account number never leaves the API.
    public static readonly Expression<Func<Transaction, string>> MaskedAccountNumber = transaction =>
        transaction.Account.AccountNumber.Length > VisibleAccountNumberDigits
            ? AccountNumberMask + transaction.Account.AccountNumber.Substring(transaction.Account.AccountNumber.Length - VisibleAccountNumberDigits, VisibleAccountNumberDigits)
            : AccountNumberMask;

    public void Mapping(Profile profile)
    {
        DateTime disputeWindowStart = DateTime.MaxValue;

        profile.CreateMap<Transaction, TransactionDTO>()
            .ForMember(dto => dto.AccountNumber, options => options.MapFrom(MaskedAccountNumber))
            .ForMember(dto => dto.AccountType, options => options.MapFrom(transaction => transaction.Account.Type))
            .ForMember(dto => dto.OpenDisputeId, options => options.MapFrom(transaction => transaction.Disputes
                .Where(dispute => dispute.Status == DisputeStatus.Submitted || dispute.Status == DisputeStatus.UnderReview)
                .Select(dispute => (Guid?)dispute.Id)
                .FirstOrDefault()))
            .ForMember(dto => dto.IsDisputable, options => options.MapFrom(transaction =>
                transaction.Status == TransactionStatus.Posted
                && transaction.TransactionDate >= disputeWindowStart
                && !transaction.Disputes.Any(dispute => dispute.Status == DisputeStatus.Submitted || dispute.Status == DisputeStatus.UnderReview || dispute.Status == DisputeStatus.Approved)));
    }

    public static Dictionary<string, object> ProjectionParameters(DateTime utcNow) => new()
    {
        [DisputeWindowStartParameter] = Dispute.GetDisputeWindowStart(utcNow)
    };
}
