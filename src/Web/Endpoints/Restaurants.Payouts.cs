using Microsoft.AspNetCore.Mvc;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Payouts.Commands.RequestPayout;
using YummyZoom.Application.Payouts.Queries.Common;
using YummyZoom.Application.Payouts.Queries.GetPayoutDetails;
using YummyZoom.Application.Payouts.Queries.GetPayoutEligibility;
using YummyZoom.Application.Payouts.Queries.ListPayouts;

namespace YummyZoom.Web.Endpoints;

public partial class Restaurants
{
    private static void MapPayouts(IEndpointRouteBuilder group)
    {
        // GET /api/v1/restaurants/{restaurantId}/account/payout-eligibility
        group.MapGet("/{restaurantId:guid}/account/payout-eligibility", async (
            Guid restaurantId,
            ISender sender,
            CancellationToken ct) =>
        {
            var query = new GetPayoutEligibilityQuery(restaurantId);
            var result = await sender.Send(query, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("GetRestaurantPayoutEligibility")
        .WithSummary("Get payout eligibility")
        .WithDescription("Returns payout eligibility and available balance for a restaurant. Requires restaurant staff authorization.")
        .WithStandardResults<PayoutEligibilityDto>();

        // POST /api/v1/restaurants/{restaurantId}/account/payouts
        group.MapPost("/{restaurantId:guid}/account/payouts", async (
            Guid restaurantId,
            [FromBody] RequestPayoutRequestDto body,
            ISender sender,
            CancellationToken ct) =>
        {
            var command = new RequestPayoutCommand(
                RestaurantGuid: restaurantId,
                Amount: body.Amount,
                IdempotencyKey: body.IdempotencyKey);
            var result = await sender.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/restaurants/{restaurantId}/account/payouts/{result.Value.PayoutId}", result.Value)
                : result.ToIResult();
        })
        .WithName("RequestRestaurantPayout")
        .WithSummary("Request a payout")
        .WithDescription("Submits a payout request for the restaurant. Requires restaurant owner authorization.")
        .WithStandardCreationResults<RequestPayoutResponse>();

        // GET /api/v1/restaurants/{restaurantId}/account/payouts
        group.MapGet("/{restaurantId:guid}/account/payouts", async (
            Guid restaurantId,
            [AsParameters] ListPayoutsRequestDto queryParams,
            ISender sender,
            CancellationToken ct) =>
        {
            var query = new ListPayoutsQuery(
                RestaurantGuid: restaurantId,
                Status: queryParams.Status,
                From: queryParams.From,
                To: queryParams.To,
                PageNumber: queryParams.PageNumber ?? 1,
                PageSize: queryParams.PageSize ?? 20);
            var result = await sender.Send(query, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("ListRestaurantPayouts")
        .WithSummary("List payout requests")
        .WithDescription("Returns paginated payout history for a restaurant with optional filters. Requires restaurant staff authorization.")
        .WithStandardResults<PaginatedList<PayoutSummaryDto>>();

        // GET /api/v1/restaurants/{restaurantId}/account/payouts/{payoutId}
        group.MapGet("/{restaurantId:guid}/account/payouts/{payoutId:guid}", async (
            Guid restaurantId,
            Guid payoutId,
            ISender sender,
            CancellationToken ct) =>
        {
            var query = new GetPayoutDetailsQuery(restaurantId, payoutId);
            var result = await sender.Send(query, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("GetRestaurantPayoutDetails")
        .WithSummary("Get payout details")
        .WithDescription("Returns payout details for a restaurant. Requires restaurant staff authorization.")
        .WithStandardResults<PayoutDetailsDto>();
    }

    private sealed record RequestPayoutRequestDto(decimal? Amount, string? IdempotencyKey);

    private sealed record ListPayoutsRequestDto(
        string? Status,
        DateTimeOffset? From,
        DateTimeOffset? To,
        int? PageNumber,
        int? PageSize);
}
