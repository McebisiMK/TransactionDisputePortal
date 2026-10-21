using Microsoft.EntityFrameworkCore;
using TransactionDisputePortal.Application.Common.Extensions;
using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Infrastructure.Persistence;
using TransactionDisputePortal.Infrastructure.Persistence.Configurations;

namespace TransactionDisputePortal.Infrastructure.Services;

public class DisputeReferenceGenerator(ApplicationDbContext context, TimeProvider timeProvider) : IDisputeReferenceGenerator
{
    private const string NextNumberSql = $"SELECT NEXT VALUE FOR [{DisputeConfiguration.ReferenceSequence}] AS [Value]";

    private readonly ApplicationDbContext context = context;
    private readonly TimeProvider timeProvider = timeProvider;

    public async Task<string> NextReferenceAsync(CancellationToken cancellationToken)
    {
        var numbers = await context.Database
                             .SqlQueryRaw<long>(NextNumberSql)
                             .ToListAsync(cancellationToken);

        var number = numbers.Single();
        var year = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), DateOnlyExtensions.BusinessTimeZone).Year;

        return $"DSP-{year}-{number:D6}";
    }
}
