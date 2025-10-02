using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.TodoLists.Commands.CreateTodoList;
using YummyZoom.Application.TodoLists.Commands.UpdateTodoList;
using YummyZoom.Domain.TodoListAggregate;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;

namespace YummyZoom.Application.FunctionalTests.Features.TodoLists.Commands;

using static Testing;

public class UpdateTodoListTests : BaseTestFixture
{
    [Test]
    public async Task ShouldReturnNotFound_ForInvalidTodoListId()
    {
        var command = new UpdateTodoListCommand { Id = Guid.NewGuid(), Title = "New Title" };
        var result = await SendAsync(command);
        result.ShouldBeFailure("TodoLists.NotFound");
    }

    [Test]
    public async Task ShouldRequireUniqueTitle()
    {
        var listResult = await SendAsync(new CreateTodoListCommand
        {
            Title = "New List"
        });

        listResult.ShouldBeSuccessful();

        await SendAsync(new CreateTodoListCommand
        {
            Title = "Other List"
        });

        var command = new UpdateTodoListCommand
        {
            Id = listResult.Value,
            Title = "Other List"
        };

        (await FluentActions.Invoking(() =>
            SendAsync(command))
                .Should().ThrowAsync<ValidationException>().Where(ex => ex.Errors.ContainsKey("Title")))
                .And.Errors["Title"].Should().Contain("'Title' must be unique.");
    }

    [Test]
    public async Task ShouldUpdateTodoList()
    {
        var userId = await RunAsDefaultUserAsync();

        var listResult = await SendAsync(new CreateTodoListCommand
        {
            Title = "New List"
        });

        listResult.ShouldBeSuccessful();
        var listId = listResult.Value;

        var command = new UpdateTodoListCommand
        {
            Id = listId,
            Title = "Updated List Title"
        };

        var updateResult = await SendAsync(command);
        updateResult.ShouldBeSuccessful();

        var list = await FindAsync<TodoList>(TodoListId.Create(listId));

        list.Should().NotBeNull();
        list!.Title.Should().Be(command.Title);
        list.LastModifiedBy.Should().NotBeNull();
        list.LastModifiedBy.Should().Be(userId.ToString());
        list.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMilliseconds(10000));
    }
}
