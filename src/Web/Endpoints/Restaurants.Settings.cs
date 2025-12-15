using YummyZoom.Application.Restaurants.Commands.SetRestaurantAcceptingOrders;
using YummyZoom.Application.Restaurants.Commands.UpdateRestaurantBusinessHours;
using YummyZoom.Application.Restaurants.Commands.UpdateRestaurantLocation;
using YummyZoom.Application.Restaurants.Commands.UpdateRestaurantProfile;

namespace YummyZoom.Web.Endpoints;

public partial class Restaurants
{
    private static void MapSettings(IEndpointRouteBuilder group)
    {
        // PUT /api/v1/restaurants/{restaurantId}/profile
        group.MapPut("/{restaurantId:guid}/profile", async (Guid restaurantId, UpdateProfileRequestDto body, ISender sender) =>
        {
            var cmd = new UpdateRestaurantProfileCommand(
                RestaurantId: restaurantId,
                Name: body.Name,
                Description: body.Description,
                LogoUrl: body.LogoUrl,
                Phone: body.Phone,
                Email: body.Email);
            var result = await sender.Send(cmd);
            return result.ToIResult();
        })
        .WithName("UpdateRestaurantProfile")
        .WithSummary("Update restaurant profile basics")
        .WithDescription("Updates name, description, logo URL, and/or contact info. Fields are optional; only provided fields are updated. Requires restaurant staff authorization.")
        .WithStandardResults();

        // PUT /api/v1/restaurants/{restaurantId}/accepting-orders
        group.MapPut("/{restaurantId:guid}/accepting-orders", async (Guid restaurantId, SetAcceptingOrdersRequestDto body, ISender sender) =>
        {
            var cmd = new SetRestaurantAcceptingOrdersCommand(
                RestaurantId: restaurantId,
                IsAccepting: body.IsAccepting);
            var result = await sender.Send(cmd);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("SetRestaurantAcceptingOrders")
        .WithSummary("Set restaurant accepting orders switch")
        .WithDescription("Toggles whether the restaurant is currently accepting orders. Requires restaurant staff authorization.")
        .WithStandardResults<SetRestaurantAcceptingOrdersResponse>();

        // PUT /api/v1/restaurants/{restaurantId}/business-hours
        group.MapPut("/{restaurantId:guid}/business-hours", async (Guid restaurantId, UpdateBusinessHoursRequestDto body, ISender sender) =>
        {
            var cmd = new UpdateRestaurantBusinessHoursCommand(
                RestaurantId: restaurantId,
                BusinessHours: body.BusinessHours);
            var result = await sender.Send(cmd);
            return result.ToIResult();
        })
        .WithName("UpdateRestaurantBusinessHours")
        .WithSummary("Update restaurant business hours")
        .WithDescription("Updates the restaurant business hours string. Requires restaurant staff authorization.")
        .WithStandardResults();

        // PUT /api/v1/restaurants/{restaurantId}/location
        group.MapPut("/{restaurantId:guid}/location", async (Guid restaurantId, UpdateLocationRequestDto body, ISender sender) =>
        {
            var cmd = new UpdateRestaurantLocationCommand(
                RestaurantId: restaurantId,
                Street: body.Street,
                City: body.City,
                State: body.State,
                ZipCode: body.ZipCode,
                Country: body.Country,
                Latitude: body.Latitude,
                Longitude: body.Longitude);
            var result = await sender.Send(cmd);
            return result.ToIResult();
        })
        .WithName("UpdateRestaurantLocation")
        .WithSummary("Update restaurant address and optional geo coordinates")
        .WithDescription("Updates address fields and, if provided, updates geo coordinates. Requires restaurant staff authorization.")
        .WithStandardResults();
    }

    #region DTOs for Restaurant Settings
    public sealed record SetAcceptingOrdersRequestDto(bool IsAccepting);
    public sealed record UpdateBusinessHoursRequestDto(string BusinessHours);
    public sealed record UpdateLocationRequestDto(
        string Street,
        string City,
        string State,
        string ZipCode,
        string Country,
        double? Latitude,
        double? Longitude);
    public sealed record UpdateProfileRequestDto(
        string? Name,
        string? Description,
        string? LogoUrl,
        string? Phone,
        string? Email);
    #endregion
}
