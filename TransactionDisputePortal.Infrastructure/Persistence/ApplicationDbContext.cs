using Microsoft.EntityFrameworkCore;
using TransactionDisputePortal.Application.Common.Interfaces;

namespace TransactionDisputePortal.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options), IApplicationDbContext
{
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await base.SaveChangesAsync(cancellationToken);

        return result;
    }
}
