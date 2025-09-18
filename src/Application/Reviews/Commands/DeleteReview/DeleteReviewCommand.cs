using FluentValidation;
using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.ReviewAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.Reviews.Commands.DeleteReview;

[Authorize(Policy = Policies.MustBeUserOwner)]
public sealed record DeleteReviewCommand(
    Guid ReviewId
) : IRequest<Result>, IUserCommand
{
    public required YummyZoom.Domain.UserAggregate.ValueObjects.UserId UserId { get; init; }
}

public sealed class DeleteReviewCommandValidator : AbstractValidator<DeleteReviewCommand>
{
    public DeleteReviewCommandValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty();
    }
}

public sealed class DeleteReviewCommandHandler : IRequestHandler<DeleteReviewCommand, Result>
{
    private readonly IReviewRepository _reviews;
    private readonly IUnitOfWork _uow;
    private readonly IUser _user;

    public DeleteReviewCommandHandler(IReviewRepository reviews, IUnitOfWork uow, IUser user)
    {
        _reviews = reviews;
        _uow = uow;
        _user = user;
    }

    public Task<Result> Handle(DeleteReviewCommand request, CancellationToken cancellationToken)
    {
        return _uow.ExecuteInTransactionAsync(async () =>
        {
            if (_user.DomainUserId is null)
            {
                return Result.Failure(Error.Problem("Auth.Unauthorized", "User is not authenticated."));
            }

            var reviewId = ReviewId.Create(request.ReviewId);
            var review = await _reviews.GetByIdAsync(reviewId, cancellationToken);
            if (review is null)
            {
                return Result.Failure(DeleteReviewErrors.NotFound(request.ReviewId));
            }

            if (review.CustomerId != _user.DomainUserId)
            {
                return Result.Failure(DeleteReviewErrors.NotOwner);
            }

            var mark = review.MarkAsDeleted(DateTimeOffset.UtcNow, _user.Id);
            if (mark.IsFailure)
            {
                return mark;
            }

            await _reviews.UpdateAsync(review, cancellationToken);
            return Result.Success();
        }, cancellationToken);
    }
}

public static class DeleteReviewErrors
{
    public static Error NotFound(Guid reviewId) => Error.NotFound(
        "DeleteReview.NotFound", $"Review '{reviewId}' was not found.");

    public static Error NotOwner => Error.Problem(
        "DeleteReview.NotOwner", "You can only delete your own review.");
}
