using YummyZoom.Application.TodoItems.Commands.CreateTodoItem;
using YummyZoom.Application.TodoItems.Commands.UpdateTodoItem;
using YummyZoom.Application.TodoLists.Commands.CreateTodoList;
using YummyZoom.Domain.TodoListAggregate;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;

namespace YummyZoom.Application.FunctionalTests.TodoItems.Commands;

using static Testing;

public class UpdateTodoItemTests : BaseTestFixture
{
    [Test]
    public async Task ShouldReturnNotFound_ForInvalidTodoItemId()
    {
        var command = new UpdateTodoItemCommand { ListId = Guid.NewGuid(), Id = Guid.NewGuid(), Title = "New Title" };
        var result = await SendAsync(command);
        result.ShouldBeFailure("TodoLists.NotFound");
    }

    [Test]
    public async Task ShouldUpdateTodoItem()
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
            Title = "New Item"
        });
        
        itemResult.ShouldBeSuccessful();
        var itemId = itemResult.Value;

        var command = new UpdateTodoItemCommand
        {
            ListId = listId,
            Id = itemId,
            Title = "Updated Item Title"
        };

        var updateResult = await SendAsync(command);
        updateResult.ShouldBeSuccessful();

        var list = await FindAsync<TodoList>(TodoListId.Create(listId));
        var item = list?.Items.FirstOrDefault(i => i.Id.Value == itemId);

        item.Should().NotBeNull();
        item!.Title.Should().Be(command.Title);
        item.LastModifiedBy.Should().NotBeNull();
        item.LastModifiedBy.Should().Be(userId.ToString()); 
        item.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMilliseconds(10000));
    }
}
