using FluentValidation;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Security;
using YummyZoom.Application.Common.Authorization;
using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;
using YummyZoom.Domain.CustomizationGroupAggregate.Errors;

namespace YummyZoom.Application.CustomizationGroups.Commands.ReorderCustomizationChoices;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record ReorderCustomizationChoicesCommand(
    Guid RestaurantId,
    Guid GroupId,
    List<ChoiceOrderDto> ChoiceOrders) : IRequest<Result>, IRestaurantCommand
{
    Domain.RestaurantAggregate.ValueObjects.RestaurantId IRestaurantCommand.RestaurantId => Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public sealed record ChoiceOrderDto(Guid ChoiceId, int DisplayOrder);

public sealed class ReorderCustomizationChoicesCommandValidator : AbstractValidator<ReorderCustomizationChoicesCommand>
{
    public ReorderCustomizationChoicesCommandValidator()
    {
        RuleFor(x => x.RestaurantId).NotEmpty();
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.ChoiceOrders)
            .NotEmpty()
            .WithMessage("Choice orders list cannot be empty.");

        RuleForEach(x => x.ChoiceOrders).ChildRules(order =>
        {
            order.RuleFor(x => x.ChoiceId).NotEmpty();
            order.RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed class ReorderCustomizationChoicesCommandHandler : IRequestHandler<ReorderCustomizationChoicesCommand, Result>
{
    private readonly ICustomizationGroupRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ReorderCustomizationChoicesCommandHandler(ICustomizationGroupRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ReorderCustomizationChoicesCommand request, CancellationToken cancellationToken)
    {
        var group = await _repository.GetByIdAsync(CustomizationGroupId.Create(request.GroupId), cancellationToken);

        if (group is null)
        {
            return Result.Failure(CustomizationGroupErrors.NotFound);
        }

        if (group.RestaurantId.Value != request.RestaurantId)
        {
            return Result.Failure(CustomizationGroupErrors.NotFound);
        }

        var orderChanges = request.ChoiceOrders
            .Select(x => (ChoiceId.Create(x.ChoiceId), x.DisplayOrder))
            .ToList();

        var result = group.ReorderChoices(orderChanges);

        if (result.IsFailure)
        {
            return result;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
