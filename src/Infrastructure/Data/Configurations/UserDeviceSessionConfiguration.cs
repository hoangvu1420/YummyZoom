using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YummyZoom.Application.Common.Models;

namespace YummyZoom.Infrastructure.Data.Configurations;

public class UserDeviceSessionConfiguration : IEntityTypeConfiguration<UserDeviceSession>
{
    public void Configure(EntityTypeBuilder<UserDeviceSession> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.UserId)
            .IsRequired();

        builder.Property(s => s.DeviceId)
            .IsRequired();

        builder.Property(s => s.FcmToken)
            .IsRequired();

        builder.Property(s => s.IsActive)
            .IsRequired();

        builder.Property(s => s.LastLoginAt)
            .IsRequired();

        // Add index for efficient querying of active sessions by device
        builder.HasIndex(s => new { s.DeviceId, s.IsActive });
    }
}
