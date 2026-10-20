namespace TransactionDisputePortal.Application.Common.Interfaces;

public interface ICurrentCustomerService
{
    Task<Guid> GetCustomerIdAsync(CancellationToken cancellationToken);
}
