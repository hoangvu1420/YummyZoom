using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.RoleAssignmentAggregate;
using YummyZoom.Domain.RoleAssignmentAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore.Configurations.Common;

namespace YummyZoom.Infrastructure.Persistence.EfCore.Configurations;

public class RoleAssignmentConfiguration : IEntityTypeConfiguration<RoleAssignment>
{
    public void Configure(EntityTypeBuilder<RoleAssignment> builder)
    {
        builder.ToTable("RoleAssignments");

        builder.HasKey(ra => ra.Id);

        // Configure RoleAssignmentId value object
        builder.Property(ra => ra.Id)
            .ValueGeneratedNever()
            .HasConversion(
                id => id.Value,
                value => RoleAssignmentId.Create(value));

        // Configure UserId value object
        builder.Property(ra => ra.UserId)
            .IsRequired()
            .HasConversion(
                id => id.Value,
                value => UserId.Create(value));

        // Configure RestaurantId value object
        builder.Property(ra => ra.RestaurantId)
            .IsRequired()
            .HasConversion(
                id => id.Value,
                value => RestaurantId.Create(value));

        // Configure RestaurantRole enum
        builder.Property(ra => ra.Role)
            .IsRequired()
            .HasConversion<string>(); // Store as string in database

        // Unique constraint: User cannot have the same role twice for the same restaurant
        builder.HasIndex(ra => new { ra.UserId, ra.RestaurantId, ra.Role })
            .IsUnique()
            .HasDatabaseName("IX_RoleAssignments_User_Restaurant_Role");

        // Index for querying by UserId (common lookup)
        builder.HasIndex(ra => ra.UserId)
            .HasDatabaseName("IX_RoleAssignments_UserId");

        // Index for querying by RestaurantId (common lookup)
        builder.HasIndex(ra => ra.RestaurantId)
            .HasDatabaseName("IX_RoleAssignments_RestaurantId");

        // Index for querying by Role (for admin queries)
        builder.HasIndex(ra => ra.Role)
            .HasDatabaseName("IX_RoleAssignments_Role");

        // Configure creation audit properties (RoleAssignment implements ICreationAuditable)
        builder.ConfigureCreationAuditProperties();
    }
}
