using System.Text.Json.Serialization;
using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.TodoListAggregate.Enums;
using YummyZoom.Domain.TodoListAggregate.Events;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;

namespace YummyZoom.Domain.TodoListAggregate.Entities;

public class TodoItem : Entity<TodoItemId>, IAuditableEntity
{
    public string? Title { get; private set; }
    public string? Note { get; private set; }
    public PriorityLevel Priority { get; private set; }
    public DateTime? Reminder { get; private set; }
    public bool IsDone { get; private set; }

    // Properties from IAuditableEntity
    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public string? LastModifiedBy { get; set; } 

    // Private constructor for creating new instances
    [JsonConstructor]
    private TodoItem(
        TodoItemId id,
        string? title,
        string? note,
        PriorityLevel priority,
        DateTime? reminder,
        bool isDone) : base(id) // Pass the ID to the base Entity constructor
    {
        Title = title;
        Note = note;
        Priority = priority;
        Reminder = reminder;
        IsDone = isDone;
    }

    // Factory method for creating a new TodoItem with a generated ID
    public static TodoItem Create(
        string? title,
        string? note,
        PriorityLevel priority,
        DateTime? reminder)
    {
        var todoItem = new TodoItem(TodoItemId.CreateUnique(), title, note, priority, reminder, false);
        todoItem.AddDomainEvent(new TodoItemCreatedEvent(todoItem));
        return todoItem;
    }

    // Factory method for creating a TodoItem with a specific ID (e.g., for hydration from persistence)
    public static TodoItem Create(
        TodoItemId id,
        string? title,
        string? note,
        PriorityLevel priority,
        DateTime? reminder)
    {
        return new TodoItem(id, title, note, priority, reminder, false);
    }

    public void UpdateTitle(string title)
    {
        Title = title;
    }

    public void UpdateNote(string? note)
    {
        Note = note;
    }

    public void UpdatePriority(PriorityLevel priority)
    {
        Priority = priority;
    }

    public void UpdateReminder(DateTime? reminder)
    {
        Reminder = reminder;
    }

    public void Complete()
    {
        if (!IsDone)
        {
            IsDone = true;
            AddDomainEvent(new TodoItemCompletedEvent(this));
        }
    }

    public void Incomplete()
    {
        if (IsDone)
        {
            IsDone = false;
        }
    }

#pragma warning disable CS8618
    // Internal parameterless constructor for EF Core and JSON deserialization
    internal TodoItem()
    {
    }
#pragma warning restore CS8618
}
