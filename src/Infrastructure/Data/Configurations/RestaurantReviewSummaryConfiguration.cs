using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YummyZoom.Infrastructure.Data.Models;

namespace YummyZoom.Infrastructure.Data.Configurations;

public class RestaurantReviewSummaryConfiguration : IEntityTypeConfiguration<RestaurantReviewSummary>
{
    public void Configure(EntityTypeBuilder<RestaurantReviewSummary> builder)
    {
        builder.ToTable("RestaurantReviewSummaries");

        builder.HasKey(x => x.RestaurantId);

        builder.Property(x => x.AverageRating)
            .IsRequired()
            .HasDefaultValue(0.0);

        builder.Property(x => x.TotalReviews)
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasIndex(x => x.AverageRating)
            .HasDatabaseName("IX_RestaurantReviewSummaries_AverageRating");
    }
}
