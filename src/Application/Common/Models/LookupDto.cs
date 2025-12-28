using YummyZoom.Domain.TodoListAggregate;
using YummyZoom.Domain.TodoListAggregate.Entities;

namespace YummyZoom.Application.Common.Models;

public class LookupDto
{
    public int Id { get; init; }

    public string? Title { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<TodoList, LookupDto>()
                .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id.Value.GetHashCode()));
            CreateMap<TodoItem, LookupDto>()
                .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id.Value.GetHashCode()));
        }
    }
}
