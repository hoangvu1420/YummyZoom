using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YummyZoom.Domain.TodoListAggregate;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore.Configurations.Common;

namespace YummyZoom.Infrastructure.Persistence.EfCore.Configurations;

public class TodoListConfiguration : IEntityTypeConfiguration<TodoList>
{
    public void Configure(EntityTypeBuilder<TodoList> builder)
    {
        ConfigureTodoListsTable(builder);
        ConfigureTodoItemsTable(builder);
    }

    private static void ConfigureTodoListsTable(EntityTypeBuilder<TodoList> builder)
    {
        builder.ToTable("TodoLists");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .ValueGeneratedNever()
            .HasConversion(
                id => id.Value,
                value => TodoListId.Create(value));

        builder.Property(t => t.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.OwnsOne(t => t.Color, cb =>
        {
            cb.Property(c => c.Code)
                .HasColumnName("Colour")
                .HasMaxLength(10)
                .IsRequired();
        });

        // Configure audit properties
        builder.ConfigureAuditProperties();
    }

    private static void ConfigureTodoItemsTable(EntityTypeBuilder<TodoList> builder)
    {
        builder.OwnsMany(t => t.Items, ib =>
        {
            ib.ToTable("TodoItems");

            ib.WithOwner().HasForeignKey("TodoListId");

            ib.HasKey("TodoListId", "Id");

            ib.Property(i => i.Id)
                .HasColumnName("TodoItemId")
                .ValueGeneratedNever()
                .HasConversion(
                    id => id.Value,
                    value => TodoItemId.Create(value));

            ib.Property(i => i.Title)
                .HasMaxLength(200);

            ib.Property(i => i.Note)
                .HasMaxLength(1000);

            ib.Property(i => i.Priority)
                .HasConversion<int>();

            ib.Property(i => i.Reminder);

            ib.Property(i => i.IsDone);
        });

        builder
            .Metadata
            .FindNavigation(nameof(TodoList.Items))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
