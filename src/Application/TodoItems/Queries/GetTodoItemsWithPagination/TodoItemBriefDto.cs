using YummyZoom.Domain.TodoListAggregate.Entities;

namespace YummyZoom.Application.TodoItems.Queries.GetTodoItemsWithPagination;

public class TodoItemBriefDto
{
    public Guid Id { get; init; }

    public string? Title { get; init; }

    public bool IsDone { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<TodoItem, TodoItemBriefDto>()
                .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id.Value))
                .ForMember(d => d.IsDone, opt => opt.MapFrom(s => s.IsDone));
        }
    }
}
