using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.TodoItems.Commands.UpdateTodoItem;
using YummyZoom.Application.TodoLists.Commands.CreateTodoList;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.FunctionalTests.Features.TodoItems.Commands;

using static Testing;

public class UpdateNonExistentItemTests : BaseTestFixture
{
    [Test]
    public async Task ShouldReturnNotFoundError_WhenItemDoesNotExist()
    {
        // Arrange
        await RunAsDefaultUserAsync();
        
        var listResult = await SendAsync(new CreateTodoListCommand
        {
            Title = "New List"
        });
        
        listResult.ShouldBeSuccessful();
        var listId = listResult.Value;
        
        var nonExistentItemId = Guid.NewGuid();
        
        // Act
        var command = new UpdateTodoItemCommand
        {
            Id = nonExistentItemId,
            ListId = listId,
            Title = "Updated Title",
            IsDone = true
        };
        
        var result = await SendAsync(command);
        
        // Assert
        result.ShouldBeFailure("TodoItems.NotFound");
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }
}
