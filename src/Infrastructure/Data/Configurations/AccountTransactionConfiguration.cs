using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YummyZoom.Domain.AccountTransactionEntity;
using YummyZoom.Domain.AccountTransactionEntity.ValueObjects;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAccountAggregate.ValueObjects;
using YummyZoom.Infrastructure.Data.Configurations.Common;

namespace YummyZoom.Infrastructure.Data.Configurations;

public class AccountTransactionConfiguration : IEntityTypeConfiguration<AccountTransaction>
{
    public void Configure(EntityTypeBuilder<AccountTransaction> builder)
    {
        builder.ToTable("AccountTransactions");

        // Primary key
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .ValueGeneratedNever()
            .HasConversion(id => id.Value, value => AccountTransactionId.Create(value));

        // Foreign aggregate references by ID only
        builder.Property(t => t.RestaurantAccountId)
            .IsRequired()
            .HasConversion(id => id.Value, value => RestaurantAccountId.Create(value));

        builder.Property(t => t.RelatedOrderId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value.HasValue ? OrderId.Create(value.Value) : null);

        // Enum as string
        builder.Property(t => t.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // Money VO
        builder.OwnsOne(t => t.Amount, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount)
                .HasColumnName("Amount")
                .HasColumnType("decimal(18,2)");
            moneyBuilder.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3);
        });

        // Other properties
        builder.Property(t => t.Timestamp)
            .IsRequired();
        builder.Property(t => t.Notes)
            .HasMaxLength(500)
            .IsRequired(false);

        // Creation audit
        builder.ConfigureCreationAuditProperties();

        // Indexes
        builder.HasIndex(t => t.RestaurantAccountId)
            .HasDatabaseName("IX_AccountTransactions_RestaurantAccountId");
        builder.HasIndex(t => t.Timestamp)
            .HasDatabaseName("IX_AccountTransactions_Timestamp");
    }
}


