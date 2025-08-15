using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YummyZoom.Infrastructure.Data.Models;

namespace YummyZoom.Infrastructure.Data.Configurations;

public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
	public void Configure(EntityTypeBuilder<InboxMessage> b)
	{
		b.ToTable("InboxMessages");
		b.HasKey(x => new { x.EventId, x.Handler });

		b.Property(x => x.Handler).HasMaxLength(256).IsRequired();
		b.HasIndex(x => x.ProcessedOnUtc);
	}
}
