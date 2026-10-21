using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransactionDisputePortal.Domain.Entities;

namespace TransactionDisputePortal.Infrastructure.Persistence.Configurations;

public class DisputeStatusHistoryConfiguration : IEntityTypeConfiguration<DisputeStatusHistory>
{
    public void Configure(EntityTypeBuilder<DisputeStatusHistory> builder)
    {
        builder.ToTable(nameof(DisputeStatusHistory));

        builder.HasKey(history => history.Id);

        builder.Property(history => history.FromStatus)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(history => history.ToStatus)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(history => history.Comment)
            .HasMaxLength(DisputeStatusHistory.CommentMaxLength);

        builder.Property(history => history.ChangedBy)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(history => new { history.DisputeId, history.ChangedAt });
    }
}
