using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.OrderAggregate;
using YummyZoom.Domain.OrderAggregate.Entities;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.Infrastructure.Data.Configurations.Common;

namespace YummyZoom.Infrastructure.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        // --- 1. Primary Key ---
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id)
            .ValueGeneratedNever()
            .HasConversion(id => id.Value, value => OrderId.Create(value));

        // --- 2. Simple Properties & Enums ---
        builder.Property(o => o.OrderNumber)
            .HasMaxLength(50)
            .IsRequired();
        builder.HasIndex(o => o.OrderNumber)
            .IsUnique();

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(o => o.PlacementTimestamp)
            .IsRequired();
        builder.Property(o => o.SpecialInstructions)
            .IsRequired(false); // Can be null or empty

        // --- 3. References to Other Aggregates (by ID) ---
        builder.Property(o => o.CustomerId)
            .IsRequired()
            .HasConversion(id => id.Value, value => UserId.Create(value));
        builder.Property(o => o.RestaurantId)
            .IsRequired()
            .HasConversion(id => id.Value, value => RestaurantId.Create(value));

        // Nullable references.
        builder.Property(o => o.SourceTeamCartId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value.HasValue ? TeamCartId.Create(value.Value) : null
            );

        builder.Property(o => o.AppliedCouponId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value.HasValue ? CouponId.Create(value.Value) : null
            );

        // --- 4. Owned Value Objects ---
        builder.OwnsOne(o => o.DeliveryAddress, addressBuilder =>
        {
            addressBuilder.Property(a => a.Street)
                .HasMaxLength(255);
            addressBuilder.Property(a => a.City)
                .HasMaxLength(100);
            addressBuilder.Property(a => a.State)
                .HasMaxLength(100);
            addressBuilder.Property(a => a.ZipCode)
                .HasMaxLength(20);
            addressBuilder.Property(a => a.Country)
                .HasMaxLength(100);
        });

        // Money VOs.
        builder.OwnsOne(o => o.Subtotal, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount)
                .HasColumnName("Subtotal_Amount")
                .HasColumnType("decimal(18,2)");
            moneyBuilder.Property(m => m.Currency)
                .HasColumnName("Subtotal_Currency")
                .HasMaxLength(3);
        });
        builder.OwnsOne(o => o.DiscountAmount, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount)
                .HasColumnName("DiscountAmount_Amount")
                .HasColumnType("decimal(18,2)");
            moneyBuilder.Property(m => m.Currency)
                .HasColumnName("DiscountAmount_Currency")
                .HasMaxLength(3);
        });
        builder.OwnsOne(o => o.DeliveryFee, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount)
                .HasColumnName("DeliveryFee_Amount")
                .HasColumnType("decimal(18,2)");
            moneyBuilder.Property(m => m.Currency)
                .HasColumnName("DeliveryFee_Currency")
                .HasMaxLength(3);
        });
        builder.OwnsOne(o => o.TipAmount, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount)
                .HasColumnName("TipAmount_Amount")
                .HasColumnType("decimal(18,2)");
            moneyBuilder.Property(m => m.Currency)
                .HasColumnName("TipAmount_Currency")
                .HasMaxLength(3);
        });
        builder.OwnsOne(o => o.TaxAmount, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount)
                .HasColumnName("TaxAmount_Amount")
                .HasColumnType("decimal(18,2)");
            moneyBuilder.Property(m => m.Currency)
                .HasColumnName("TaxAmount_Currency")
                .HasMaxLength(3);
        });
        builder.OwnsOne(o => o.TotalAmount, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount)
                .HasColumnName("TotalAmount_Amount")
                .HasColumnType("decimal(18,2)");
            moneyBuilder.Property(m => m.Currency)
                .HasColumnName("TotalAmount_Currency")
                .HasMaxLength(3);
        });

        // --- 5. Child Entity Collections ---
        ConfigureOrderItems(builder);
        ConfigurePaymentTransactions(builder);

        // --- 6. Auditing & Field Access ---
        builder.ConfigureCreationAuditProperties();

        // Use field access for collections to enforce encapsulation.
        builder.Metadata.FindNavigation(nameof(Order.OrderItems))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Order.PaymentTransactions))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }

    private void ConfigureOrderItems(EntityTypeBuilder<Order> builder)
    {
        builder.OwnsMany(o => o.OrderItems, itemBuilder =>
        {
            itemBuilder.ToTable("OrderItems");
            itemBuilder.WithOwner().HasForeignKey("OrderId");

            itemBuilder.HasKey("OrderId", "Id");
            itemBuilder.HasKey(oi => oi.Id);
            itemBuilder.Property(oi => oi.Id)
                .HasColumnName("OrderItemId")
                .ValueGeneratedNever()
                .HasConversion(id => id.Value, value => OrderItemId.Create(value));

            // Configure snapshot properties
            itemBuilder.Property(oi => oi.Snapshot_MenuCategoryId)
                .HasConversion(id => id.Value, value => MenuCategoryId.Create(value));
            itemBuilder.Property(oi => oi.Snapshot_MenuItemId)
                .HasConversion(id => id.Value, value => MenuItemId.Create(value));
            itemBuilder.Property(oi => oi.Snapshot_ItemName)
                .HasMaxLength(200)
                .IsRequired();
            itemBuilder.Property(oi => oi.Quantity)
                .IsRequired();

            itemBuilder.OwnsOne(oi => oi.Snapshot_BasePriceAtOrder, moneyBuilder =>
            {
                moneyBuilder.Property(m => m.Amount)
                    .HasColumnName("BasePrice_Amount")
                    .HasColumnType("decimal(18,2)");
                moneyBuilder.Property(m => m.Currency)
                    .HasColumnName("BasePrice_Currency")
                    .HasMaxLength(3);
            });
            itemBuilder.OwnsOne(oi => oi.LineItemTotal, moneyBuilder =>
            {
                moneyBuilder.Property(m => m.Amount)
                    .HasColumnName("LineItemTotal_Amount")
                    .HasColumnType("decimal(18,2)");
                moneyBuilder.Property(m => m.Currency)
                    .HasColumnName("LineItemTotal_Currency")
                    .HasMaxLength(3);
            });

            // Configure collection of Value Objects as a JSONB column.
            itemBuilder.Property(oi => oi.SelectedCustomizations)
                .HasColumnType("jsonb") 
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<OrderItemCustomization>>(v, (JsonSerializerOptions?)null)!,
                    new ValueComparer<IReadOnlyList<OrderItemCustomization>>(
                        (c1, c2) => c1!.SequenceEqual(c2!),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList()
                    )
                );
        });
    }

    private void ConfigurePaymentTransactions(EntityTypeBuilder<Order> builder)
    {
        builder.OwnsMany(o => o.PaymentTransactions, ptBuilder =>
        {
            ptBuilder.ToTable("PaymentTransactions");
            ptBuilder.WithOwner().HasForeignKey("OrderId");

            ptBuilder.HasKey("OrderId", "Id");
            ptBuilder.Property(pt => pt.Id)
                .HasColumnName("PaymentTransactionId")
                .ValueGeneratedNever()
                .HasConversion(id => id.Value, value => PaymentTransactionId.Create(value));

            ptBuilder.Property(pt => pt.PaymentMethodType)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();
            ptBuilder.Property(pt => pt.PaymentMethodDisplay)
                .HasMaxLength(100)
                .IsRequired(false);
            ptBuilder.Property(pt => pt.Type)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();
            ptBuilder.Property(pt => pt.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();
            ptBuilder.Property(pt => pt.Timestamp)
                .IsRequired();
            ptBuilder.Property(pt => pt.PaymentGatewayReferenceId)
                .HasMaxLength(255)
                .IsRequired(false);

            ptBuilder.OwnsOne(pt => pt.Amount, moneyBuilder =>
            {
                moneyBuilder.Property(m => m.Amount)
                    .HasColumnName("Transaction_Amount")
                    .HasColumnType("decimal(18,2)");
                moneyBuilder.Property(m => m.Currency)
                    .HasColumnName("Transaction_Currency")
                    .HasMaxLength(3);
            });

            ptBuilder.Property(pt => pt.PaidByUserId)
                .HasConversion(
                    id => id == null ? (Guid?)null : id.Value,
                    value => value.HasValue ? UserId.Create(value.Value) : null
                );
        });
    }
}
