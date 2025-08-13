using YummyZoom.Domain.RestaurantAggregate.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.RestaurantAggregate.ValueObjects;

public sealed class GeoCoordinates : ValueObject
{
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }

    private GeoCoordinates(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public static Result<GeoCoordinates> Create(double latitude, double longitude)
    {
        if (double.IsNaN(latitude) || latitude < -90.0 || latitude > 90.0)
            return Result.Failure<GeoCoordinates>(RestaurantErrors.LatitudeOutOfRange(latitude));

        if (double.IsNaN(longitude) || longitude < -180.0 || longitude > 180.0)
            return Result.Failure<GeoCoordinates>(RestaurantErrors.LongitudeOutOfRange(longitude));

        return Result.Success(new GeoCoordinates(latitude, longitude));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Latitude;
        yield return Longitude;
    }

#pragma warning disable CS8618
    private GeoCoordinates() { }
#pragma warning restore CS8618
}


