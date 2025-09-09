using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YummyZoom.Domain.TagEntity;
using YummyZoom.Domain.TagEntity.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore.Configurations.Common;

namespace YummyZoom.Infrastructure.Persistence.EfCore.Configurations;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags");

        // Primary key with strongly-typed ID
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .ValueGeneratedNever()
            .HasConversion(id => id.Value, value => TagId.Create(value));

        // Simple properties
        builder.Property(t => t.TagName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.TagDescription)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(t => t.TagCategory)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // Auditing & soft delete
        builder.ConfigureAuditProperties();
        builder.ConfigureSoftDeleteProperties();

        // Indexes
        builder.HasIndex(t => t.TagName)
            .HasDatabaseName("IX_Tags_TagName");
        builder.HasIndex(t => t.TagCategory)
            .HasDatabaseName("IX_Tags_TagCategory");
    }
}


