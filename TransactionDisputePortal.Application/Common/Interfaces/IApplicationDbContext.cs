using Microsoft.EntityFrameworkCore;
using TransactionDisputePortal.Domain.Entities;

namespace TransactionDisputePortal.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Account> Accounts { get; }
    DbSet<Dispute> Disputes { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Transaction> Transactions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
