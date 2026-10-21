using AutoMapper;
using TransactionDisputePortal.Application.Common.Mappings;
using TransactionDisputePortal.Application.Features.Disputes.Models;
using TransactionDisputePortal.Domain.Entities;
using TransactionDisputePortal.Domain.Enums;

namespace TransactionDisputePortal.Application.Features.Agent.Models;

public record AgentDisputeDTO : IMapFrom<Dispute>
{
    public Guid Id { get; init; }
    public string Reference { get; init; } = string.Empty;
    public Guid CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
    public Guid TransactionId { get; init; }
    public DisputeReason Reason { get; init; }
    public string? Description { get; init; }
    public DisputeStatus Status { get; init; }
    public DateTime SubmittedAt { get; init; }
    public byte[] RowVersion { get; init; } = [];
    public DisputeTransactionDTO Transaction { get; init; } = null!;
    public IReadOnlyList<DisputeStatusHistoryDTO> Timeline { get; init; } = [];

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Dispute, AgentDisputeDTO>()
            .ForMember(dto => dto.CustomerName, options => options.MapFrom(dispute => dispute.Customer.FullName))
            .ForMember(dto => dto.CustomerEmail, options => options.MapFrom(dispute => dispute.Customer.Email))
            .ForMember(dto => dto.Timeline, options => options.MapFrom(dispute => dispute.StatusHistory.OrderBy(history => history.ChangedAt)));
    }
}
