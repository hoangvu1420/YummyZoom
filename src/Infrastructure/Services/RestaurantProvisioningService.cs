using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.RestaurantAggregate;
using YummyZoom.SharedKernel;

namespace YummyZoom.Infrastructure.Services;

public sealed class RestaurantProvisioningService : IRestaurantProvisioningService
{
    private readonly IRestaurantRepository _restaurants;

    public RestaurantProvisioningService(IRestaurantRepository restaurants)
    {
        _restaurants = restaurants;
    }

    public async Task<Result<Guid>> CreateAndVerifyAsync(RestaurantProvisioningRequest request, CancellationToken ct = default)
    {
        var create = Restaurant.Create(
            name: request.Name,
            logoUrl: request.LogoUrl,
            backgroundImageUrl: request.BackgroundImageUrl,
            description: request.Description,
            cuisineType: request.CuisineType,
            street: request.Street,
            city: request.City,
            state: request.State,
            zipCode: request.ZipCode,
            country: request.Country,
            phoneNumber: request.PhoneNumber,
            email: request.Email,
            businessHours: request.BusinessHours,
            latitude: request.Latitude,
            longitude: request.Longitude);

        if (create.IsFailure)
        {
            return Result.Failure<Guid>(create.Error);
        }

        var restaurant = create.Value;
        restaurant.Verify();

        await _restaurants.AddAsync(restaurant, ct);

        return Result.Success(restaurant.Id.Value);
    }
}

