using Microsoft.EntityFrameworkCore;
using TransactionDisputePortal.Application.Common.Exceptions;
using TransactionDisputePortal.Application.Common.Interfaces;

namespace TransactionDisputePortal.Application.Common.Services;

public class CurrentCustomerService(IApplicationDbContext context, ICurrentUserService currentUserService) : ICurrentCustomerService
{
    private readonly IApplicationDbContext context = context;
    private readonly ICurrentUserService currentUserService = currentUserService;

    private Guid? customerId;

    public async Task<Guid> GetCustomerIdAsync(CancellationToken cancellationToken)
    {
        if (customerId.HasValue) return customerId.Value;

        var userId = currentUserService.UserId ?? throw new ForbiddenAccessException();

        customerId = await context.Customers
                            .Where(customer => customer.ExternalUserId == userId)
                            .Select(customer => (Guid?)customer.Id)
                            .FirstOrDefaultAsync(cancellationToken)
                            ?? throw new ForbiddenAccessException();

        return customerId.Value;
    }
}
