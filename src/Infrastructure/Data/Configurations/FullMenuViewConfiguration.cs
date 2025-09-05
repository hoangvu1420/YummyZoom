using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YummyZoom.Infrastructure.Data.Models;
using YummyZoom.Infrastructure.Data.ReadModels.FullMenu;

namespace YummyZoom.Infrastructure.Data.Configurations;

public class FullMenuViewConfiguration : IEntityTypeConfiguration<FullMenuView>
{
    public void Configure(EntityTypeBuilder<FullMenuView> builder)
    {
        builder.ToTable("FullMenuViews");

        builder.HasKey(x => x.RestaurantId);

        builder.Property(x => x.MenuJson)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(x => x.LastRebuiltAt)
            .IsRequired();

        builder.HasIndex(x => x.LastRebuiltAt)
            .HasDatabaseName("IX_FullMenuViews_LastRebuiltAt");
    }
}
