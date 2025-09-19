using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Reviews.Commands.Moderation;
using YummyZoom.Application.Reviews.Queries.Moderation;
using YummyZoom.SharedKernel.Constants;
using YummyZoom.Web.Infrastructure;

namespace YummyZoom.Web.Endpoints;

/// <summary>
/// Admin-only endpoints for moderating customer reviews.
/// </summary>
public sealed class AdminReviews : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/admin/reviews")
            .WithGroupName(nameof(AdminReviews))
            .WithTags(nameof(AdminReviews))
            .RequireAuthorization(new AuthorizeAttribute { Roles = Roles.Administrator });

        MapListEndpoint(group);
        MapDetailEndpoint(group);
        MapAuditEndpoint(group);
        MapModerateEndpoint(group);
        MapHideEndpoint(group);
        MapShowEndpoint(group);
    }

    private static void MapListEndpoint(RouteGroupBuilder group)
    {
        group.MapGet(
            "/",
            async (
                [FromQuery] int? pageNumber,
                [FromQuery] int? pageSize,
                [FromQuery] bool? isModerated,
                [FromQuery] bool? isHidden,
                [FromQuery] int? minRating,
                [FromQuery] int? maxRating,
                [FromQuery] bool? hasTextOnly,
                [FromQuery] Guid? restaurantId,
                [FromQuery] DateTime? fromUtc,
                [FromQuery] DateTime? toUtc,
                [FromQuery] string? search,
                [FromQuery] AdminReviewListSort? sortBy,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var query = new ListReviewsForModerationQuery(
                    PageNumber: pageNumber ?? 1,
                    PageSize: pageSize ?? 25,
                    IsModerated: isModerated,
                    IsHidden: isHidden,
                    MinRating: minRating,
                    MaxRating: maxRating,
                    HasTextOnly: hasTextOnly,
                    RestaurantId: restaurantId,
                    FromUtc: fromUtc,
                    ToUtc: toUtc,
                    Search: search,
                    SortBy: sortBy ?? AdminReviewListSort.Newest);

                var result = await sender.Send(query, cancellationToken);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
            })
            .WithName("ListReviewsForModeration")
            .WithSummary("List reviews for admin moderation")
            .WithDescription("Returns a paginated list of reviews with moderation filters.")
            .WithStandardResults<PaginatedList<AdminModerationReviewDto>>();
    }

    private static void MapDetailEndpoint(RouteGroupBuilder group)
    {
        group.MapGet(
            "/{reviewId:guid}",
            async (Guid reviewId, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetReviewDetailForAdminQuery(reviewId), cancellationToken);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
            })
            .WithName("GetReviewDetailForAdmin")
            .WithSummary("Get review detail for admin")
            .WithDescription("Returns detailed information of a review for moderation context.")
            .WithStandardResults<AdminModerationReviewDetailDto>();
    }

    private static void MapAuditEndpoint(RouteGroupBuilder group)
    {
        group.MapGet(
            "/{reviewId:guid}/audit",
            async (Guid reviewId, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetReviewAuditTrailQuery(reviewId), cancellationToken);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
            })
            .WithName("GetReviewAuditTrail")
            .WithSummary("Get review moderation audit trail")
            .WithDescription("Returns the moderation audit trail for a review.")
            .WithStandardResults<IReadOnlyList<ReviewModerationAuditDto>>();
    }

    private static void MapModerateEndpoint(RouteGroupBuilder group)
    {
        group.MapPost(
            "/{reviewId:guid}/moderate",
            async (Guid reviewId, [FromBody] ReasonRequest body, ISender sender, CancellationToken cancellationToken) =>
            {
                var command = new ModerateReviewCommand(reviewId, body?.Reason);
                var result = await sender.Send(command, cancellationToken);
                return result.ToIResult();
            })
            .WithName("ModerateReview")
            .WithSummary("Mark a review as moderated")
            .WithDescription("Admin-only: marks the specified review as moderated, optionally with a reason.")
            .WithStandardResults();
    }

    private static void MapHideEndpoint(RouteGroupBuilder group)
    {
        group.MapPost(
            "/{reviewId:guid}/hide",
            async (Guid reviewId, [FromBody] ReasonRequest body, ISender sender, CancellationToken cancellationToken) =>
            {
                var command = new HideReviewCommand(reviewId, body?.Reason);
                var result = await sender.Send(command, cancellationToken);
                return result.ToIResult();
            })
            .WithName("HideReview")
            .WithSummary("Hide a review from public visibility")
            .WithDescription("Admin-only: hides the specified review, optionally with a reason.")
            .WithStandardResults();
    }

    private static void MapShowEndpoint(RouteGroupBuilder group)
    {
        group.MapPost(
            "/{reviewId:guid}/show",
            async (Guid reviewId, [FromBody] ReasonRequest body, ISender sender, CancellationToken cancellationToken) =>
            {
                var command = new ShowReviewCommand(reviewId, body?.Reason);
                var result = await sender.Send(command, cancellationToken);
                return result.ToIResult();
            })
            .WithName("ShowReview")
            .WithSummary("Show a previously hidden review")
            .WithDescription("Admin-only: shows the specified review, optionally with a reason.")
            .WithStandardResults();
    }

    public sealed record ReasonRequest(string? Reason);
}


