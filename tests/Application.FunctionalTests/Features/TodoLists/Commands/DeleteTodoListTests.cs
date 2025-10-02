using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.TodoLists.Commands.CreateTodoList;
using YummyZoom.Application.TodoLists.Commands.DeleteTodoList;
using YummyZoom.Domain.TodoListAggregate;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;

namespace YummyZoom.Application.FunctionalTests.Features.TodoLists.Commands;

using static Testing;

public class DeleteTodoListTests : BaseTestFixture
{
    [Test]
    public async Task ShouldReturnNotFound_ForInvalidTodoListId()
    {
        var command = new DeleteTodoListCommand(Guid.NewGuid());
        var result = await SendAsync(command);
        result.ShouldBeFailure("TodoLists.NotFound");
    }

    [Test]
    public async Task ShouldDeleteTodoList()
    {
        var listResult = await SendAsync(new CreateTodoListCommand
        {
            Title = "New List"
        });

        listResult.ShouldBeSuccessful();
        var listId = listResult.Value;

        var deleteResult = await SendAsync(new DeleteTodoListCommand(listId));
        deleteResult.ShouldBeSuccessful();

        var list = await FindAsync<TodoList>(TodoListId.Create(listId));

        list.Should().BeNull();
    }
}
