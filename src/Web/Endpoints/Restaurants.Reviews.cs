using MediatR;
using Microsoft.AspNetCore.Routing;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Reviews.Commands.CreateReview;
using YummyZoom.Application.Reviews.Commands.DeleteReview;
using YummyZoom.Application.Reviews.Queries.Common;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Web.Endpoints;

public partial class Restaurants
{
    private static void MapReviews(IEndpointRouteBuilder group)
    {
        // POST /api/v1/restaurants/{restaurantId}/reviews
        group.MapPost("/{restaurantId:guid}/reviews", async (Guid restaurantId, CreateReviewRequest body, ISender sender, IUser user) =>
        {
            var uid = user.DomainUserId ?? (user.Id is string sid && Guid.TryParse(sid, out var gid) ? UserId.Create(gid) : throw new UnauthorizedAccessException());
            var cmd = new CreateReviewCommand(
                OrderId: body.OrderId,
                RestaurantId: restaurantId,
                Rating: body.Rating,
                Title: body.Title,
                Comment: body.Comment)
            { UserId = uid };
            var result = await sender.Send(cmd);
            return result.IsSuccess
                ? Results.Created($"/api/v1/restaurants/{restaurantId}/reviews/{result.Value.ReviewId}", new { reviewId = result.Value.ReviewId })
                : result.ToIResult();
        })
        .WithName("CreateReview")
        .WithSummary("Create a review for a delivered order at this restaurant")
        .WithDescription("Requires CompletedSignup; user must own the order and the order must be Delivered.")
        .WithStandardCreationResults<CreateReviewResponse>();

        // DELETE /api/v1/restaurants/{restaurantId}/reviews/{reviewId}
        group.MapDelete("/{restaurantId:guid}/reviews/{reviewId:guid}", async (Guid restaurantId, Guid reviewId, ISender sender, IUser user) =>
        {
            var uid = user.DomainUserId ?? (user.Id is string sid && Guid.TryParse(sid, out var gid) ? UserId.Create(gid) : throw new UnauthorizedAccessException());
            var cmd = new DeleteReviewCommand(reviewId) { UserId = uid };
            var result = await sender.Send(cmd);
            return result.IsSuccess
                ? Results.Ok()
                : result.ToIResult();
        })
        .WithName("DeleteReview")
        .WithSummary("Delete my review")
        .WithDescription("Requires CompletedSignup; user must own the review.")
        .WithStandardResults();
    }

    #region DTOs for Reviews
    public sealed record CreateReviewRequest(Guid OrderId, int Rating, string? Title, string? Comment);
    #endregion
}
