using FluentValidation;
using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.ReviewAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;
using YummyZoom.Application.Common.Caching;

namespace YummyZoom.Application.Reviews.Commands.Moderation;

[Authorize(Roles = Roles.Administrator)]
public sealed record HideReviewCommand(
    Guid ReviewId,
    string? Reason
) : IRequest<Result>;

public sealed class HideReviewCommandValidator : AbstractValidator<HideReviewCommand>
{
    public HideReviewCommandValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty();
        RuleFor(x => x.Reason)
            .Cascade(CascadeMode.Stop)
            .Must(r => r is null || !string.IsNullOrWhiteSpace(r))
            .WithMessage("Reason cannot be whitespace only.")
            .MaximumLength(500)
            .When(x => x.Reason is not null);
    }
}

public sealed class HideReviewCommandHandler : IRequestHandler<HideReviewCommand, Result>
{
    private readonly IReviewRepository _reviews;
    private readonly IUnitOfWork _uow;
    private readonly ICacheService _cache;

    public HideReviewCommandHandler(IReviewRepository reviews, IUnitOfWork uow, ICacheService cache)
    {
        _reviews = reviews;
        _uow = uow;
        _cache = cache;
    }

    public Task<Result> Handle(HideReviewCommand request, CancellationToken cancellationToken)
    {
        return _uow.ExecuteInTransactionAsync(async () =>
        {
            var reviewId = ReviewId.Create(request.ReviewId);
            var review = await _reviews.GetByIdAsync(reviewId, cancellationToken);
            if (review is null)
            {
                return Result.Failure(Error.NotFound("HideReview.NotFound", $"Review '{request.ReviewId}' was not found."));
            }

            var res = review.Hide();
            if (res.IsFailure)
            {
                return res;
            }

            await _reviews.UpdateAsync(review, cancellationToken);
            await _cache.InvalidateByTagAsync("cache:admin:review-moderation:list", cancellationToken);
            return Result.Success();
        }, cancellationToken);
    }
}


