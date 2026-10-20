using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransactionDisputePortal.Domain.Entities;

namespace TransactionDisputePortal.Infrastructure.Persistence.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.HasKey(account => account.Id);

        builder.Property(account => account.AccountNumber)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(account => account.Type)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne(account => account.Customer)
            .WithMany(customer => customer.Accounts)
            .HasForeignKey(account => account.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
