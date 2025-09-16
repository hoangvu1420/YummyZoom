using MediatR;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.UserAggregate.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Users.Queries.GetMyProfile;

public record GetMyProfileQuery : IRequest<Result<GetMyProfileResponse>>;

public record GetMyProfileResponse(
    Guid UserId,
    string Name,
    string Email,
    string? PhoneNumber,
    MyProfileAddress? Address);

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

    public GetMyProfileQueryHandler(IUser currentUser, IUserAggregateRepository userRepository)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public async Task<Result<GetMyProfileResponse>> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.DomainUserId is null)
        {
            return Result.Failure<GetMyProfileResponse>(Unauthorized);
        }

        var userId = _currentUser.DomainUserId;
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
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

        var response = new GetMyProfileResponse(
            user.Id.Value,
            user.Name,
            user.Email,
            user.PhoneNumber,
            view);

        return Result.Success(response);
    }
}
