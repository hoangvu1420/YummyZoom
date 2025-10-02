using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.TodoItems.Commands.CreateTodoItem;
using YummyZoom.Application.TodoItems.Commands.UpdateTodoItemDetail;
using YummyZoom.Application.TodoLists.Commands.CreateTodoList;
using YummyZoom.Domain.TodoListAggregate;
using YummyZoom.Domain.TodoListAggregate.Enums;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;

namespace YummyZoom.Application.FunctionalTests.Features.TodoItems.Commands;

using static Testing;

public class UpdateTodoItemDetailTests : BaseTestFixture
{
    [Test]
    public async Task ShouldReturnNotFound_ForInvalidTodoItemId()
    {
        var command = new UpdateTodoItemDetailCommand
        {
            ListId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Note = "Test note",
            Priority = PriorityLevel.High
        };

        var result = await SendAsync(command);
        result.ShouldBeFailure("TodoLists.NotFound");
    }

    [Test]
    public async Task ShouldUpdateTodoItemDetail()
    {
        var userId = await RunAsDefaultUserAsync();

        var listResult = await SendAsync(new CreateTodoListCommand
        {
            Title = "New List"
        });

        listResult.ShouldBeSuccessful();
        var listId = listResult.Value;

        var itemResult = await SendAsync(new CreateTodoItemCommand
        {
            ListId = listId,
            Title = "New Item",
            Note = "Initial note",
            Priority = PriorityLevel.Low,
            Reminder = null
        });

        itemResult.ShouldBeSuccessful();
        var itemId = itemResult.Value;

        var command = new UpdateTodoItemDetailCommand
        {
            ListId = listId,
            Id = itemId,
            Note = "This is the note.",
            Priority = PriorityLevel.High,
            Reminder = DateTime.UtcNow.AddDays(1)
        };

        var updateResult = await SendAsync(command);
        updateResult.ShouldBeSuccessful();

        var list = await FindAsync<TodoList>(TodoListId.Create(listId));
        var item = list?.Items.FirstOrDefault(i => i.Id.Value == itemId);

        item.Should().NotBeNull();
        item!.Note.Should().Be(command.Note);
        item.Priority.Should().Be(command.Priority);
        item.Reminder.Should().NotBeNull();
        command.Reminder.Should().NotBeNull();
        item.Reminder!.Value.Should().BeCloseTo(command.Reminder!.Value, TimeSpan.FromMilliseconds(10000));
        item.LastModifiedBy.Should().NotBeNull();
        item.LastModifiedBy.Should().Be(userId.ToString());
        item.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMilliseconds(10000));
    }
}
