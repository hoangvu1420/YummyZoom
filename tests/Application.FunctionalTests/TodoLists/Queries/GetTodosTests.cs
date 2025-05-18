using YummyZoom.Application.TodoLists.Queries.GetTodos;
using YummyZoom.Domain.TodoListAggregate;
using YummyZoom.Domain.TodoListAggregate.Entities;
using YummyZoom.Domain.TodoListAggregate.Enums;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;

namespace YummyZoom.Application.FunctionalTests.TodoLists.Queries;

using static Testing;

public class GetTodosTests : BaseTestFixture
{
    [Test]
    public async Task ShouldReturnPriorityLevels()
    {
        await RunAsDefaultUserAsync();

        var query = new GetTodosQuery();

        var result = await SendAsync(query);

        result.ShouldBeSuccessful();
        result.Value.PriorityLevels.Should().NotBeEmpty();
    }

    [Test]
    public async Task ShouldReturnAllListsAndItems()
    {
        await RunAsDefaultUserAsync();

        var todoList = TodoList.Create("Shopping", Color.Blue);

        var apples = TodoItem.Create("Apples", null, PriorityLevel.None, null);
        apples.Complete();
        todoList.AddItem(apples);

        var milk = TodoItem.Create("Milk", null, PriorityLevel.None, null);
        milk.Complete();
        todoList.AddItem(milk);

        var bread = TodoItem.Create("Bread", null, PriorityLevel.None, null);
        bread.Complete();
        todoList.AddItem(bread);

        todoList.AddItem(TodoItem.Create("Toilet paper", null, PriorityLevel.None, null));
        todoList.AddItem(TodoItem.Create("Pasta", null, PriorityLevel.None, null));
        todoList.AddItem(TodoItem.Create("Tissues", null, PriorityLevel.None, null));
        todoList.AddItem(TodoItem.Create("Tuna", null, PriorityLevel.None, null));

        await AddAsync(todoList);

        var query = new GetTodosQuery();

        var result = await SendAsync(query);

        result.ShouldBeSuccessful();
        result.Value.Lists.Should().HaveCount(1);
        result.Value.Lists.First().Items.Should().HaveCount(7);
    }

    [Test]
    public async Task ShouldDenyAnonymousUser()
    {
        var query = new GetTodosQuery();

        var action = () => SendAsync(query);

        await action.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
