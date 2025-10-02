using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.ReviewAggregate.Errors;
using YummyZoom.Domain.ReviewAggregate.Events;
using YummyZoom.Domain.ReviewAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.ReviewAggregate;

public sealed class Review : AggregateRoot<ReviewId, Guid>, IAuditableEntity, ISoftDeletableEntity
{
    public OrderId OrderId { get; private set; }
    public UserId CustomerId { get; private set; }
    public RestaurantId RestaurantId { get; private set; }
    public Rating Rating { get; private set; }
    public string? Comment { get; private set; }
    public DateTime SubmissionTimestamp { get; private set; }
    public bool IsModerated { get; private set; }
    public bool IsHidden { get; private set; }
    public string? Reply { get; private set; }

    // Properties from IAuditableEntity
    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public string? LastModifiedBy { get; set; }

    // Properties from ISoftDeletableEntity
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedOn { get; set; }
    public string? DeletedBy { get; set; }

    private Review(
        ReviewId id,
        OrderId orderId,
        UserId customerId,
        RestaurantId restaurantId,
        Rating rating,
        string? comment,
        DateTime submissionTimestamp,
        bool isModerated,
        bool isHidden,
        string? reply)
        : base(id)
    {
        OrderId = orderId;
        CustomerId = customerId;
        RestaurantId = restaurantId;
        Rating = rating;
        Comment = comment;
        SubmissionTimestamp = submissionTimestamp;
        IsModerated = isModerated;
        IsHidden = isHidden;
        Reply = reply;
    }

    public static Result<Review> Create(
        OrderId orderId,
        UserId customerId,
        RestaurantId restaurantId,
        Rating rating,
        string? comment = null)
    {
        if (orderId is null)
        {
            return Result.Failure<Review>(ReviewErrors.InvalidOrderId);
        }

        if (customerId is null)
        {
            return Result.Failure<Review>(ReviewErrors.InvalidCustomerId);
        }

        if (restaurantId is null)
        {
            return Result.Failure<Review>(ReviewErrors.InvalidRestaurantId);
        }

        var review = new Review(
            ReviewId.CreateUnique(),
            orderId,
            customerId,
            restaurantId,
            rating,
            comment,
            DateTime.UtcNow,
            isModerated: false,
            isHidden: false,
            reply: null);

        // Add domain event
        review.AddDomainEvent(new ReviewCreated(
            review.Id,
            orderId,
            customerId,
            restaurantId,
            rating,
            review.SubmissionTimestamp));

        return Result.Success(review);
    }

    public Result MarkAsModerated()
    {
        if (IsModerated)
        {
            return Result.Success();
        }

        IsModerated = true;

        AddDomainEvent(new ReviewModerated(
            Id,
            DateTime.UtcNow));

        return Result.Success();
    }

    public Result Hide()
    {
        if (IsHidden)
        {
            return Result.Success(); // Already hidden, no need to raise event
        }

        IsHidden = true;

        AddDomainEvent(new ReviewHidden(
            Id,
            DateTime.UtcNow));

        return Result.Success();
    }

    public Result Show()
    {
        if (!IsHidden)
        {
            return Result.Success(); // Already shown, no need to raise event
        }

        IsHidden = false;

        AddDomainEvent(new ReviewShown(
            Id,
            DateTime.UtcNow));

        return Result.Success();
    }

    public Result AddReply(string reply)
    {
        if (string.IsNullOrWhiteSpace(reply))
        {
            return Result.Failure(ReviewErrors.EmptyReply);
        }

        if (!string.IsNullOrEmpty(Reply))
        {
            return Result.Failure(ReviewErrors.ReviewAlreadyReplied);
        }

        Reply = reply;

        AddDomainEvent(new ReviewReplied(
            Id,
            reply,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Marks this review as deleted (soft delete).
    /// </summary>
    /// <param name="deletedOn">The timestamp when the review was deleted</param>
    /// <param name="deletedBy">The user who deleted the review</param>
    /// <returns>A Result indicating success</returns>
    public Result MarkAsDeleted(DateTimeOffset deletedOn, string? deletedBy = null)
    {
        if (IsDeleted)
        {
            // Already deleted, consider this a success
            return Result.Success();
        }

        IsDeleted = true;
        DeletedOn = deletedOn;
        DeletedBy = deletedBy;

        AddDomainEvent(new ReviewDeleted(Id));

        return Result.Success();
    }

#pragma warning disable CS8618
    private Review()
    {
    }
#pragma warning restore CS8618
}
