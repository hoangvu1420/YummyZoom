using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YummyZoom.Domain.UserAggregate;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("DomainUsers"); 

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .ValueGeneratedNever()
            .HasConversion(
                id => id.Value,
                value => UserId.Create(value));

        builder.Property(u => u.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(u => u.Email)
            .HasMaxLength(255)
            .IsRequired();

        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.PhoneNumber)
            .HasMaxLength(50) 
            .IsRequired(false);

        // --- Configure owned collection of RoleAssignment Value Objects ---
        // This maps UserRoles to a separate UserRoles table
        builder.OwnsMany(u => u.UserRoles, roleBuilder =>
        {
            roleBuilder.ToTable("UserRoles");

            // Foreign key back to the Users table
            roleBuilder.WithOwner().HasForeignKey("UserId");

            // Properties of the RoleAssignment VO
            roleBuilder.Property(ur => ur.RoleName)
                .HasMaxLength(100)
                .IsRequired();

            roleBuilder.Property(ur => ur.TargetEntityId) 
                .HasMaxLength(100)
                .IsRequired(false); 

            roleBuilder.Property(ur => ur.TargetEntityType)
                .HasMaxLength(100)
                .IsRequired(false); 

            // Composite Primary Key for the UserRoles table
            // This ensures a user doesn't have the exact same role assignment twice.
            roleBuilder.HasKey("UserId", "RoleName");

            // Index for querying roles by TargetEntityId (e.g., find all owners of a restaurant)
            roleBuilder.HasIndex("TargetEntityId", "TargetEntityType", "RoleName");
        });

        // --- Configure owned collection of Address Value Objects ---
        // This maps Addresses to a separate UserAddresses table
        builder.OwnsMany(u => u.Addresses, addressBuilder =>
        {
            addressBuilder.ToTable("UserAddresses");
            addressBuilder.WithOwner().HasForeignKey("UserId");

            // Since Address is a value object without an ID property, we need a key for EF Core's owned entities
            // Using an index for the collection - EF Core will create a shadow property Id by default
            addressBuilder.UsePropertyAccessMode(PropertyAccessMode.Field);
            addressBuilder.HasKey("Id");

            // Configure Address properties
            addressBuilder.Property(a => a.Label).HasMaxLength(100).IsRequired(false);
            addressBuilder.Property(a => a.Street).HasMaxLength(255).IsRequired();
            addressBuilder.Property(a => a.City).HasMaxLength(100).IsRequired();
            addressBuilder.Property(a => a.State).HasMaxLength(100).IsRequired(false);
            addressBuilder.Property(a => a.ZipCode).HasMaxLength(20).IsRequired();
            addressBuilder.Property(a => a.Country).HasMaxLength(100).IsRequired();
            addressBuilder.Property(a => a.DeliveryInstructions).HasMaxLength(500).IsRequired(false);
        });

        // --- Configure owned collection of PaymentMethod Child Entities ---
        builder.OwnsMany(u => u.PaymentMethods, paymentBuilder =>
        {
            paymentBuilder.ToTable("UserPaymentMethods");
            paymentBuilder.WithOwner().HasForeignKey("UserId"); // Establishes the FK to Users table

            // PaymentMethod has its own Id, which is its primary key in this table.
            // Assuming PaymentMethodId is globally unique.
            paymentBuilder.HasKey(pm => pm.Id);

            paymentBuilder.Property(i => i.Id)
                .HasColumnName("PaymentMethodId")
                .ValueGeneratedNever()
                .HasConversion(
                    id => id.Value,
                    value => PaymentMethodId.Create(value));

            paymentBuilder.Property(pm => pm.Type)
                .HasMaxLength(50)
                .IsRequired();

            paymentBuilder.Property(pm => pm.TokenizedDetails)
                .HasMaxLength(500) 
                .IsRequired();

            paymentBuilder.Property(pm => pm.IsDefault)
                .IsRequired();
        });
    }
}
