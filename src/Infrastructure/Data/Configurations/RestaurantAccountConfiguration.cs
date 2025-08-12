using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.RestaurantAccountAggregate;
using YummyZoom.Domain.RestaurantAccountAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Infrastructure.Data.Configurations.Common;

namespace YummyZoom.Infrastructure.Data.Configurations;

public class RestaurantAccountConfiguration : IEntityTypeConfiguration<RestaurantAccount>
{
    public void Configure(EntityTypeBuilder<RestaurantAccount> builder)
    {
        builder.ToTable("RestaurantAccounts");

        // Primary Key
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .ValueGeneratedNever()
            .HasConversion(id => id.Value, value => RestaurantAccountId.Create(value));

        // Reference to Restaurant aggregate (by ID only)
        builder.Property(a => a.RestaurantId)
            .IsRequired()
            .HasConversion(id => id.Value, value => RestaurantId.Create(value));

        // Money VO for current balance
        builder.OwnsOne(a => a.CurrentBalance, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount)
                .HasColumnName("CurrentBalance_Amount")
                .HasColumnType("decimal(18,2)");
            moneyBuilder.Property(m => m.Currency)
                .HasColumnName("CurrentBalance_Currency")
                .HasMaxLength(3);
        });

        // Optional payout method details VO
        builder.OwnsOne(a => a.PayoutMethodDetails, payoutBuilder =>
        {
            payoutBuilder.Property(p => p.Details)
                .HasColumnName("PayoutMethod_Details")
                .HasMaxLength(500)
                .IsRequired();
        });

        // Creation audit (ICreationAuditable)
        builder.ConfigureCreationAuditProperties();

        // Business rule: one account per restaurant
        builder.HasIndex(a => a.RestaurantId)
            .IsUnique()
            .HasDatabaseName("IX_RestaurantAccounts_RestaurantId");
    }
}


