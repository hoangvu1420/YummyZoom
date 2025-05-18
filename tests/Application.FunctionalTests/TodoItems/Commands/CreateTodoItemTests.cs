using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.TodoItems.Commands.CreateTodoItem;
using YummyZoom.Application.TodoLists.Commands.CreateTodoList;
using YummyZoom.Domain.TodoListAggregate.Enums;
using YummyZoom.Domain.TodoListAggregate;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;

namespace YummyZoom.Application.FunctionalTests.TodoItems.Commands;

using static Testing;

public class CreateTodoItemTests : BaseTestFixture
{
    [Test]
    public async Task ShouldRequireMinimumFields()
    {
        var command = new CreateTodoItemCommand();

        await FluentActions.Invoking(() =>
            SendAsync(command)).Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task ShouldCreateTodoItem()
    {
        var userId = await RunAsDefaultUserAsync();

        var listResult = await SendAsync(new CreateTodoListCommand
        {
            Title = "New List"
        });
        
        listResult.ShouldBeSuccessful();
        var listId = listResult.Value;

        var command = new CreateTodoItemCommand
        {
            ListId = listId,
            Title = "Tasks",
            Note = "Test note",
            Priority = PriorityLevel.Medium,
            Reminder = null
        };

        var itemResult = await SendAsync(command);
        
        itemResult.ShouldBeSuccessful();
        var itemId = itemResult.Value;

        var list = await FindAsync<TodoList>(TodoListId.Create(listId));
        var item = list?.Items.FirstOrDefault(i => i.Id.Value == itemId);

        item.Should().NotBeNull();
        item!.Title.Should().Be(command.Title);
        item.CreatedBy.Should().Be(userId.ToString()); 
        item.Created.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMilliseconds(10000));
        item.LastModifiedBy.Should().Be(userId.ToString()); 
        item.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMilliseconds(10000));
    }
}
