using YummyZoom.Domain.UserAggregate.ValueObjects; 

namespace YummyZoom.Application.Common.Interfaces;

public interface IUser
{
    string? Id { get; } 
    UserId? DomainId { get; } 
}
