using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YummyZoom.Infrastructure.Data.Outbox;

namespace YummyZoom.Infrastructure.Data.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
	public void Configure(EntityTypeBuilder<OutboxMessage> b)
	{
		b.ToTable("OutboxMessages");
		b.HasKey(x => x.Id);

		b.Property(x => x.Type).IsRequired().HasMaxLength(512);
		b.Property(x => x.Content).IsRequired();
		b.Property(x => x.Attempt).IsRequired();

		b.HasIndex(x => x.ProcessedOnUtc);
		b.HasIndex(x => x.NextAttemptOnUtc);
		b.HasIndex(x => x.OccurredOnUtc);

		b.Property(x => x.Content).HasColumnType("jsonb");
	}
}
