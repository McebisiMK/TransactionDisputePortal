using AutoMapper;
using TransactionDisputePortal.Application.Common.Mappings;
using TransactionDisputePortal.Domain.Entities;
using TransactionDisputePortal.Domain.Enums;

namespace TransactionDisputePortal.Application.Features.Agent.Models;

public record DisputeQueueItemDTO : IMapFrom<Dispute>
{
    public Guid Id { get; init; }
    public string Reference { get; init; } = string.Empty;
    public Guid CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string TransactionReference { get; init; } = string.Empty;
    public string Merchant { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public DisputeReason Reason { get; init; }
    public DisputeStatus Status { get; init; }
    public DateTime SubmittedAt { get; init; }
    public DateTime LastUpdatedAt { get; init; }
    public byte[] RowVersion { get; init; } = [];

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Dispute, DisputeQueueItemDTO>()
            .ForMember(dto => dto.CustomerName, options => options.MapFrom(dispute => dispute.Customer.FullName))
            .ForMember(dto => dto.TransactionReference, options => options.MapFrom(dispute => dispute.Transaction.Reference))
            .ForMember(dto => dto.Merchant, options => options.MapFrom(dispute => dispute.Transaction.Merchant))
            .ForMember(dto => dto.Amount, options => options.MapFrom(dispute => dispute.Transaction.Amount))
            .ForMember(dto => dto.Currency, options => options.MapFrom(dispute => dispute.Transaction.Currency))
            .ForMember(dto => dto.LastUpdatedAt, options => options.MapFrom(dispute => dispute.StatusHistory.Max(history => history.ChangedAt)));
    }
}
