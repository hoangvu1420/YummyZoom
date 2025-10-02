using FluentValidation;
using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Caching;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.ReviewAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.Reviews.Commands.Moderation;

[Authorize(Roles = Roles.Administrator)]
public sealed record ModerateReviewCommand(
    Guid ReviewId,
    string? Reason
) : IRequest<Result>;

public sealed class ModerateReviewCommandValidator : AbstractValidator<ModerateReviewCommand>
{
    public ModerateReviewCommandValidator()
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

public sealed class ModerateReviewCommandHandler : IRequestHandler<ModerateReviewCommand, Result>
{
    private readonly IReviewRepository _reviews;
    private readonly IUnitOfWork _uow;
    private readonly ICacheService _cache;

    public ModerateReviewCommandHandler(IReviewRepository reviews, IUnitOfWork uow, ICacheService cache)
    {
        _reviews = reviews;
        _uow = uow;
        _cache = cache;
    }

    public Task<Result> Handle(ModerateReviewCommand request, CancellationToken cancellationToken)
    {
        return _uow.ExecuteInTransactionAsync(async () =>
        {
            var reviewId = ReviewId.Create(request.ReviewId);
            var review = await _reviews.GetByIdAsync(reviewId, cancellationToken);
            if (review is null)
            {
                return Result.Failure(Error.NotFound("ModerateReview.NotFound", $"Review '{request.ReviewId}' was not found."));
            }

            var res = review.MarkAsModerated();
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


