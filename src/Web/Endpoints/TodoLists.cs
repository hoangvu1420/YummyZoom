using YummyZoom.Application.TodoLists.Commands.CreateTodoList;
using YummyZoom.Application.TodoLists.Commands.DeleteTodoList;
using YummyZoom.Application.TodoLists.Commands.UpdateTodoList;
using YummyZoom.Application.TodoLists.Queries.GetTodos;

namespace YummyZoom.Web.Endpoints;

public class TodoLists : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(this)
            .RequireAuthorization();

        // GET /api/TodoLists
        group.MapGet(GetTodoLists)
            .WithStandardResults<TodosVm>();

        // POST /api/TodoLists
        group.MapPost(CreateTodoList)
            .WithStandardResults<Guid>();

        // PUT /api/TodoLists/{id}
        group.MapPut(UpdateTodoList, "{id}")
            .WithStandardResults();

        // DELETE /api/TodoLists/{id}
        group.MapDelete(DeleteTodoList, "{id}")
            .WithStandardResults();
    }

    private async Task<IResult> GetTodoLists(ISender sender)
    {
        var result = await sender.Send(new GetTodosQuery());

        return result.ToIResult();
    }

    private async Task<IResult> CreateTodoList(ISender sender, CreateTodoListCommand command)
    {
        var result = await sender.Send(command);

        return result.IsSuccess 
            ? TypedResults.Created($"/{nameof(TodoLists)}/{result.Value}", result.Value) 
            : result.ToIResult();
    }

    private async Task<IResult> UpdateTodoList(ISender sender, Guid id, UpdateTodoListCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();
        
        var result = await sender.Send(command);

        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.ToIResult();
    }

    private async Task<IResult> DeleteTodoList(ISender sender, Guid id)
    {
        var result = await sender.Send(new DeleteTodoListCommand(id));

        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.ToIResult();
    }
}
