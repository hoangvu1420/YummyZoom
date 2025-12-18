using FluentValidation;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Security;
using YummyZoom.Application.Common.Authorization;
using YummyZoom.Domain.CustomizationGroupAggregate;
using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

using YummyZoom.Domain.CustomizationGroupAggregate.Errors;
namespace YummyZoom.Application.CustomizationGroups.Commands.RemoveCustomizationChoice;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record RemoveCustomizationChoiceCommand(
    Guid RestaurantId,
    Guid GroupId,
    Guid ChoiceId) : IRequest<Result>, IRestaurantCommand
{
    Domain.RestaurantAggregate.ValueObjects.RestaurantId IRestaurantCommand.RestaurantId => Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public sealed class RemoveCustomizationChoiceCommandValidator : AbstractValidator<RemoveCustomizationChoiceCommand>
{
    public RemoveCustomizationChoiceCommandValidator()
    {
        RuleFor(x => x.RestaurantId).NotEmpty();
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.ChoiceId).NotEmpty();
    }
}

public sealed class RemoveCustomizationChoiceCommandHandler : IRequestHandler<RemoveCustomizationChoiceCommand, Result>
{
    private readonly ICustomizationGroupRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveCustomizationChoiceCommandHandler(ICustomizationGroupRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RemoveCustomizationChoiceCommand request, CancellationToken cancellationToken)
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

        var result = group.RemoveChoice(ChoiceId.Create(request.ChoiceId));

        if (result.IsFailure)
        {
            return result;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
