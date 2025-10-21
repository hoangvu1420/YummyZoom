using System.Text.Json;

namespace YummyZoom.Application.Restaurants.Queries.Common;

internal static class RestaurantPublicInfoMapper
{
    public static RestaurantPublicInfoDto Map(RestaurantPublicInfoData row)
    {
        var cuisineTags = ParseCuisineTags(row.CuisineTagsJson);

        var addressDto = new AddressDto(
            row.Street,
            row.City,
            row.State,
            row.ZipCode,
            row.Country);

        var contactInfoDto = new ContactInfoDto(
            row.PhoneNumber,
            row.Email);

        return new RestaurantPublicInfoDto(
            row.RestaurantId,
            row.Name,
            row.LogoUrl,
            row.BackgroundImageUrl,
            row.Description,
            row.CuisineType,
            cuisineTags,
            row.IsAcceptingOrders,
            row.IsVerified,
            addressDto,
            contactInfoDto,
            row.BusinessHours,
            row.EstablishedDate,
            row.LastModified,
            row.DistanceKm);
    }

    private static IReadOnlyList<string> ParseCuisineTags(string? cuisineTagsJson)
    {
        if (string.IsNullOrWhiteSpace(cuisineTagsJson))
        {
            return Array.Empty<string>();
        }

        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(cuisineTagsJson);
            return list is { Count: > 0 } ? list : Array.Empty<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }
}

internal sealed class RestaurantPublicInfoData
{
    public Guid RestaurantId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? LogoUrl { get; init; }
    public string? BackgroundImageUrl { get; init; }
    public string Description { get; init; } = string.Empty;
    public string CuisineType { get; init; } = string.Empty;
    public string? CuisineTagsJson { get; init; }
    public bool IsAcceptingOrders { get; init; }
    public bool IsVerified { get; init; }
    public string Street { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string ZipCode { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string BusinessHours { get; init; } = string.Empty;
    public DateTimeOffset EstablishedDate { get; init; }
    public DateTimeOffset LastModified { get; init; }
    public decimal? DistanceKm { get; init; }
}
