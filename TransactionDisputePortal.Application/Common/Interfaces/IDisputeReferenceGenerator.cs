namespace TransactionDisputePortal.Application.Common.Interfaces;

public interface IDisputeReferenceGenerator
{
    Task<string> NextReferenceAsync(CancellationToken cancellationToken);
}
