using YummyZoom.Domain.TodoListAggregate.Enums;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;

namespace YummyZoom.Domain.TodoListAggregate.Entities;

public class TodoItem : Entity<TodoItemId>
{
    public string? Title { get; private set; }
    public string? Note { get; private set; }
    public PriorityLevel Priority { get; private set; }
    public DateTime? Reminder { get; private set; }
    public bool IsDone { get; private set; } 

    // Private constructor for creating new instances
    private TodoItem(
        TodoItemId id,
        string? title,
        string? note,
        PriorityLevel priority,
        DateTime? reminder) : base(id) // Pass the ID to the base Entity constructor
    {
        Title = title;
        Note = note;
        Priority = priority;
        Reminder = reminder;
        IsDone = false;
    }

    // Factory method for creating a new TodoItem with a generated ID
    public static TodoItem Create(
        string? title,
        string? note,
        PriorityLevel priority,
        DateTime? reminder)
    {
        return new TodoItem(TodoItemId.CreateUnique(), title, note, priority, reminder);
    }

    // Factory method for creating a TodoItem with a specific ID (e.g., for hydration from persistence)
    public static TodoItem Create(
        TodoItemId id,
        string? title,
        string? note,
        PriorityLevel priority,
        DateTime? reminder)
    {
        return new TodoItem(id, title, note, priority, reminder);
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
    // For EF Core
    private TodoItem()
    {
    }
#pragma warning restore CS8618
}
