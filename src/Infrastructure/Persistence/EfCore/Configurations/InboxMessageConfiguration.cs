using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YummyZoom.Infrastructure.Persistence.EfCore.Models;

namespace YummyZoom.Infrastructure.Persistence.EfCore.Configurations;

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
