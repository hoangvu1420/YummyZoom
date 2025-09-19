using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YummyZoom.Infrastructure.Persistence.ReadModels.Admin;

namespace YummyZoom.Infrastructure.Persistence.EfCore.Configurations;

public sealed class AdminDailyPerformanceSeriesConfiguration : IEntityTypeConfiguration<AdminDailyPerformanceSeries>
{
    public void Configure(EntityTypeBuilder<AdminDailyPerformanceSeries> builder)
    {
        builder.ToTable("AdminDailyPerformanceSeries");

        builder.HasKey(x => x.BucketDate);

        builder.Property(x => x.BucketDate)
            .HasColumnType("date")
            .HasConversion(
                dateOnly => dateOnly.ToDateTime(TimeOnly.MinValue),
                dateTime => DateOnly.FromDateTime(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)))
            .IsRequired();

        builder.Property(x => x.TotalOrders).IsRequired();
        builder.Property(x => x.DeliveredOrders).IsRequired();

        builder.Property(x => x.GrossMerchandiseVolume)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.TotalRefunds)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.NewCustomers).IsRequired();
        builder.Property(x => x.NewRestaurants).IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(x => x.UpdatedAtUtc)
            .HasDatabaseName("IX_AdminDailyPerformanceSeries_UpdatedAtUtc");
    }
}
