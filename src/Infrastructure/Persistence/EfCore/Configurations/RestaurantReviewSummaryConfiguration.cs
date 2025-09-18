using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YummyZoom.Infrastructure.Persistence.ReadModels.Reviews;

namespace YummyZoom.Infrastructure.Persistence.EfCore.Configurations;

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

        // Rating distribution defaults
        builder.Property(x => x.Ratings1)
            .IsRequired()
            .HasDefaultValue(0);
        builder.Property(x => x.Ratings2)
            .IsRequired()
            .HasDefaultValue(0);
        builder.Property(x => x.Ratings3)
            .IsRequired()
            .HasDefaultValue(0);
        builder.Property(x => x.Ratings4)
            .IsRequired()
            .HasDefaultValue(0);
        builder.Property(x => x.Ratings5)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.TotalWithText)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.LastReviewAtUtc)
            .IsRequired(false);

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(x => x.AverageRating)
            .HasDatabaseName("IX_RestaurantReviewSummaries_AverageRating");
    }
}
