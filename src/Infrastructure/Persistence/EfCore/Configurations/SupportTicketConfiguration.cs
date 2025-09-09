using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YummyZoom.Domain.SupportTicketAggregate;
using YummyZoom.Domain.SupportTicketAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore.Configurations.Common;

namespace YummyZoom.Infrastructure.Persistence.EfCore.Configurations;

public class SupportTicketConfiguration : IEntityTypeConfiguration<SupportTicket>
{
    public void Configure(EntityTypeBuilder<SupportTicket> builder)
    {
        builder.ToTable("SupportTickets");

        // Primary key
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .ValueGeneratedNever()
            .HasConversion(id => id.Value, value => SupportTicketId.Create(value));

        // Ticket number value object
        builder.OwnsOne(t => t.TicketNumber, tnBuilder =>
        {
            tnBuilder.Property(n => n.Value)
                .HasColumnName("TicketNumber")
                .HasMaxLength(50)
                .IsRequired();

            // Unique index on ticket number value
            tnBuilder.HasIndex(n => n.Value).IsUnique();
        });

        // Simple properties & enums
        builder.Property(t => t.Subject)
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(t => t.Priority)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(t => t.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.SubmissionTimestamp).IsRequired();
        builder.Property(t => t.LastUpdateTimestamp).IsRequired();

        builder.Property(t => t.AssignedToAdminId)
            .IsRequired(false);

        // Owned value object collection: ContextLinks
        builder.OwnsMany(t => t.ContextLinks, clBuilder =>
        {
            clBuilder.ToTable("SupportTicketContextLinks");
            clBuilder.WithOwner().HasForeignKey("SupportTicketId");

            clBuilder.HasKey("SupportTicketId", "EntityID", "EntityType");

            clBuilder.Property(c => c.EntityType)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            clBuilder.Property(c => c.EntityID)
                .IsRequired();
        });

        // Owned child entity collection: Messages
        builder.OwnsMany(t => t.Messages, msgBuilder =>
        {
            msgBuilder.ToTable("SupportTicketMessages");
            msgBuilder.WithOwner().HasForeignKey("SupportTicketId");

            msgBuilder.HasKey("SupportTicketId", "Id");
            msgBuilder.Property(m => m.Id)
                .HasColumnName("MessageId")
                .ValueGeneratedNever()
                .HasConversion(id => id.Value, value => MessageId.Create(value));

            msgBuilder.Property(m => m.AuthorId).IsRequired();
            msgBuilder.Property(m => m.AuthorType)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();
            msgBuilder.Property(m => m.MessageText)
                .HasMaxLength(5000)
                .IsRequired();
            msgBuilder.Property(m => m.Timestamp)
                .IsRequired();
            msgBuilder.Property(m => m.IsInternalNote)
                .IsRequired();
        });

        // Creation audit (ICreationAuditable)
        builder.ConfigureCreationAuditProperties();

        // Field access for collections
        builder.Metadata.FindNavigation(nameof(SupportTicket.ContextLinks))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(SupportTicket.Messages))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        // Indexes
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.Priority);
        builder.HasIndex(t => t.AssignedToAdminId);
        builder.HasIndex(t => t.SubmissionTimestamp);
        builder.HasIndex(t => t.LastUpdateTimestamp);
    }
}


