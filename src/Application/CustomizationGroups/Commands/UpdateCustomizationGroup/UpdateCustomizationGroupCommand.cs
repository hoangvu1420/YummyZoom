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
namespace YummyZoom.Application.CustomizationGroups.Commands.UpdateCustomizationGroup;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record UpdateCustomizationGroupCommand(
    Guid RestaurantId,
    Guid GroupId,
    string GroupName,
    int MinSelections,
    int MaxSelections) : IRequest<Result>, IRestaurantCommand
{
    Domain.RestaurantAggregate.ValueObjects.RestaurantId IRestaurantCommand.RestaurantId => Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public sealed class UpdateCustomizationGroupCommandValidator : AbstractValidator<UpdateCustomizationGroupCommand>
{
    public UpdateCustomizationGroupCommandValidator()
    {
        RuleFor(x => x.RestaurantId).NotEmpty();
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.GroupName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.MinSelections).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxSelections).GreaterThanOrEqualTo(0);
        RuleFor(x => x)
            .Must(x => x.MaxSelections >= x.MinSelections)
            .WithMessage("Max selections must be greater than or equal to min selections.");
    }
}

public sealed class UpdateCustomizationGroupCommandHandler : IRequestHandler<UpdateCustomizationGroupCommand, Result>
{
    private readonly ICustomizationGroupRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCustomizationGroupCommandHandler(ICustomizationGroupRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateCustomizationGroupCommand request, CancellationToken cancellationToken)
    {
        var group = await _repository.GetByIdAsync(CustomizationGroupId.Create(request.GroupId), cancellationToken);

        if (group is null)
        {
            return Result.Failure(CustomizationGroupErrors.NotFound);
        }

        // Ensure group belongs to the restaurant
        if (group.RestaurantId.Value != request.RestaurantId)
        {
            return Result.Failure(CustomizationGroupErrors.NotFound);
        }

        var result = group.UpdateGroupDetails(request.GroupName, request.MinSelections, request.MaxSelections);

        if (result.IsFailure)
        {
            return result;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
