using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YummyZoom.Infrastructure.Persistence.ReadModels.Admin;

namespace YummyZoom.Infrastructure.Persistence.EfCore.Configurations;

public sealed class AdminPlatformMetricsSnapshotConfiguration : IEntityTypeConfiguration<AdminPlatformMetricsSnapshot>
{
    public void Configure(EntityTypeBuilder<AdminPlatformMetricsSnapshot> builder)
    {
        builder.ToTable("AdminPlatformMetricsSnapshots");

        builder.HasKey(x => x.SnapshotId);
        builder.Property(x => x.SnapshotId)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.TotalOrders).IsRequired();
        builder.Property(x => x.ActiveOrders).IsRequired();
        builder.Property(x => x.DeliveredOrders).IsRequired();

        builder.Property(x => x.GrossMerchandiseVolume)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.TotalRefunds)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.ActiveRestaurants).IsRequired();
        builder.Property(x => x.ActiveCustomers).IsRequired();
        builder.Property(x => x.OpenSupportTickets).IsRequired();
        builder.Property(x => x.TotalReviews).IsRequired();

        builder.Property(x => x.LastOrderAtUtc).IsRequired(false);

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(x => x.UpdatedAtUtc)
            .HasDatabaseName("IX_AdminPlatformMetricsSnapshots_UpdatedAtUtc");
    }
}
