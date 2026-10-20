using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransactionDisputePortal.Domain.Entities;

namespace TransactionDisputePortal.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(customer => customer.Id);

        builder.Property(customer => customer.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(customer => customer.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(customer => customer.ExternalUserId)
            .IsUnique();
    }
}
