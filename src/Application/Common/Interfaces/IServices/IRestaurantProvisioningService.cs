using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Common.Interfaces.IServices;

public interface IRestaurantProvisioningService
{
    Task<Result<Guid>> CreateAndVerifyAsync(RestaurantProvisioningRequest request, CancellationToken ct = default);
}

public sealed record RestaurantProvisioningRequest(
    string Name,
    string? LogoUrl,
    string? BackgroundImageUrl,
    string Description,
    string CuisineType,
    string Street,
    string City,
    string State,
    string ZipCode,
    string Country,
    string PhoneNumber,
    string Email,
    string BusinessHours,
    double? Latitude,
    double? Longitude);

