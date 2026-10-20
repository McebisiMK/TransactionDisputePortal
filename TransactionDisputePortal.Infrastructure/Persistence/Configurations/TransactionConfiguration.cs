using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransactionDisputePortal.Domain.Entities;

namespace TransactionDisputePortal.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(transaction => transaction.Id);

        builder.Property(transaction => transaction.Reference)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(transaction => transaction.Amount)
            .HasPrecision(18, 2);

        builder.Property(transaction => transaction.Currency)
            .HasMaxLength(3)
            .IsFixedLength()
            .IsRequired();

        builder.Property(transaction => transaction.Merchant)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(transaction => transaction.Category)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(transaction => transaction.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(transaction => transaction.Reference)
            .IsUnique();

        builder.HasIndex(transaction => new { transaction.AccountId, transaction.TransactionDate });

        builder.HasOne(transaction => transaction.Account)
            .WithMany(account => account.Transactions)
            .HasForeignKey(transaction => transaction.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
