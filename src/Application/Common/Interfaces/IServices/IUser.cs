using System.Security.Claims;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Application.Common.Interfaces.IServices;

public interface IUser
{
    string? Id { get; } 
    UserId? DomainUserId { get; } 
    ClaimsPrincipal? Principal { get; }
}
