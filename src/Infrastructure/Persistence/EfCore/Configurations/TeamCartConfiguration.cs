using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore.Configurations.Common;

namespace YummyZoom.Infrastructure.Persistence.EfCore.Configurations;

public class TeamCartConfiguration : IEntityTypeConfiguration<TeamCart>
{
    public void Configure(EntityTypeBuilder<TeamCart> builder)
    {
        builder.ToTable("TeamCarts");

        // Primary key
        builder.HasKey(tc => tc.Id);
        builder.Property(tc => tc.Id)
            .ValueGeneratedNever()
            .HasConversion(id => id.Value, value => TeamCartId.Create(value));

        // References to other aggregates by ID only
        builder.Property(tc => tc.RestaurantId)
            .IsRequired()
            .HasConversion(id => id.Value, value => RestaurantId.Create(value));
        builder.Property(tc => tc.HostUserId)
            .IsRequired()
            .HasConversion(id => id.Value, value => UserId.Create(value));

        // Simple properties & VOs
        builder.Property(tc => tc.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(tc => tc.Deadline)
            .IsRequired(false);
        builder.Property(tc => tc.CreatedAt)
            .IsRequired();
        builder.Property(tc => tc.ExpiresAt)
            .IsRequired();

        builder.OwnsOne(tc => tc.ShareToken, tokenBuilder =>
        {
            tokenBuilder.Property(t => t.Value)
                .HasColumnName("ShareToken_Value")
                .HasMaxLength(50)
                .IsRequired();
            tokenBuilder.Property(t => t.ExpiresAt)
                .HasColumnName("ShareToken_ExpiresAt")
                .IsRequired();
        });

        builder.OwnsOne(tc => tc.TipAmount, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount)
                .HasColumnName("TipAmount_Amount")
                .HasColumnType("decimal(18,2)");
            moneyBuilder.Property(m => m.Currency)
                .HasColumnName("TipAmount_Currency")
                .HasMaxLength(3);
        });

        builder.Property(tc => tc.AppliedCouponId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value.HasValue ? CouponId.Create(value.Value) : null);

        // Child collections
        ConfigureMembers(builder);
        ConfigureItems(builder);
        ConfigureMemberPayments(builder);

        // Creation audit
        builder.ConfigureCreationAuditProperties();

        // Quote Lite persistence
        builder.Property(tc => tc.QuoteVersion)
            .IsRequired();

        builder.OwnsOne(tc => tc.GrandTotal, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount)
                .HasColumnName("GrandTotal_Amount")
                .HasColumnType("decimal(18,2)");
            moneyBuilder.Property(m => m.Currency)
                .HasColumnName("GrandTotal_Currency")
                .HasMaxLength(3);
        });

        builder.Property(tc => tc.MemberTotalsRows)
            .HasColumnName("MemberTotals")
            .HasJsonbListConversion();

        // Field access for collections to respect encapsulation
        builder.Metadata.FindNavigation(nameof(TeamCart.Members))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(TeamCart.Items))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(TeamCart.MemberPayments))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigureMembers(EntityTypeBuilder<TeamCart> builder)
    {
        builder.OwnsMany(tc => tc.Members, memberBuilder =>
        {
            memberBuilder.ToTable("TeamCartMembers");
            memberBuilder.WithOwner().HasForeignKey("TeamCartId");

            memberBuilder.HasKey("TeamCartId", "Id");
            memberBuilder.Property(m => m.Id)
                .HasColumnName("TeamCartMemberId")
                .ValueGeneratedNever()
                .HasConversion(id => id.Value, value => TeamCartMemberId.Create(value));

            memberBuilder.Property(m => m.UserId)
                .HasConversion(id => id.Value, value => UserId.Create(value))
                .IsRequired();
            memberBuilder.Property(m => m.Name)
                .HasMaxLength(200)
                .IsRequired();
            memberBuilder.Property(m => m.Role)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();
        });
    }

    private static void ConfigureItems(EntityTypeBuilder<TeamCart> builder)
    {
        builder.OwnsMany(tc => tc.Items, itemBuilder =>
        {
            itemBuilder.ToTable("TeamCartItems");
            itemBuilder.WithOwner().HasForeignKey("TeamCartId");

            itemBuilder.HasKey("TeamCartId", "Id");
            itemBuilder.Property(i => i.Id)
                .HasColumnName("TeamCartItemId")
                .ValueGeneratedNever()
                .HasConversion(id => id.Value, value => TeamCartItemId.Create(value));

            itemBuilder.Property(i => i.AddedByUserId)
                .HasConversion(id => id.Value, value => UserId.Create(value));
            itemBuilder.Property(i => i.Snapshot_MenuItemId)
                .HasConversion(id => id.Value, value => MenuItemId.Create(value));
            itemBuilder.Property(i => i.Snapshot_MenuCategoryId)
                .HasConversion(id => id.Value, value => MenuCategoryId.Create(value));
            itemBuilder.Property(i => i.Snapshot_ItemName)
                .HasMaxLength(200)
                .IsRequired();
            itemBuilder.Property(i => i.Quantity)
                .IsRequired();

            itemBuilder.OwnsOne(i => i.Snapshot_BasePriceAtOrder, moneyBuilder =>
            {
                moneyBuilder.Property(m => m.Amount)
                    .HasColumnName("BasePrice_Amount")
                    .HasColumnType("decimal(18,2)");
                moneyBuilder.Property(m => m.Currency)
                    .HasColumnName("BasePrice_Currency")
                    .HasMaxLength(3);
            });
            itemBuilder.OwnsOne(i => i.LineItemTotal, moneyBuilder =>
            {
                moneyBuilder.Property(m => m.Amount)
                    .HasColumnName("LineItemTotal_Amount")
                    .HasColumnType("decimal(18,2)");
                moneyBuilder.Property(m => m.Currency)
                    .HasColumnName("LineItemTotal_Currency")
                    .HasMaxLength(3);
            });

            // Selected customizations as JSONB
            itemBuilder.Property(i => i.SelectedCustomizations)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<TeamCartItemCustomization>>(v, (JsonSerializerOptions?)null)!,
                    new ValueComparer<IReadOnlyList<TeamCartItemCustomization>>(
                        (c1, c2) => c1!.SequenceEqual(c2!),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList()
                    )
                );
        });
    }

    private static void ConfigureMemberPayments(EntityTypeBuilder<TeamCart> builder)
    {
        builder.OwnsMany(tc => tc.MemberPayments, paymentBuilder =>
        {
            paymentBuilder.ToTable("TeamCartMemberPayments");
            paymentBuilder.WithOwner().HasForeignKey("TeamCartId");

            paymentBuilder.HasKey("TeamCartId", "Id");
            paymentBuilder.Property(p => p.Id)
                .HasColumnName("MemberPaymentId")
                .ValueGeneratedNever()
                .HasConversion(id => id.Value, value => MemberPaymentId.Create(value));

            paymentBuilder.Property(p => p.UserId)
                .HasConversion(id => id.Value, value => UserId.Create(value))
                .IsRequired();

            paymentBuilder.OwnsOne(p => p.Amount, moneyBuilder =>
            {
                moneyBuilder.Property(m => m.Amount)
                    .HasColumnName("Payment_Amount")
                    .HasColumnType("decimal(18,2)");
                moneyBuilder.Property(m => m.Currency)
                    .HasColumnName("Payment_Currency")
                    .HasMaxLength(3);
            });

            paymentBuilder.Property(p => p.Method)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();
            paymentBuilder.Property(p => p.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();
            paymentBuilder.Property(p => p.OnlineTransactionId)
                .HasMaxLength(255)
                .IsRequired(false);
            paymentBuilder.Property(p => p.CreatedAt)
                .IsRequired();
            paymentBuilder.Property(p => p.UpdatedAt)
                .IsRequired();
        });
    }
}

