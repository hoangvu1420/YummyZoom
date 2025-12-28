using YummyZoom.Domain.TodoListAggregate;

namespace YummyZoom.Application.TodoLists.Queries.GetTodos;

public class TodoListDto
{
    public TodoListDto()
    {
        Items = Array.Empty<TodoItemDto>();
    }

    public Guid Id { get; init; }

    public string? Title { get; init; }

    public string? Colour { get; init; }

    public IReadOnlyCollection<TodoItemDto> Items { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<TodoList, TodoListDto>()
                .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id.Value))
                .ForMember(d => d.Colour, opt => opt.MapFrom(s => s.Color.Code));
        }
    }
}
