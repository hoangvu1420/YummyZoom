using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YummyZoom.Domain.RestaurantRegistrationAggregate;
using YummyZoom.Domain.RestaurantRegistrationAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Infrastructure.Persistence.EfCore.Configurations;

public sealed class RestaurantRegistrationConfiguration : IEntityTypeConfiguration<RestaurantRegistration>
{
    public void Configure(EntityTypeBuilder<RestaurantRegistration> builder)
    {
        builder.ToTable("RestaurantRegistrations");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedNever()
            .HasConversion(id => id.Value, v => RestaurantRegistrationId.Create(v));

        builder.Property(r => r.SubmitterUserId)
            .IsRequired()
            .HasConversion(id => id.Value, v => UserId.Create(v));

        builder.Property(r => r.Name).IsRequired().HasMaxLength(100);
        builder.Property(r => r.Description).IsRequired().HasMaxLength(500);
        builder.Property(r => r.CuisineType).IsRequired().HasMaxLength(50);

        builder.Property(r => r.Street).IsRequired().HasMaxLength(200);
        builder.Property(r => r.City).IsRequired().HasMaxLength(100);
        builder.Property(r => r.State).IsRequired().HasMaxLength(100);
        builder.Property(r => r.ZipCode).IsRequired().HasMaxLength(20);
        builder.Property(r => r.Country).IsRequired().HasMaxLength(100);

        builder.Property(r => r.PhoneNumber).IsRequired().HasMaxLength(30);
        builder.Property(r => r.Email).IsRequired().HasMaxLength(320);
        builder.Property(r => r.BusinessHours).IsRequired().HasMaxLength(200);
        builder.Property(r => r.LogoUrl).HasMaxLength(2048);

        builder.Property(r => r.Latitude);
        builder.Property(r => r.Longitude);

        builder.Property(r => r.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(r => r.SubmittedAtUtc).IsRequired();
        builder.Property(r => r.ReviewedAtUtc);
        builder.Property(r => r.ReviewNote).HasMaxLength(500);
        builder.Property(r => r.ReviewedByUserId)
            .HasConversion(id => id!.Value, v => UserId.Create(v))
            .IsRequired(false);

        builder.HasIndex(r => r.Status).HasDatabaseName("IX_RestaurantRegistrations_Status");
        builder.HasIndex(r => new { r.SubmitterUserId, r.Name, r.City }).HasDatabaseName("IX_RestaurantRegistrations_Submitter_Name_City");
        builder.HasIndex(r => r.SubmittedAtUtc).HasDatabaseName("IX_RestaurantRegistrations_SubmittedAtUtc");
    }
}

