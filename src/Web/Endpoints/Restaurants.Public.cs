using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Restaurants.Queries.GetFullMenu;
using YummyZoom.Application.Restaurants.Queries.GetRestaurantAggregatedDetails;
using YummyZoom.Application.Restaurants.Queries.GetRestaurantPublicInfo;
using YummyZoom.Application.Restaurants.Queries.Public.GetMenuItemAvailability;
using YummyZoom.Application.Restaurants.Queries.Public.GetMenuItemDetails;
using YummyZoom.Application.Restaurants.Queries.SearchRestaurants;
using YummyZoom.Application.Reviews.Queries.Common;
using YummyZoom.Application.Reviews.Queries.GetRestaurantReviews;
using YummyZoom.Application.Reviews.Queries.GetRestaurantReviewSummary;
using YummyZoom.Web.Infrastructure.Http;

namespace YummyZoom.Web.Endpoints;

public partial class Restaurants
{
    private static void MapPublic(IEndpointRouteBuilder publicGroup)
    {
        // GET /api/v1/restaurants/{restaurantId}/menu
        publicGroup.MapGet("/{restaurantId:guid}/menu", async (Guid restaurantId, ISender sender, HttpContext http) =>
        {
            var result = await sender.Send(new GetFullMenuQuery(restaurantId));
            if (result.IsFailure) return result.ToIResult();

            var etag = HttpCaching.BuildWeakEtag(restaurantId, result.Value.LastRebuiltAt);
            var lastModified = HttpCaching.ToRfc1123(result.Value.LastRebuiltAt);

            if (HttpCaching.MatchesIfNoneMatch(http.Request, etag) ||
                HttpCaching.NotModifiedSince(http.Request, result.Value.LastRebuiltAt))
            {
                http.Response.Headers.ETag = etag.ToString();
                http.Response.Headers.LastModified = lastModified;
                http.Response.Headers.CacheControl = "public, max-age=300";
                return Results.StatusCode(StatusCodes.Status304NotModified);
            }

            http.Response.Headers.ETag = etag.ToString();
            http.Response.Headers.LastModified = lastModified;
            http.Response.Headers.CacheControl = "public, max-age=300";
            return Results.Text(result.Value.MenuJson, "application/json");
        })
        .WithName("GetRestaurantPublicMenu")
        .WithSummary("Get restaurant's public menu")
        .WithDescription("Retrieves the complete menu for a restaurant including categories and items. Public endpoint - no authentication required. Supports HTTP caching with ETag and Last-Modified headers.")
        .Produces<string>(StatusCodes.Status200OK, "application/json")
        .Produces(StatusCodes.Status304NotModified)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // GET /api/v1/restaurants/{restaurantId}/reviews
        publicGroup.MapGet("/{restaurantId:guid}/reviews", async (Guid restaurantId, int? pageNumber, int? pageSize, ISender sender) =>
        {
            // Apply defaults after binding to avoid Minimal API early 400s for missing value-type properties
            var page = pageNumber ?? 1;
            var size = pageSize ?? 10;

            var result = await sender.Send(new GetRestaurantReviewsQuery(restaurantId, page, size));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("GetRestaurantReviews")
        .WithSummary("List public reviews for this restaurant")
        .Produces<PaginatedList<ReviewDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // GET /api/v1/restaurants/{restaurantId}/menu-items/{itemId}
        publicGroup.MapGet("/{restaurantId:guid}/menu-items/{itemId:guid}", async (
            Guid restaurantId,
            Guid itemId,
            ISender sender,
            HttpContext http) =>
        {
            var result = await sender.Send(new YummyZoom.Application.Restaurants.Queries.Public.GetMenuItemDetails.GetMenuItemPublicDetailsQuery(restaurantId, itemId));
            if (!result.IsSuccess) return result.ToIResult();

            var dto = result.Value;
            var etag = HttpCaching.BuildWeakEtag(restaurantId, dto.LastModified);
            var lastModified = HttpCaching.ToRfc1123(dto.LastModified);

            if (HttpCaching.MatchesIfNoneMatch(http.Request, etag) ||
                HttpCaching.NotModifiedSince(http.Request, dto.LastModified))
            {
                http.Response.Headers.ETag = etag.ToString();
                http.Response.Headers.LastModified = lastModified;
                http.Response.Headers.CacheControl = "public, max-age=120";
                return Results.StatusCode(StatusCodes.Status304NotModified);
            }

            http.Response.Headers.ETag = etag.ToString();
            http.Response.Headers.LastModified = lastModified;
            http.Response.Headers.CacheControl = "public, max-age=120";
            return Results.Ok(dto);
        })
        .WithName("GetMenuItemPublicDetails")
        .WithSummary("Get public menu item details")
        .WithDescription("Returns full public details for a menu item including customization groups, sold count, rating, and upsell suggestions. Supports HTTP caching via ETag and Last-Modified.")
        .WithStandardResults<YummyZoom.Application.Restaurants.Queries.Public.GetMenuItemDetails.MenuItemPublicDetailsDto>()
        .Produces(StatusCodes.Status304NotModified);

        // GET /api/v1/restaurants/{restaurantId}/menu-items/{itemId}/availability
        publicGroup.MapGet("/{restaurantId:guid}/menu-items/{itemId:guid}/availability", async (
            Guid restaurantId,
            Guid itemId,
            ISender sender,
            HttpContext http) =>
        {
            var result = await sender.Send(new YummyZoom.Application.Restaurants.Queries.Public.GetMenuItemAvailability.GetMenuItemAvailabilityQuery(restaurantId, itemId));
            if (!result.IsSuccess) return result.ToIResult();

            http.Response.Headers.CacheControl = "public, max-age=15";
            return Results.Ok(result.Value);
        })
        .WithName("GetMenuItemAvailability")
        .WithSummary("Get quick menu item availability")
        .WithDescription("Returns a short-lived availability snapshot for a menu item. Uses short TTL caching on the server side and Cache-Control header for clients.")
        .WithStandardResults<YummyZoom.Application.Restaurants.Queries.Public.GetMenuItemAvailability.MenuItemAvailabilityDto>();

        // GET /api/v1/restaurants/{restaurantId}/reviews/summary
        publicGroup.MapGet("/{restaurantId:guid}/reviews/summary", async (Guid restaurantId, ISender sender) =>
        {
            var result = await sender.Send(new GetRestaurantReviewSummaryQuery(restaurantId));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("GetRestaurantReviewSummary")
        .WithSummary("Get review summary for this restaurant")
        .Produces<RestaurantReviewSummaryDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // GET /api/v1/restaurants/{restaurantId}/details
        publicGroup.MapGet("/{restaurantId:guid}/details", async (
            Guid restaurantId,
            double? lat,
            double? lng,
            ISender sender,
            HttpContext http) =>
        {
            var result = await sender.Send(new GetRestaurantAggregatedDetailsQuery(restaurantId, lat, lng));
            if (!result.IsSuccess) return result.ToIResult();

            var details = result.Value;
            var isPersonalized = lat.HasValue || lng.HasValue;

            if (!isPersonalized)
            {
                var etag = HttpCaching.BuildWeakEtag(restaurantId, details.LastChangedUtc);
                var lastModified = HttpCaching.ToRfc1123(details.LastChangedUtc);

                if (HttpCaching.MatchesIfNoneMatch(http.Request, etag) ||
                    HttpCaching.NotModifiedSince(http.Request, details.LastChangedUtc))
                {
                    http.Response.Headers.ETag = etag.ToString();
                    http.Response.Headers.LastModified = lastModified;
                    http.Response.Headers.CacheControl = "public, max-age=120";
                    return Results.StatusCode(StatusCodes.Status304NotModified);
                }

                http.Response.Headers.ETag = etag.ToString();
                http.Response.Headers.LastModified = lastModified;
                http.Response.Headers.CacheControl = "public, max-age=120";
            }
            else
            {
                http.Response.Headers.CacheControl = "no-store";
            }

            JsonElement menuElement = ParseMenuJson(details.Menu.MenuJson);

            var info = details.Info;
            decimal? avgRating = (decimal)details.ReviewSummary.AverageRating;
            int? ratingCount = details.ReviewSummary.TotalReviews;

            var infoResponse = new RestaurantPublicInfoResponseDto(
                info.RestaurantId,
                info.Name,
                info.LogoUrl,
                info.BackgroundImageUrl,
                info.Description,
                info.CuisineType,
                info.CuisineTags,
                info.IsAcceptingOrders,
                info.IsVerified,
                info.Address,
                info.ContactInfo,
                info.BusinessHours,
                info.EstablishedDate,
                avgRating,
                ratingCount,
                info.DistanceKm);

            var response = new RestaurantAggregatedDetailsResponseDto(
                infoResponse,
                new RestaurantAggregatedMenuResponseDto(details.Menu.LastRebuiltAt, menuElement),
                details.ReviewSummary,
                details.LastChangedUtc);

            return Results.Ok(response);

            static JsonElement ParseMenuJson(string json)
            {
                var payload = string.IsNullOrWhiteSpace(json) ? "{}" : json;
                try
                {
                    using var menuDoc = JsonDocument.Parse(payload);
                    return menuDoc.RootElement.Clone();
                }
                catch (JsonException)
                {
                    using var fallbackDoc = JsonDocument.Parse("{}");
                    return fallbackDoc.RootElement.Clone();
                }
            }
        })
        .WithName("GetRestaurantAggregatedDetails")
        .WithSummary("Get combined restaurant info, menu, and review summary")
        .WithDescription("Aggregates public restaurant information, full menu JSON, and review summary into one response. Supports conditional HTTP caching when no personalization parameters are provided.")
        .Produces<RestaurantAggregatedDetailsResponseDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status304NotModified)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // GET /api/v1/restaurants/{restaurantId}/info
        publicGroup.MapGet("/{restaurantId:guid}/info", async (
            Guid restaurantId,
            double? lat,
            double? lng,
            ISender sender) =>
        {
            var result = await sender.Send(new GetRestaurantPublicInfoQuery(restaurantId, lat, lng));
            if (!result.IsSuccess) return result.ToIResult();

            var dto = result.Value;
            // Option B: fetch review summary separately and project optional rating fields.
            decimal? avg = null;
            int? count = null;
            var summaryRes = await sender.Send(new GetRestaurantReviewSummaryQuery(restaurantId));
            if (summaryRes.IsSuccess)
            {
                avg = (decimal)summaryRes.Value.AverageRating;
                count = summaryRes.Value.TotalReviews;
            }

            var response = new RestaurantPublicInfoResponseDto(
                dto.RestaurantId,
                dto.Name,
                dto.LogoUrl,
                dto.BackgroundImageUrl,
                dto.Description,
                dto.CuisineType,
                dto.CuisineTags,
                dto.IsAcceptingOrders,
                dto.IsVerified,
                dto.Address,
                dto.ContactInfo,
                dto.BusinessHours,
                dto.EstablishedDate,
                avg,
                count,
                dto.DistanceKm);

            return Results.Ok(response);
        })
        .WithName("GetRestaurantPublicInfo")
        .WithSummary("Get restaurant's public information")
        .WithDescription("Retrieves basic public information about a restaurant such as name, address, and contact details. Optionally calculates distance when lat and lng query parameters are provided. Public endpoint - no authentication required.")
        .Produces<RestaurantPublicInfoResponseDto>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // GET /api/v1/restaurants/search
        publicGroup.MapGet("/search", async (
            string? q,
            string? cuisine,
            double? lat,
            double? lng,
            double? radiusKm,
            int? pageNumber,
            int? pageSize,
            double? minRating,
            string? sort,
            string? bbox,
            [FromQuery(Name = "tags")] string[]? tags,
            [FromQuery(Name = "tagIds")] Guid[]? tagIds,
            [FromQuery(Name = "discountedOnly")] bool? discountedOnly,
            [FromQuery(Name = "includeFacets")] bool? includeFacets,
            ISender sender) =>
        {
            // Apply defaults after binding to avoid Minimal API early 400s for missing value-type properties
            var page = pageNumber ?? 1;
            var size = pageSize ?? 10;

            var query = new SearchRestaurantsQuery(
                q,
                cuisine,
                lat,
                lng,
                radiusKm,
                page,
                size,
                minRating,
                sort,
                bbox,
                tags?.ToList(),
                tagIds?.ToList(),
                discountedOnly,
                includeFacets ?? false);
            var result = await sender.Send(query);
            if (!result.IsSuccess)
            {
                return result.ToIResult();
            }

            return result.Value switch
            {
                RestaurantSearchPageResult pageResult => Results.Ok(pageResult.Page),
                RestaurantSearchWithFacetsDto withFacets => Results.Ok(withFacets),
                _ => Results.Ok(result.Value)
            };
        })
        .WithName("SearchRestaurants")
        .WithSummary("Search restaurants")
        .WithDescription("Searches for restaurants by name, cuisine type, tags, and/or location. Supports sort=rating|distance|popularity, discountedOnly=true to restrict results to venues with currently active coupons, and includeFacets=true to retrieve `{ page, facets }` metadata. When lat/lng are provided, returns distanceKm and allows sort=distance. Public endpoint - no authentication required.")
        .Produces<object>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    #region DTOs for Public Info (Response)
    public sealed record RestaurantPublicInfoResponseDto(
        Guid RestaurantId,
        string Name,
        string? LogoUrl,
        string? BackgroundImageUrl,
        string Description,
        string CuisineType,
        IReadOnlyList<string> CuisineTags,
        bool IsAcceptingOrders,
        bool IsVerified,
        Application.Restaurants.Queries.Common.AddressDto Address,
        Application.Restaurants.Queries.Common.ContactInfoDto ContactInfo,
        string BusinessHours,
        DateTimeOffset EstablishedDate,
        decimal? AvgRating,
        int? RatingCount,
        decimal? DistanceKm);

    public sealed record RestaurantAggregatedDetailsResponseDto(
        RestaurantPublicInfoResponseDto Info,
        RestaurantAggregatedMenuResponseDto Menu,
        RestaurantReviewSummaryDto ReviewSummary,
        DateTimeOffset LastChangedUtc);

    public sealed record RestaurantAggregatedMenuResponseDto(
        DateTimeOffset LastRebuiltAt,
        JsonElement Data);
    #endregion
}
