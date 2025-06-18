using System.Security.Claims;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Web.Services;

public class CurrentUser : IUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? Id => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public UserId? DomainUserId
    {
        get
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(userIdClaim, out var guidValue) && guidValue != Guid.Empty)
            {
                try
                {
                    return UserId.Create(guidValue);
                }
                catch (ArgumentException) 
                {
                    // Log this occurrence if necessary
                    return null;
                }
            }
            return null;
        }
    }
}
