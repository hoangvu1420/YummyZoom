﻿using YummyZoom.Domain.TodoListAggregate.Entities;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;

namespace YummyZoom.Domain.TodoListAggregate;

public class TodoList : AggregateRoot<TodoListId, Guid>
{
    private readonly List<TodoItem> _items = [];

    public string? Title { get; private set; }
    public Color Color { get; private set; } 
    public IReadOnlyList<TodoItem> Items => _items.AsReadOnly();

    // Private constructor for creating new instances
    private TodoList(
        TodoListId id,
        string title,
        Color colour) : base(id) 
    {
        Title = title;
        Color = colour;
    }

    // Factory method for creating a new TodoList with a generated ID
    public static TodoList Create(string title, Color? colour = null)
    {
        return new TodoList(TodoListId.CreateUnique(), title, colour ?? Color.White);
    }

    public static TodoList Create(
        TodoListId id,
        string title,
        Color colour)
    {
        return new TodoList(id, title, colour);
    }

    public void UpdateTitle(string title)
    {
        Title = title;
    }

    public void UpdateColour(Color colour)
    {
        Color = colour;
    }

    public void AddItem(TodoItem item)
    {
        _items.Add(item);
    }

    public void RemoveItem(TodoItem item)
    {
        _items.Remove(item);
    }

    public void UpdateItem(TodoItem item)
    {
        var index = _items.FindIndex(i => i.Id == item.Id);
        _items[index] = item;
    }

#pragma warning disable CS8618
    // For EF Core
    private TodoList()
    {
    }
#pragma warning restore CS8618
}
