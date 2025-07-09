using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YummyZoom.Domain.Common.Models;

namespace YummyZoom.Infrastructure.Data.Configurations.Common;

/// <summary>
/// Extension methods for configuring audit properties in EF Core entity configurations
/// </summary>
public static class AuditableEntityConfiguration
{
    /// <summary>
    /// Configures audit properties for entities that implement ICreationAuditable
    /// </summary>
    /// <typeparam name="T">The entity type that implements ICreationAuditable</typeparam>
    /// <param name="builder">The entity type builder</param>
    public static void ConfigureCreationAuditProperties<T>(this EntityTypeBuilder<T> builder)
        where T : class, ICreationAuditable
    {
        builder.Property(e => e.Created)
            .IsRequired()
            .HasComment("Timestamp when the entity was created");

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(255)
            .HasComment("Identifier of who created the entity");

        // Create index for querying by creation time
        builder.HasIndex(e => e.Created)
            .HasDatabaseName($"IX_{typeof(T).Name}_Created");
    }

    /// <summary>
    /// Configures audit properties for entities that implement IModificationAuditable
    /// </summary>
    /// <typeparam name="T">The entity type that implements IModificationAuditable</typeparam>
    /// <param name="builder">The entity type builder</param>
    public static void ConfigureModificationAuditProperties<T>(this EntityTypeBuilder<T> builder)
        where T : class, IModificationAuditable
    {
        builder.Property(e => e.LastModified)
            .IsRequired()
            .HasComment("Timestamp when the entity was last modified");

        builder.Property(e => e.LastModifiedBy)
            .HasMaxLength(255)
            .HasComment("Identifier of who last modified the entity");

        // Create index for querying by last modified time
        builder.HasIndex(e => e.LastModified)
            .HasDatabaseName($"IX_{typeof(T).Name}_LastModified");
    }

    /// <summary>
    /// Configures audit properties for entities that implement IAuditableEntity
    /// </summary>
    /// <typeparam name="T">The entity type that implements IAuditableEntity</typeparam>
    /// <param name="builder">The entity type builder</param>
    public static void ConfigureAuditProperties<T>(this EntityTypeBuilder<T> builder)
        where T : class, IAuditableEntity
    {
        builder.ConfigureCreationAuditProperties();
        builder.ConfigureModificationAuditProperties();
    }

    /// <summary>
    /// Configures soft delete properties for entities that implement ISoftDeletableEntity
    /// </summary>
    /// <typeparam name="T">The entity type that implements ISoftDeletableEntity</typeparam>
    /// <param name="builder">The entity type builder</param>
    public static void ConfigureSoftDeleteProperties<T>(this EntityTypeBuilder<T> builder)
        where T : class, ISoftDeletableEntity
    {
        builder.Property(e => e.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment("Indicates if the entity is soft-deleted");

        builder.Property(e => e.DeletedOn)
            .IsRequired(false)
            .HasComment("Timestamp when the entity was deleted");

        builder.Property(e => e.DeletedBy)
            .HasMaxLength(255)
            .IsRequired(false)
            .HasComment("Identifier of who deleted the entity");

        // Create indexes for efficient querying
        builder.HasIndex(e => e.IsDeleted)
            .HasDatabaseName($"IX_{typeof(T).Name}_IsDeleted");

        builder.HasIndex(e => e.DeletedOn)
            .HasDatabaseName($"IX_{typeof(T).Name}_DeletedOn");
    }
}
