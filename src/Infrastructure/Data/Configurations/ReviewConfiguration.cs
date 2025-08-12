using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.ReviewAggregate;
using YummyZoom.Domain.ReviewAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.Infrastructure.Data.Configurations.Common;

namespace YummyZoom.Infrastructure.Data.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Reviews");

        // Primary key
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .ValueGeneratedNever()
            .HasConversion(id => id.Value, value => ReviewId.Create(value));

        // References to other aggregates (IDs only)
        builder.Property(r => r.OrderId)
            .IsRequired()
            .HasConversion(id => id.Value, value => OrderId.Create(value));

        builder.Property(r => r.CustomerId)
            .IsRequired()
            .HasConversion(id => id.Value, value => UserId.Create(value));

        builder.Property(r => r.RestaurantId)
            .IsRequired()
            .HasConversion(id => id.Value, value => RestaurantId.Create(value));

        // Value objects / simple properties
        builder.OwnsOne(r => r.Rating, ratingBuilder =>
        {
            ratingBuilder.Property(x => x.Value)
                .HasColumnName("Rating")
                .IsRequired();
        });

        builder.Property(r => r.Comment)
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.Property(r => r.SubmissionTimestamp)
            .IsRequired();

        builder.Property(r => r.IsModerated)
            .IsRequired();
        builder.Property(r => r.IsHidden)
            .IsRequired();

        builder.Property(r => r.Reply)
            .HasMaxLength(2000)
            .IsRequired(false);

        // Auditing & soft delete
        builder.ConfigureAuditProperties();
        builder.ConfigureSoftDeleteProperties();

        // Indexes
        builder.HasIndex(r => new { r.RestaurantId, r.SubmissionTimestamp })
            .HasDatabaseName("IX_Reviews_Restaurant_SubmissionTimestamp");
        builder.HasIndex(r => r.CustomerId)
            .HasDatabaseName("IX_Reviews_CustomerId");
        builder.HasIndex(r => r.OrderId)
            .HasDatabaseName("IX_Reviews_OrderId");
    }
}


