using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YummyZoom.Infrastructure.Persistence.ReadModels.MenuItemSales;

namespace YummyZoom.Infrastructure.Persistence.EfCore.Configurations;

public class MenuItemSalesSummaryConfiguration : IEntityTypeConfiguration<MenuItemSalesSummary>
{
    public void Configure(EntityTypeBuilder<MenuItemSalesSummary> builder)
    {
        builder.ToTable("MenuItemSalesSummaries");

        builder.HasKey(x => new { x.RestaurantId, x.MenuItemId });

        builder.Property(x => x.LifetimeQuantity)
            .IsRequired()
            .HasDefaultValue(0L);

        builder.Property(x => x.Rolling7DayQuantity)
            .IsRequired()
            .HasDefaultValue(0L);

        builder.Property(x => x.Rolling30DayQuantity)
            .IsRequired()
            .HasDefaultValue(0L);

        builder.Property(x => x.LastSoldAt);

        builder.Property(x => x.LastUpdatedAt)
            .IsRequired();

        builder.Property(x => x.SourceVersion)
            .IsRequired()
            .HasDefaultValue(0L);

        builder.HasIndex(x => x.LastUpdatedAt)
            .HasDatabaseName("IX_MenuItemSalesSummaries_LastUpdatedAt");

        builder.HasIndex(x => x.MenuItemId)
            .HasDatabaseName("IX_MenuItemSalesSummaries_MenuItemId");
    }
}
