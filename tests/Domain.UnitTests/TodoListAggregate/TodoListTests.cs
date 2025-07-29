using YummyZoom.Domain.TodoListAggregate;
using YummyZoom.Domain.TodoListAggregate.Entities;
using YummyZoom.Domain.TodoListAggregate.Enums;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.TodoListAggregate;

[TestFixture]
public class TodoListTests
{
    private const string DefaultTodoListTitle = "Shopping List";
    private const string AnotherTodoListTitle = "Work Tasks";
    private const string DefaultTodoItemTitle = "Buy Milk";

    // Helper to create a new TodoItem with default or specified values for adding
    private TodoItem CreateNewTodoItem(string title = DefaultTodoItemTitle, PriorityLevel priority = PriorityLevel.None, string? note = null)
    {
        // Creates an item with a new ID, listId is null as it's not yet added to a list
        return TodoItem.Create(title, note, priority, null);
    }

    // Helper to create a TodoItem instance meant to represent an update to an existing item
    private TodoItem CreateUpdatedTodoItem(TodoItemId id, string title, PriorityLevel priority = PriorityLevel.None, string? note = null)
    {
        // Uses an existing ID, assumes the Create overload for existing items does not take listId
        return TodoItem.Create(id, title, note, priority, null);
    }

    [Test]
    public void Create_WithValidTitle_ShouldInitializeTodoListWithTitleAndDefaultColor()
    {
        // Act
        var todoList = TodoList.Create(DefaultTodoListTitle);

        // Assert
        todoList.Should().NotBeNull();
        todoList.Id.Should().NotBeNull(); // Assuming TodoListId is generated
        todoList.Title.Should().Be(DefaultTodoListTitle);
        todoList.Color.Should().Be(Color.White); // Default color
        todoList.Items.Should().BeEmpty();
    }

    [Test]
    public void Create_WithValidTitleAndSpecifiedColor_ShouldInitializeTodoListCorrectly()
    {
        // Arrange
        var color = Color.Blue;

        // Act
        var todoList = TodoList.Create(AnotherTodoListTitle, color);

        // Assert
        todoList.Should().NotBeNull();
        todoList.Id.Should().NotBeNull();
        todoList.Title.Should().Be(AnotherTodoListTitle);
        todoList.Color.Should().Be(color);
        todoList.Items.Should().BeEmpty();
    }

    [Test]
    public void UpdateTitle_WithValidNewTitle_ShouldUpdateTodoListTitle()
    {
        // Arrange
        var todoList = TodoList.Create("Old Title");
        const string newTitle = "New Valid Title";

        // Act
        todoList.UpdateTitle(newTitle);

        // Assert
        todoList.Title.Should().Be(newTitle);
    }

    [Test]
    public void UpdateColour_WithNewColor_ShouldUpdateTodoListColour()
    {
        // Arrange
        var todoList = TodoList.Create(DefaultTodoListTitle, Color.White);
        var newColor = Color.Green;

        // Act
        todoList.UpdateColour(newColor);

        // Assert
        todoList.Color.Should().Be(newColor);
    }

    [Test]
    public void AddItem_WithValidItem_ShouldAddTodoItemToList()
    {
        // Arrange
        var todoList = TodoList.Create(DefaultTodoListTitle);
        var todoItem = CreateNewTodoItem();

        // Act
        todoList.AddItem(todoItem);

        // Assert
        todoList.Items.Should().ContainSingle();
        todoList.Items.Should().Contain(todoItem);
    }

    [Test]
    public void RemoveItem_ExistingItem_ShouldRemoveTodoItemFromList()
    {
        // Arrange
        var todoList = TodoList.Create(DefaultTodoListTitle);
        var item1 = CreateNewTodoItem("Task 1");
        var item2 = CreateNewTodoItem("Task 2");
        todoList.AddItem(item1);
        todoList.AddItem(item2);

        // Act
        todoList.RemoveItem(item1); // Pass the TodoItem object

        // Assert
        todoList.Items.Should().ContainSingle();
        todoList.Items.Should().NotContain(i => i.Id == item1.Id);
        todoList.Items.Should().Contain(i => i.Id == item2.Id);
    }

    [Test]
    public void UpdateItem_ExistingItem_ShouldUpdateExistingTodoItemInList()
    {
        // Arrange
        var todoList = TodoList.Create(DefaultTodoListTitle);
        var originalItem = CreateNewTodoItem("Initial Task", PriorityLevel.Low);
        todoList.AddItem(originalItem);

        var updatedItemInstance = CreateUpdatedTodoItem(originalItem.Id, "Updated Task", PriorityLevel.High);

        // Act
        todoList.UpdateItem(updatedItemInstance);

        // Assert
        todoList.Items.Should().ContainSingle();
        var itemInList = todoList.Items.Single();
        itemInList.Id.Should().Be(originalItem.Id);
        itemInList.Title.Should().Be("Updated Task");
        itemInList.Priority.Should().Be(PriorityLevel.High);
    }
}
