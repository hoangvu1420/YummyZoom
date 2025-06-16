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

        // --- Configure owned collection of Address Entities ---
        // This maps Addresses to a separate UserAddresses table
        builder.OwnsMany(u => u.Addresses, addressBuilder =>
        {
            addressBuilder.ToTable("UserAddresses");
            addressBuilder.WithOwner().HasForeignKey("UserId");

            // Address is an Entity with AddressId, so we need to configure the key properly
            addressBuilder.HasKey(a => a.Id);

            // Configure AddressId value conversion
            addressBuilder.Property(a => a.Id)
                .HasColumnName("AddressId")
                .ValueGeneratedNever()
                .HasConversion(
                    id => id.Value,
                    value => AddressId.Create(value));

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
