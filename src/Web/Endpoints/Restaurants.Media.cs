using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Caching;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Web.Infrastructure;

namespace YummyZoom.Web.Endpoints;

public partial class Restaurants
{
    private static void MapMedia(IEndpointRouteBuilder group)
    {
        // POST /api/v1/restaurants/{restaurantId}/media/logo
        group.MapPost("/{restaurantId:guid}/media/logo", async (
            HttpContext httpContext,
            Guid restaurantId,
            [FromForm] IFormFile file,
            [FromServices] IAuthorizationService authorizationService,
            [FromServices] ICacheService cache,
            [FromServices] IMediaStorageService mediaStorage,
            [FromServices] IHostEnvironment environment,
            [FromServices] IUser currentUser,
            [FromServices] ILogger<Restaurants> logger,
            CancellationToken ct) =>
        {
            return await MediaUploadEndpoint.HandleAsync(
                httpContext,
                file,
                restaurantId,
                null,
                "restaurant-logo",
                null,
                authorizationService,
                cache,
                mediaStorage,
                environment,
                currentUser,
                logger,
                ct);
        })
        .DisableAntiforgery()
        .WithName("UploadRestaurantLogo")
        .WithSummary("Upload restaurant logo")
        .WithDescription("Uploads or replaces the restaurant logo and returns the hosted URL.")
        .Produces<YummyZoom.Application.Common.Models.Media.MediaUploadResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status415UnsupportedMediaType)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // POST /api/v1/restaurants/{restaurantId}/media/background
        group.MapPost("/{restaurantId:guid}/media/background", async (
            HttpContext httpContext,
            Guid restaurantId,
            [FromForm] IFormFile file,
            [FromServices] IAuthorizationService authorizationService,
            [FromServices] ICacheService cache,
            [FromServices] IMediaStorageService mediaStorage,
            [FromServices] IHostEnvironment environment,
            [FromServices] IUser currentUser,
            [FromServices] ILogger<Restaurants> logger,
            CancellationToken ct) =>
        {
            return await MediaUploadEndpoint.HandleAsync(
                httpContext,
                file,
                restaurantId,
                null,
                "restaurant-background",
                null,
                authorizationService,
                cache,
                mediaStorage,
                environment,
                currentUser,
                logger,
                ct);
        })
        .DisableAntiforgery()
        .WithName("UploadRestaurantBackground")
        .WithSummary("Upload restaurant background image")
        .WithDescription("Uploads or replaces the restaurant background/hero image and returns the hosted URL.")
        .Produces<YummyZoom.Application.Common.Models.Media.MediaUploadResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status415UnsupportedMediaType)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // POST /api/v1/restaurants/{restaurantId}/menu-items/{menuItemId}/image
        group.MapPost("/{restaurantId:guid}/menu-items/{menuItemId:guid}/image", async (
            HttpContext httpContext,
            Guid restaurantId,
            Guid menuItemId,
            [FromForm] IFormFile file,
            [FromServices] IAuthorizationService authorizationService,
            [FromServices] ICacheService cache,
            [FromServices] IMediaStorageService mediaStorage,
            [FromServices] IHostEnvironment environment,
            [FromServices] IUser currentUser,
            [FromServices] ILogger<Restaurants> logger,
            CancellationToken ct) =>
        {
            return await MediaUploadEndpoint.HandleAsync(
                httpContext,
                file,
                restaurantId,
                menuItemId,
                "menu-item",
                null,
                authorizationService,
                cache,
                mediaStorage,
                environment,
                currentUser,
                logger,
                ct);
        })
        .DisableAntiforgery()
        .WithName("UploadMenuItemImage")
        .WithSummary("Upload menu item image")
        .WithDescription("Uploads or replaces a menu item image and returns the hosted URL.")
        .Produces<YummyZoom.Application.Common.Models.Media.MediaUploadResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status415UnsupportedMediaType)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
