using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YummyZoom.Domain.PayoutAggregate;
using YummyZoom.Domain.PayoutAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAccountAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore.Configurations.Common;

namespace YummyZoom.Infrastructure.Persistence.EfCore.Configurations;

public class PayoutConfiguration : IEntityTypeConfiguration<Payout>
{
    public void Configure(EntityTypeBuilder<Payout> builder)
    {
        builder.ToTable("Payouts");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .ValueGeneratedNever()
            .HasConversion(id => id.Value, value => PayoutId.Create(value));

        builder.Property(p => p.RestaurantAccountId)
            .IsRequired()
            .HasConversion(id => id.Value, value => RestaurantAccountId.Create(value));

        builder.Property(p => p.RestaurantId)
            .IsRequired()
            .HasConversion(id => id.Value, value => RestaurantId.Create(value));

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.RequestedAt)
            .IsRequired();

        builder.Property(p => p.CompletedAt)
            .IsRequired(false);

        builder.Property(p => p.FailedAt)
            .IsRequired(false);

        builder.Property(p => p.ProviderReferenceId)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(p => p.FailureReason)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(p => p.IdempotencyKey)
            .HasMaxLength(200)
            .IsRequired();

        builder.OwnsOne(p => p.Amount, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount)
                .HasColumnName("Amount_Amount")
                .HasColumnType("decimal(18,2)");
            moneyBuilder.Property(m => m.Currency)
                .HasColumnName("Amount_Currency")
                .HasMaxLength(3);
        });

        builder.ConfigureCreationAuditProperties();

        builder.HasIndex(p => p.RestaurantId)
            .HasDatabaseName("IX_Payouts_RestaurantId");

        builder.HasIndex(p => p.RestaurantAccountId)
            .HasDatabaseName("IX_Payouts_RestaurantAccountId");

        builder.HasIndex(p => p.Status)
            .HasDatabaseName("IX_Payouts_Status");
    }
}
