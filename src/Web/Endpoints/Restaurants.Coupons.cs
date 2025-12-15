using Microsoft.AspNetCore.Mvc;
using YummyZoom.Application.Coupons.Commands.CreateCoupon;
using YummyZoom.Application.Coupons.Commands.DeleteCoupon;
using YummyZoom.Application.Coupons.Commands.DisableCoupon;
using YummyZoom.Application.Coupons.Commands.EnableCoupon;
using YummyZoom.Application.Coupons.Commands.UpdateCoupon;
using YummyZoom.Application.Coupons.Queries.GetCouponDetails;
using YummyZoom.Application.Coupons.Queries.GetCouponStats;
using YummyZoom.Application.Coupons.Queries.ListCouponsByRestaurant;
using YummyZoom.Domain.CouponAggregate.ValueObjects;

namespace YummyZoom.Web.Endpoints;

public partial class Restaurants
{
    private static void MapCoupons(IEndpointRouteBuilder group)
    {
        // GET /api/v1/restaurants/{restaurantId}/coupons
        group.MapGet("/{restaurantId:guid}/coupons", async (Guid restaurantId, [AsParameters] ListCouponsRequestDto queryParams, ISender sender, CancellationToken ct) =>
        {
            var query = new ListCouponsByRestaurantQuery(
                RestaurantId: restaurantId,
                PageNumber: queryParams.PageNumber ?? 1,
                PageSize: queryParams.PageSize ?? 20,
                Q: queryParams.Q,
                IsEnabled: queryParams.Enabled,
                ActiveFrom: queryParams.From,
                ActiveTo: queryParams.To);
            var result = await sender.Send(query, ct);
            return result.ToIResult();
        })
        .WithName("ListCoupons")
        .WithSummary("List coupons for a restaurant")
        .WithDescription("Returns paginated coupons with optional filters for code, description, enabled status, and validity window. Requires restaurant staff authorization.")
        .WithStandardResults();

        // GET /api/v1/restaurants/{restaurantId}/coupons/{couponId}
        group.MapGet("/{restaurantId:guid}/coupons/{couponId:guid}", async (Guid restaurantId, Guid couponId, ISender sender, CancellationToken ct) =>
        {
            var query = new GetCouponDetailsQuery(restaurantId, couponId);
            var result = await sender.Send(query, ct);
            return result.ToIResult();
        })
        .WithName("GetCouponDetails")
        .WithSummary("Get coupon details")
        .WithDescription("Returns full coupon details including applies-to scope and usage settings. Requires restaurant staff authorization.")
        .WithStandardResults();

        // GET /api/v1/restaurants/{restaurantId}/coupons/{couponId}/stats
        group.MapGet("/{restaurantId:guid}/coupons/{couponId:guid}/stats", async (Guid restaurantId, Guid couponId, ISender sender, CancellationToken ct) =>
        {
            var query = new GetCouponStatsQuery(restaurantId, couponId);
            var result = await sender.Send(query, ct);
            return result.ToIResult();
        })
        .WithName("GetCouponStats")
        .WithSummary("Get coupon usage stats")
        .WithDescription("Returns total usage count, unique users, and last used timestamp for a coupon. Requires restaurant staff authorization.")
        .WithStandardResults();

        // POST /api/v1/restaurants/{restaurantId}/coupons
        group.MapPost("/{restaurantId:guid}/coupons", async (Guid restaurantId, CreateCouponRequestDto body, ISender sender) =>
        {
            var cmd = new CreateCouponCommand(
                RestaurantId: restaurantId,
                Code: body.Code,
                Description: body.Description,
                ValueType: body.ValueType,
                Percentage: body.Percentage,
                FixedAmount: body.FixedAmount,
                FixedCurrency: body.FixedCurrency,
                FreeItemId: body.FreeItemId,
                Scope: body.Scope,
                ItemIds: body.ItemIds,
                CategoryIds: body.CategoryIds,
                ValidityStartDate: body.ValidityStartDate,
                ValidityEndDate: body.ValidityEndDate,
                MinOrderAmount: body.MinOrderAmount,
                MinOrderCurrency: body.MinOrderCurrency,
                TotalUsageLimit: body.TotalUsageLimit,
                UsageLimitPerUser: body.UsageLimitPerUser,
                IsEnabled: body.IsEnabled ?? true);
            var result = await sender.Send(cmd);
            return result.IsSuccess
                ? Results.Created($"/api/v1/restaurants/{restaurantId}/coupons/{result.Value.CouponId}", result.Value)
                : result.ToIResult();
        })
        .WithName("CreateCoupon")
        .WithSummary("Create a new coupon")
        .WithDescription("Creates a new coupon for the restaurant. Requires restaurant staff authorization.")
        .WithStandardCreationResults<CreateCouponResponse>();

        // PUT /api/v1/restaurants/{restaurantId}/coupons/{couponId}/enable
        group.MapPut("/{restaurantId:guid}/coupons/{couponId:guid}/enable", async (Guid restaurantId, Guid couponId, ISender sender) =>
        {
            var cmd = new EnableCouponCommand(restaurantId, couponId);
            var result = await sender.Send(cmd);
            return result.ToIResult();
        })
        .WithName("EnableCoupon")
        .WithSummary("Enable a coupon")
        .WithDescription("Enables a coupon so it can be used. Requires restaurant staff authorization.")
        .WithStandardResults();

        // PUT /api/v1/restaurants/{restaurantId}/coupons/{couponId}/disable
        group.MapPut("/{restaurantId:guid}/coupons/{couponId:guid}/disable", async (Guid restaurantId, Guid couponId, ISender sender) =>
        {
            var cmd = new DisableCouponCommand(restaurantId, couponId);
            var result = await sender.Send(cmd);
            return result.ToIResult();
        })
        .WithName("DisableCoupon")
        .WithSummary("Disable a coupon")
        .WithDescription("Disables a coupon immediately. Requires restaurant staff authorization.")
        .WithStandardResults();

        // DELETE /api/v1/restaurants/{restaurantId}/coupons/{couponId}
        group.MapDelete("/{restaurantId:guid}/coupons/{couponId:guid}", async (Guid restaurantId, Guid couponId, ISender sender) =>
        {
            var cmd = new DeleteCouponCommand(restaurantId, couponId);
            var result = await sender.Send(cmd);
            return result.ToIResult();
        })
        .WithName("DeleteCoupon")
        .WithSummary("Delete a coupon")
        .WithDescription("Soft-deletes a coupon so it can no longer be used. Requires restaurant staff authorization.")
        .WithStandardResults();

        // PUT /api/v1/restaurants/{restaurantId}/coupons/{couponId}
        group.MapPut("/{restaurantId:guid}/coupons/{couponId:guid}", async (Guid restaurantId, Guid couponId, UpdateCouponRequestDto body, ISender sender) =>
        {
            var cmd = new UpdateCouponCommand(
                RestaurantId: restaurantId,
                CouponId: couponId,
                Description: body.Description,
                ValidityStartDate: body.ValidityStartDate,
                ValidityEndDate: body.ValidityEndDate,
                ValueType: body.ValueType,
                Percentage: body.Percentage,
                FixedAmount: body.FixedAmount,
                FixedCurrency: body.FixedCurrency,
                FreeItemId: body.FreeItemId,
                Scope: body.Scope,
                ItemIds: body.ItemIds,
                CategoryIds: body.CategoryIds,
                MinOrderAmount: body.MinOrderAmount,
                MinOrderCurrency: body.MinOrderCurrency,
                TotalUsageLimit: body.TotalUsageLimit,
                UsageLimitPerUser: body.UsageLimitPerUser);
            var result = await sender.Send(cmd);
            return result.ToIResult();
        })
        .WithName("UpdateCoupon")
        .WithSummary("Update an existing coupon")
        .WithDescription("Updates description, value, min order, and applies-to scope. Requires restaurant staff authorization.")
        .WithStandardResults();
    }

    #region DTOs for Restaurant Settings
    public sealed record ListCouponsRequestDto(
        int? PageNumber,
        int? PageSize,
        string? Q,
        bool? Enabled,
        DateTime? From,
        DateTime? To);

    public sealed record CreateCouponRequestDto(
        string Code,
        string Description,
        CouponType ValueType,
        decimal? Percentage,
        decimal? FixedAmount,
        string? FixedCurrency,
        Guid? FreeItemId,
        CouponScope Scope,
        List<Guid>? ItemIds,
        List<Guid>? CategoryIds,
        DateTime ValidityStartDate,
        DateTime ValidityEndDate,
        decimal? MinOrderAmount,
        string? MinOrderCurrency,
        int? TotalUsageLimit,
        int? UsageLimitPerUser,
        bool? IsEnabled);

    public sealed record UpdateCouponRequestDto(
        string Description,
        DateTime ValidityStartDate,
        DateTime ValidityEndDate,
        CouponType ValueType,
        decimal? Percentage,
        decimal? FixedAmount,
        string? FixedCurrency,
        Guid? FreeItemId,
        CouponScope Scope,
        List<Guid>? ItemIds,
        List<Guid>? CategoryIds,
        decimal? MinOrderAmount,
        string? MinOrderCurrency,
        int? TotalUsageLimit,
        int? UsageLimitPerUser);
    #endregion
}
