using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YummyZoom.Domain.RestaurantAggregate;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Infrastructure.Data.Configurations.Common;

namespace YummyZoom.Infrastructure.Data.Configurations;

public class RestaurantConfiguration : IEntityTypeConfiguration<Restaurant>
{
    public void Configure(EntityTypeBuilder<Restaurant> builder)
    {
        builder.ToTable("Restaurants");

        // --- 1. Primary Key ---
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .ValueGeneratedNever()
            .HasConversion(id => id.Value, value => RestaurantId.Create(value));

        // --- 2. Simple Properties ---
        builder.Property(r => r.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.LogoUrl)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(r => r.Description)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(r => r.CuisineType)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.IsVerified)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(r => r.IsAcceptingOrders)
            .IsRequired()
            .HasDefaultValue(false);

        // --- 3. Owned Value Objects ---
        
        // Map the Location (Address) Value Object using OwnsOne
        // This will create columns like Location_Street, Location_City etc. in the Restaurants table
        builder.OwnsOne(r => r.Location, locationBuilder =>
        {
            locationBuilder.Property(a => a.Street)
                .HasColumnName("Location_Street")
                .HasMaxLength(255)
                .IsRequired();
            locationBuilder.Property(a => a.City)
                .HasColumnName("Location_City")
                .HasMaxLength(100)
                .IsRequired();
            locationBuilder.Property(a => a.State)
                .HasColumnName("Location_State")
                .HasMaxLength(100)
                .IsRequired();
            locationBuilder.Property(a => a.ZipCode)
                .HasColumnName("Location_ZipCode")
                .HasMaxLength(20)
                .IsRequired();
            locationBuilder.Property(a => a.Country)
                .HasColumnName("Location_Country")
                .HasMaxLength(100)
                .IsRequired();
        });

        // Map the ContactInfo Value Object
        builder.OwnsOne(r => r.ContactInfo, contactBuilder =>
        {
            contactBuilder.Property(c => c.PhoneNumber)
                .HasColumnName("ContactInfo_PhoneNumber")
                .HasMaxLength(50)
                .IsRequired();
            contactBuilder.Property(c => c.Email)
                .HasColumnName("ContactInfo_Email")
                .HasMaxLength(255)
                .IsRequired();
        });

        // Map BusinessHours Value Object
        // Since BusinessHours is a simple string-based VO, we can map it directly
        builder.OwnsOne(r => r.BusinessHours, businessHoursBuilder =>
        {
            businessHoursBuilder.Property(bh => bh.Hours)
                .HasColumnName("BusinessHours")
                .HasMaxLength(200)
                .IsRequired();
        });

        // --- 4. Indexes for Performance ---
        builder.HasIndex(r => r.Name)
            .HasDatabaseName("IX_Restaurants_Name");

        builder.HasIndex(r => r.CuisineType)
            .HasDatabaseName("IX_Restaurants_CuisineType");

        builder.HasIndex(r => r.IsVerified)
            .HasDatabaseName("IX_Restaurants_IsVerified");

        builder.HasIndex(r => r.IsAcceptingOrders)
            .HasDatabaseName("IX_Restaurants_IsAcceptingOrders");

        // Composite index for common queries
        builder.HasIndex(r => new { r.IsVerified, r.IsAcceptingOrders })
            .HasDatabaseName("IX_Restaurants_Verified_AcceptingOrders");

        // --- 5. Auditing & Soft Delete Properties ---
        builder.ConfigureAuditProperties();
        builder.ConfigureSoftDeleteProperties();
    }
}