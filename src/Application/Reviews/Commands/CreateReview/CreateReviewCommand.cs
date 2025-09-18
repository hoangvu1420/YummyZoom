using FluentValidation;
using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.ReviewAggregate;
using YummyZoom.Domain.ReviewAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Application.Reviews.Commands.CreateReview;

[Authorize(Policy = Policies.CompletedSignup)]
public sealed record CreateReviewCommand(
    Guid OrderId,
    Guid RestaurantId,
    int Rating,
    string? Title,
    string? Comment
) : IRequest<Result<CreateReviewResponse>>, IUserCommand
{
    public required UserId UserId { get; init; }
}

public sealed record CreateReviewResponse(Guid ReviewId);

public sealed class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.RestaurantId).NotEmpty();
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Title).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.Title));
        RuleFor(x => x.Comment).MaximumLength(1000).When(x => !string.IsNullOrWhiteSpace(x.Comment));
    }
}

public sealed class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, Result<CreateReviewResponse>>
{
    private readonly IOrderRepository _orders;
    private readonly IReviewRepository _reviews;
    private readonly IUnitOfWork _uow;
    private readonly IUser _user;

    public CreateReviewCommandHandler(
        IOrderRepository orders,
        IReviewRepository reviews,
        IUnitOfWork uow,
        IUser user)
    {
        _orders = orders;
        _reviews = reviews;
        _uow = uow;
        _user = user;
    }

    public Task<Result<CreateReviewResponse>> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
        return _uow.ExecuteInTransactionAsync(async () =>
        {
            if (_user.DomainUserId is null)
            {
                return Result.Failure<CreateReviewResponse>(Error.Problem("Auth.Unauthorized", "User is not authenticated."));
            }

            if (request.UserId != _user.DomainUserId)
            {
                return Result.Failure<CreateReviewResponse>(Error.Problem("CreateReview.UserMismatch", "Authenticated user does not match request."));
            }

            var orderId = OrderId.Create(request.OrderId);
            var order = await _orders.GetByIdAsync(orderId, cancellationToken);
            if (order is null)
            {
                return Result.Failure<CreateReviewResponse>(CreateReviewErrors.OrderNotFound(request.OrderId));
            }

            // Ownership and consistency checks
            if (order.CustomerId != _user.DomainUserId)
            {
                return Result.Failure<CreateReviewResponse>(CreateReviewErrors.NotOrderOwner);
            }
            if (order.RestaurantId.Value != request.RestaurantId)
            {
                return Result.Failure<CreateReviewResponse>(CreateReviewErrors.RestaurantMismatch);
            }
            if (order.Status != OrderStatus.Delivered)
            {
                return Result.Failure<CreateReviewResponse>(CreateReviewErrors.InvalidOrderStatusForReview);
            }

            // Enforce single active review per user+restaurant
            var existing = await _reviews.GetByCustomerAndRestaurantAsync(_user.DomainUserId.Value, request.RestaurantId, cancellationToken);
            if (existing is not null)
            {
                return Result.Failure<CreateReviewResponse>(CreateReviewErrors.ReviewAlreadyExists);
            }

            // Create domain review
            var ratingResult = Rating.Create(request.Rating);
            if (ratingResult.IsFailure)
            {
                return Result.Failure<CreateReviewResponse>(ratingResult.Error);
            }

            var createResult = Review.Create(
                orderId,
                _user.DomainUserId,
                RestaurantId.Create(request.RestaurantId),
                ratingResult.Value,
                string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim());

            if (createResult.IsFailure)
            {
                return Result.Failure<CreateReviewResponse>(createResult.Error);
            }

            // Persist
            await _reviews.AddAsync(createResult.Value, cancellationToken);

            return Result.Success(new CreateReviewResponse(createResult.Value.Id.Value));
        }, cancellationToken);
    }
}

public static class CreateReviewErrors
{
    public static Error OrderNotFound(Guid orderId) => Error.NotFound(
        "CreateReview.OrderNotFound", $"Order '{orderId}' was not found.");

    public static Error NotOrderOwner => Error.Validation(
        "CreateReview.NotOrderOwner", "You do not own this order.");

    public static Error RestaurantMismatch => Error.Validation(
        "CreateReview.RestaurantMismatch", "Order does not belong to the specified restaurant.");

    public static Error InvalidOrderStatusForReview => Error.Validation(
        "CreateReview.InvalidOrderStatusForReview", "Order is not eligible for review.");

    public static Error ReviewAlreadyExists => Error.Conflict(
        "CreateReview.ReviewAlreadyExists", "You have already reviewed this restaurant.");
}
