using AutoMapper;
using TransactionDisputePortal.Application.Common.Mappings;
using TransactionDisputePortal.Application.Features.Transactions.Models;
using TransactionDisputePortal.Domain.Entities;
using TransactionDisputePortal.Domain.Enums;

namespace TransactionDisputePortal.Application.Features.Disputes.Models;

public record DisputeTransactionDTO : IMapFrom<Transaction>
{
    public Guid Id { get; init; }
    public string Reference { get; init; } = string.Empty;
    public string AccountNumber { get; init; } = string.Empty;
    public AccountType AccountType { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string Merchant { get; init; } = string.Empty;
    public TransactionCategory Category { get; init; }
    public DateTime TransactionDate { get; init; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Transaction, DisputeTransactionDTO>()
            .ForMember(dto => dto.AccountNumber, options => options.MapFrom(TransactionDTO.MaskedAccountNumber))
            .ForMember(dto => dto.AccountType, options => options.MapFrom(transaction => transaction.Account.Type));
    }
}
