using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace YummyZoom.Infrastructure.Data.Configurations;

public class UserDeviceConfiguration : IEntityTypeConfiguration<UserDevice>
{
    public void Configure(EntityTypeBuilder<UserDevice> builder)
    {
        builder.ToTable("UserDevices");

        // Primary Key
        builder.HasKey(ud => ud.Id);
        
        builder.Property(ud => ud.Id)
            .ValueGeneratedNever()
            .IsRequired();

        // Simple Guid reference to ApplicationUser (no FK constraint for clean architecture)
        builder.Property(ud => ud.UserId)
            .IsRequired();

        // FCM Token - should be unique across the system
        builder.Property(ud => ud.FcmToken)
            .HasMaxLength(512)
            .IsRequired();

        // Platform information
        builder.Property(ud => ud.Platform)
            .HasMaxLength(50)
            .IsRequired();

        // Device identifier - optional
        builder.Property(ud => ud.DeviceId)
            .HasMaxLength(100)
            .IsRequired(false);

        // Timestamps
        builder.Property(ud => ud.RegisteredAt)
            .IsRequired();

        builder.Property(ud => ud.LastUsedAt)
            .IsRequired(false);

        builder.Property(ud => ud.UpdatedAt)
            .IsRequired();

        // Active flag
        builder.Property(ud => ud.IsActive)
            .IsRequired();

        // Indexes for performance
        
        // Unique index on FCM Token - each token should be unique globally
        builder.HasIndex(ud => ud.FcmToken)
            .IsUnique()
            .HasDatabaseName("IX_UserDevices_FcmToken");

        // Composite index for efficient user token queries (most common query pattern)
        builder.HasIndex(ud => new { ud.UserId, ud.IsActive })
            .HasDatabaseName("IX_UserDevices_UserId_IsActive");

        // Index on UserId alone for general user queries
        builder.HasIndex(ud => ud.UserId)
            .HasDatabaseName("IX_UserDevices_UserId");
    }
} 
