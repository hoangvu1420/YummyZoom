using YummyZoom.Application.TodoItems.Commands.CreateTodoItem;
using YummyZoom.Application.TodoItems.Queries.GetTodoItemsWithPagination;
using YummyZoom.Application.TodoLists.Commands.CreateTodoList;
using YummyZoom.Domain.TodoListAggregate.Enums;

namespace YummyZoom.Application.FunctionalTests.TodoItems.Queries;

using static Testing;

public class GetTodoItemsWithPaginationTests : BaseTestFixture
{
    [Test]
    public async Task ShouldReturnEmptyList_WhenNoItems()
    {
        // Arrange
        await RunAsDefaultUserAsync();
        
        var listResult = await SendAsync(new CreateTodoListCommand
        {
            Title = "Empty List"
        });
        
        listResult.ShouldBeSuccessful();
        var listId = listResult.Value;
        
        // Act
        var query = new GetTodoItemsWithPaginationQuery
        {
            ListId = listId,
            PageNumber = 1,
            PageSize = 10
        };
        
        var result = await SendAsync(query);
        
        // Assert
        result.ShouldBeSuccessful();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }
    
    [Test]
    public async Task ShouldReturnAllItems_WithPagination()
    {
        // Arrange
        await RunAsDefaultUserAsync();
        
        var listResult = await SendAsync(new CreateTodoListCommand
        {
            Title = "List with Items"
        });
        
        listResult.ShouldBeSuccessful();
        var listId = listResult.Value;
        
        // Create 5 items
        for (int i = 1; i <= 5; i++)
        {
            var itemResult = await SendAsync(new CreateTodoItemCommand
            {
                ListId = listId,
                Title = $"Item {i}",
                Priority = PriorityLevel.Medium
            });
            
            itemResult.ShouldBeSuccessful();
        }
        
        // Act - Get page 1 with 2 items per page
        var query1 = new GetTodoItemsWithPaginationQuery
        {
            ListId = listId,
            PageNumber = 1,
            PageSize = 2
        };
        
        var result1 = await SendAsync(query1);
        
        // Assert
        result1.ShouldBeSuccessful();
        result1.Value.Items.Should().HaveCount(2);
        result1.Value.TotalCount.Should().Be(5);
        result1.Value.TotalPages.Should().Be(3);
        result1.Value.HasNextPage.Should().BeTrue();
        result1.Value.HasPreviousPage.Should().BeFalse();
        
        // Act - Get page 2 with 2 items per page
        var query2 = new GetTodoItemsWithPaginationQuery
        {
            ListId = listId,
            PageNumber = 2,
            PageSize = 2
        };
        
        var result2 = await SendAsync(query2);
        
        // Assert
        result2.ShouldBeSuccessful();
        result2.Value.Items.Should().HaveCount(2);
        result2.Value.TotalCount.Should().Be(5);
        result2.Value.TotalPages.Should().Be(3);
        result2.Value.HasNextPage.Should().BeTrue();
        result2.Value.HasPreviousPage.Should().BeTrue();
    }
    
    [Test]
    public async Task ShouldReturnNotFound_ForInvalidTodoListId()
    {
        // Arrange
        await RunAsDefaultUserAsync();
        
        var nonExistentListId = Guid.NewGuid();
        
        // Act
        var query = new GetTodoItemsWithPaginationQuery
        {
            ListId = nonExistentListId,
            PageNumber = 1,
            PageSize = 10
        };
        
        var result = await SendAsync(query);
        
        // Assert
        result.ShouldBeFailure("TodoLists.NotFound");
    }
}
