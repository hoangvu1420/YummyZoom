using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YummyZoom.Application.Common.Models;

namespace YummyZoom.Infrastructure.Persistence.EfCore.Configurations;

public class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.DeviceId)
            .IsRequired(false);

        builder.HasIndex(d => d.DeviceId)
            .IsUnique()
            .HasFilter("\"DeviceId\" IS NOT NULL");

        builder.Property(d => d.Platform)
            .IsRequired();

        builder.Property(d => d.CreatedAt)
            .IsRequired();

        builder.Property(d => d.UpdatedAt)
            .IsRequired();
    }
}
