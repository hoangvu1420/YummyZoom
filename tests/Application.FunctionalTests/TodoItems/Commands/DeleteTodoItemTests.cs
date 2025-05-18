using YummyZoom.Application.TodoItems.Commands.CreateTodoItem;
using YummyZoom.Application.TodoItems.Commands.DeleteTodoItem;
using YummyZoom.Application.TodoLists.Commands.CreateTodoList;
using YummyZoom.Domain.TodoListAggregate;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;

namespace YummyZoom.Application.FunctionalTests.TodoItems.Commands;

using static Testing;

public class DeleteTodoItemTests : BaseTestFixture
{
    [Test]
    public async Task ShouldReturnNotFound_ForInvalidTodoItemId()
    {
        var command = new DeleteTodoItemCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = await SendAsync(command);
        
        result.ShouldBeFailure("TodoLists.NotFound");
    }

    [Test]
    public async Task ShouldDeleteTodoItem()
    {
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

        var deleteResult = await SendAsync(new DeleteTodoItemCommand(listId, itemId));
        deleteResult.ShouldBeSuccessful();

        var list = await FindAsync<TodoList>(TodoListId.Create(listId));
        var item = list?.Items.FirstOrDefault(i => i.Id.Value == itemId);

        item.Should().BeNull();
    }
}
