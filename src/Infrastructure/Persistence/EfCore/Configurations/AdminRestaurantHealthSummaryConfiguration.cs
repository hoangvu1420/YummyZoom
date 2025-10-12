using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YummyZoom.Infrastructure.Persistence.ReadModels.Admin;

namespace YummyZoom.Infrastructure.Persistence.EfCore.Configurations;

public sealed class AdminRestaurantHealthSummaryConfiguration : IEntityTypeConfiguration<AdminRestaurantHealthSummary>
{
    public void Configure(EntityTypeBuilder<AdminRestaurantHealthSummary> builder)
    {
        builder.ToTable("AdminRestaurantHealthSummaries");

        builder.HasKey(x => x.RestaurantId);

        builder.Property(x => x.RestaurantName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.IsVerified).IsRequired();
        builder.Property(x => x.IsAcceptingOrders).IsRequired();
        builder.Property(x => x.OrdersLast7Days).IsRequired();
        builder.Property(x => x.OrdersLast30Days).IsRequired();

        builder.Property(x => x.RevenueLast30Days)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.AverageRating)
            .HasColumnType("double precision")
            .IsRequired();

        builder.Property(x => x.TotalReviews).IsRequired();
        builder.Property(x => x.CouponRedemptionsLast30Days).IsRequired();

        builder.Property(x => x.OutstandingBalance)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.LastOrderAtUtc).IsRequired(false);

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(x => new { x.IsVerified, x.IsAcceptingOrders })
            .HasDatabaseName("IX_AdminRestaurantHealthSummaries_VerifiedAccepting");

        builder.HasIndex(x => x.UpdatedAtUtc)
            .HasDatabaseName("IX_AdminRestaurantHealthSummaries_UpdatedAtUtc");
    }
}
