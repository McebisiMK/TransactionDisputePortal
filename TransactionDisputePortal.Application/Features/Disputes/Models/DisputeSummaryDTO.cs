using AutoMapper;
using TransactionDisputePortal.Application.Common.Mappings;
using TransactionDisputePortal.Domain.Entities;
using TransactionDisputePortal.Domain.Enums;

namespace TransactionDisputePortal.Application.Features.Disputes.Models;

public record DisputeSummaryDTO : IMapFrom<Dispute>
{
    public Guid Id { get; init; }
    public string Reference { get; init; } = string.Empty;
    public Guid TransactionId { get; init; }
    public string TransactionReference { get; init; } = string.Empty;
    public string Merchant { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public DateTime TransactionDate { get; init; }
    public DisputeReason Reason { get; init; }
    public DisputeStatus Status { get; init; }
    public DateTime SubmittedAt { get; init; }
    public DateTime LastUpdatedAt { get; init; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Dispute, DisputeSummaryDTO>()
            .ForMember(dto => dto.TransactionReference, options => options.MapFrom(dispute => dispute.Transaction.Reference))
            .ForMember(dto => dto.Merchant, options => options.MapFrom(dispute => dispute.Transaction.Merchant))
            .ForMember(dto => dto.Amount, options => options.MapFrom(dispute => dispute.Transaction.Amount))
            .ForMember(dto => dto.Currency, options => options.MapFrom(dispute => dispute.Transaction.Currency))
            .ForMember(dto => dto.TransactionDate, options => options.MapFrom(dispute => dispute.Transaction.TransactionDate))
            .ForMember(dto => dto.LastUpdatedAt, options => options.MapFrom(dispute => dispute.StatusHistory.Max(history => history.ChangedAt)));
    }
}
