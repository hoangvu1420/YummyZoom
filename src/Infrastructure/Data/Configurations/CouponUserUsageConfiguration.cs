using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YummyZoom.Infrastructure.Data.Models;

namespace YummyZoom.Infrastructure.Data.Configurations;

public class CouponUserUsageConfiguration : IEntityTypeConfiguration<CouponUserUsage>
{
    public void Configure(EntityTypeBuilder<CouponUserUsage> builder)
    {
        builder.ToTable("CouponUserUsages");

        builder.HasKey(x => new { x.CouponId, x.UserId });

        builder.Property(x => x.UsageCount)
            .IsRequired()
            .HasDefaultValue(0);
    }
}
