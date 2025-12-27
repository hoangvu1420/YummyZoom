using Dapper;
using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Security;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.Restaurants.Queries.Management.GetRestaurantProfile;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record GetRestaurantProfileQuery(Guid RestaurantId)
    : IRequest<Result<RestaurantProfileDto>>, IRestaurantQuery
{
    Domain.RestaurantAggregate.ValueObjects.RestaurantId IRestaurantQuery.RestaurantId =>
        Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public sealed class GetRestaurantProfileQueryHandler
    : IRequestHandler<GetRestaurantProfileQuery, Result<RestaurantProfileDto>>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public GetRestaurantProfileQueryHandler(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<Result<RestaurantProfileDto>> Handle(GetRestaurantProfileQuery request, CancellationToken cancellationToken)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        const string sql = """
            SELECT
                r."Id" AS RestaurantId,
                r."Name" AS Name,
                r."Description" AS Description,
                r."LogoUrl" AS LogoUrl,
                r."ContactInfo_PhoneNumber" AS PhoneNumber,
                r."ContactInfo_Email" AS Email,
                r."BusinessHours" AS BusinessHours,
                r."Location_Street" AS Street,
                r."Location_City" AS City,
                r."Location_State" AS State,
                r."Location_ZipCode" AS ZipCode,
                r."Location_Country" AS Country,
                r."Geo_Latitude" AS Latitude,
                r."Geo_Longitude" AS Longitude,
                r."IsAcceptingOrders" AS IsAcceptingOrders,
                r."IsVerified" AS IsVerified
            FROM "Restaurants" r
            WHERE r."Id" = @RestaurantId AND r."IsDeleted" = FALSE
            """;

        var row = await connection.QuerySingleOrDefaultAsync<RestaurantProfileRow>(
            new CommandDefinition(sql, new { request.RestaurantId }, cancellationToken: cancellationToken));

        if (row is null)
        {
            return Result.Failure<RestaurantProfileDto>(GetRestaurantProfileErrors.NotFound(request.RestaurantId));
        }

        var dto = new RestaurantProfileDto(
            row.RestaurantId,
            row.Name,
            row.Description,
            row.LogoUrl,
            row.PhoneNumber,
            row.Email,
            row.BusinessHours,
            new RestaurantProfileAddressDto(
                row.Street,
                row.City,
                row.State,
                row.ZipCode,
                row.Country),
            row.Latitude,
            row.Longitude,
            row.IsAcceptingOrders,
            row.IsVerified);

        return Result.Success(dto);
    }

    private sealed record RestaurantProfileRow(
        Guid RestaurantId,
        string Name,
        string Description,
        string? LogoUrl,
        string PhoneNumber,
        string Email,
        string BusinessHours,
        string Street,
        string City,
        string State,
        string ZipCode,
        string Country,
        double? Latitude,
        double? Longitude,
        bool IsAcceptingOrders,
        bool IsVerified);
}

public sealed record RestaurantProfileDto(
    Guid RestaurantId,
    string Name,
    string Description,
    string? LogoUrl,
    string Phone,
    string Email,
    string BusinessHours,
    RestaurantProfileAddressDto Address,
    double? Latitude,
    double? Longitude,
    bool IsAcceptingOrders,
    bool IsVerified);

public sealed record RestaurantProfileAddressDto(
    string Street,
    string City,
    string State,
    string ZipCode,
    string Country);

public static class GetRestaurantProfileErrors
{
    public static Error NotFound(Guid restaurantId) => Error.NotFound(
        "Management.RestaurantProfile.NotFound",
        $"Restaurant profile for '{restaurantId}' was not found.");
}
