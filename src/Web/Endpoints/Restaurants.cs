using Microsoft.AspNetCore.Routing;

namespace YummyZoom.Web.Endpoints;

/// <summary>
/// Restaurant-scoped endpoints for menu management, orders, and public information.
/// Includes menu hierarchy management (menus, categories, items) and restaurant operations.
/// Base route resolves to /api/v1/restaurants via versioned endpoint grouping.
/// </summary>
public partial class Restaurants : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup(this)
            .RequireAuthorization();

        MapMedia(group);
        MapMenuManagement(group);
        MapMenuItemsManagement(group);
        MapCoupons(group);
        MapSettings(group);
        MapOrders(group);
        MapCustomizationGroups(group);
        MapReviews(group);
        MapDashboard(group);
        MapPayouts(group);

        var publicGroup = app.MapGroup(this);
        MapPublic(publicGroup);
    }
}
