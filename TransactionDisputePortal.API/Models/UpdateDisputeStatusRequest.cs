using TransactionDisputePortal.Domain.Enums;

namespace TransactionDisputePortal.API.Models;

public record UpdateDisputeStatusRequest(DisputeStatus? Status, string? Comment, byte[]? RowVersion);
