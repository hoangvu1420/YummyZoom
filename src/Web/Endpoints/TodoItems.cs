using YummyZoom.Application.Common.Models;
using YummyZoom.Application.TodoItems.Commands.CreateTodoItem;
using YummyZoom.Application.TodoItems.Commands.DeleteTodoItem;
using YummyZoom.Application.TodoItems.Commands.UpdateTodoItem;
using YummyZoom.Application.TodoItems.Commands.UpdateTodoItemDetail;
using YummyZoom.Application.TodoItems.Queries.GetTodoItemsWithPagination;

namespace YummyZoom.Web.Endpoints;

public class TodoItems : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(this)
            .RequireAuthorization();

        // GET /api/TodoItems
        group.MapGet(GetTodoItemsWithPagination)
            .WithStandardResults<PaginatedList<TodoItemBriefDto>>();

        // POST /api/TodoItems
        group.MapPost(CreateTodoItem)
            .WithStandardResults<Guid>();

        // PUT /api/TodoItems/{listId}/{id}
        group.MapPut(UpdateTodoItem, "{listId}/{id}")
            .WithStandardResults();

        // PUT /api/TodoItems/UpdateDetail/{listId}/{id}
        group.MapPut(UpdateTodoItemDetail, "UpdateDetail/{listId}/{id}")
            .WithStandardResults();

        // DELETE /api/TodoItems/{listId}/{id}
        group.MapDelete(DeleteTodoItem, "{listId}/{id}")
            .WithStandardResults();
    }

    private async Task<IResult> GetTodoItemsWithPagination(ISender sender, [AsParameters] GetTodoItemsWithPaginationQuery query)
    {
        var result = await sender.Send(query);

        return result.ToIResult();
    }

    private async Task<IResult> CreateTodoItem(ISender sender, CreateTodoItemCommand command)
    {
        var result = await sender.Send(command);

        return result.IsSuccess
            ? TypedResults.Created($"/{nameof(TodoItems)}/{result.Value}", result.Value)
            : result.ToIResult();
    }

    private async Task<IResult> UpdateTodoItem(ISender sender, Guid listId, Guid id, UpdateTodoItemCommand command)
    {
        if (id != command.Id || listId != command.ListId) return TypedResults.BadRequest();

        var result = await sender.Send(command);

        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.ToIResult();
    }

    private async Task<IResult> UpdateTodoItemDetail(ISender sender, Guid listId, Guid id, UpdateTodoItemDetailCommand command)
    {
        if (id != command.Id || listId != command.ListId) return TypedResults.BadRequest();

        var result = await sender.Send(command);

        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.ToIResult();
    }

    private async Task<IResult> DeleteTodoItem(ISender sender, Guid listId, Guid id)
    {
        var result = await sender.Send(new DeleteTodoItemCommand(listId, id));

        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.ToIResult();
    }
}
