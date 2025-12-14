using MediatR;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.UserAggregate.Errors;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.Users.Queries.GetMyProfile;

public record GetMyProfileQuery : IRequest<Result<GetMyProfileResponse>>;

public record GetMyProfileResponse(
    Guid UserId,
    string Name,
    string Email,
    string? PhoneNumber,
    MyProfileAddress? Address,
    DateTime? LastLoginAt,
    IEnumerable<string> Roles,
    IEnumerable<ClaimDto> Claims);

public record ClaimDto(string Type, string Value);

public record MyProfileAddress(
    Guid AddressId,
    string Street,
    string City,
    string? State,
    string ZipCode,
    string Country,
    string? Label,
    string? DeliveryInstructions);

public class GetMyProfileQueryHandler : IRequestHandler<GetMyProfileQuery, Result<GetMyProfileResponse>>
{
    private static readonly Error Unauthorized = Error.Problem("Auth.Unauthorized", "User is not authenticated.");

    private readonly IUser _currentUser;
    private readonly IUserAggregateRepository _userRepository;
    private readonly IUserDeviceSessionRepository _sessionRepository;

    public GetMyProfileQueryHandler(
        IUser currentUser,
        IUserAggregateRepository userRepository,
        IUserDeviceSessionRepository sessionRepository)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
    }

    public async Task<Result<GetMyProfileResponse>> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.DomainUserId is null)
        {
            return Result.Failure<GetMyProfileResponse>(Unauthorized);
        }

        // Common claim extraction
        var claims = _currentUser.Principal?.Claims.Select(c => new ClaimDto(c.Type, c.Value)).ToList() ?? new List<ClaimDto>();
        var roles = _currentUser.Principal?.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList()
                    ?? new List<string>();

        var userId = _currentUser.DomainUserId;
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            // If user is Admin, they might not have a domain user profile.
            // Return a minimal profile based on Identity info.
            if (roles.Contains(Roles.Administrator))
            {
                var response = new GetMyProfileResponse(
                    userId.Value,
                    _currentUser.Principal?.Identity?.Name ?? "Administrator",
                    _currentUser.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "admin@localhost",
                    null,
                    null,
                    DateTime.UtcNow, // Or null if strictly unknown
                    roles,
                    claims);

                return Result.Success(response);
            }

            return Result.Failure<GetMyProfileResponse>(UserErrors.UserNotFound(userId.Value));
        }

        var address = user.Addresses.FirstOrDefault();
        MyProfileAddress? view = null;
        if (address is not null)
        {
            view = new MyProfileAddress(
                address.Id.Value,
                address.Street,
                address.City,
                string.IsNullOrWhiteSpace(address.State) ? null : address.State,
                address.ZipCode,
                address.Country,
                address.Label,
                address.DeliveryInstructions);
        }

        // Last login is recorded per device session; expose the latest across sessions
        var lastLogin = await _sessionRepository.GetLastLoginAsync(user.Id.Value, cancellationToken);

        var fullResponse = new GetMyProfileResponse(
            user.Id.Value,
            user.Name,
            user.Email,
            user.PhoneNumber,
            view,
            lastLogin,
            roles,
            claims);

        return Result.Success(fullResponse);
    }
}
