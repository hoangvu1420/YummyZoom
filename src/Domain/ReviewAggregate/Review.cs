using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.ReviewAggregate.Errors;
using YummyZoom.Domain.ReviewAggregate.Events;
using YummyZoom.Domain.ReviewAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.ReviewAggregate;

public sealed class Review : AggregateRoot<ReviewId, Guid>
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
            (ReviewId)review.Id,
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
            (ReviewId)Id,
            DateTime.UtcNow));

        return Result.Success();
    }

    public Result Hide()
    {
        IsHidden = true;
        return Result.Success();
    }

    public Result Show()
    {
        IsHidden = false;
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
            (ReviewId)Id,
            reply,
            DateTime.UtcNow));

        return Result.Success();
    }

#pragma warning disable CS8618
    private Review()
    {
    }
#pragma warning restore CS8618
}
