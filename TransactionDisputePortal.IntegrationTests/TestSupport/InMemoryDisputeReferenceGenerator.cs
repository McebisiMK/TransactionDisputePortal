using TransactionDisputePortal.Application.Common.Interfaces;

namespace TransactionDisputePortal.IntegrationTests.TestSupport;

/// <summary>
/// Stands in for the SQL Server sequence, which the in-memory database doesn't support.
/// </summary>
public class InMemoryDisputeReferenceGenerator : IDisputeReferenceGenerator
{
    private static int number;

    public Task<string> NextReferenceAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult($"DSP-{DateTime.UtcNow:yyyy}-{Interlocked.Increment(ref number):D6}");
    }
}
