using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransactionDisputePortal.Domain.Entities;
using TransactionDisputePortal.Domain.Enums;

namespace TransactionDisputePortal.Infrastructure.Persistence.Configurations;

public class DisputeConfiguration : IEntityTypeConfiguration<Dispute>
{
    public const string ReferenceSequence = "DisputeReferenceNumbers";

    // Keep in step with Dispute.IsOpen: a transaction can only have one dispute in these statuses at a time.
    private static readonly string OpenDisputeFilter = $"[{nameof(Dispute.Status)}] IN ('{DisputeStatus.Submitted}', '{DisputeStatus.UnderReview}')";

    public void Configure(EntityTypeBuilder<Dispute> builder)
    {
        builder.HasKey(dispute => dispute.Id);

        builder.Property(dispute => dispute.Reference)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(dispute => dispute.Reason)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(dispute => dispute.Description)
            .HasMaxLength(Dispute.DescriptionMaxLength);

        builder.Property(dispute => dispute.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(dispute => dispute.RowVersion)
            .IsRowVersion();

        builder.HasIndex(dispute => dispute.Reference)
            .IsUnique();

        builder.HasIndex(dispute => dispute.TransactionId);

        builder.HasIndex(dispute => dispute.TransactionId, "IX_Disputes_TransactionId_Open")
            .IsUnique()
            .HasFilter(OpenDisputeFilter);

        builder.HasIndex(dispute => new { dispute.CustomerId, dispute.Status });

        builder.HasOne(dispute => dispute.Transaction)
            .WithMany(transaction => transaction.Disputes)
            .HasForeignKey(dispute => dispute.TransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(dispute => dispute.Customer)
            .WithMany()
            .HasForeignKey(dispute => dispute.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(dispute => dispute.StatusHistory)
            .WithOne()
            .HasForeignKey(history => history.DisputeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(dispute => dispute.StatusHistory)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
