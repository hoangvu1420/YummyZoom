using FluentValidation;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Security;
using YummyZoom.Application.Common.Authorization;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CustomizationGroupAggregate;
using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

using YummyZoom.Domain.CustomizationGroupAggregate.Errors;
namespace YummyZoom.Application.CustomizationGroups.Commands.UpdateCustomizationChoice;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record UpdateCustomizationChoiceCommand(
    Guid RestaurantId,
    Guid GroupId,
    Guid ChoiceId,
    string Name,
    decimal PriceAmount,
    string PriceCurrency,
    bool IsDefault,
    int? DisplayOrder) : IRequest<Result>, IRestaurantCommand
{
    Domain.RestaurantAggregate.ValueObjects.RestaurantId IRestaurantCommand.RestaurantId => Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public sealed class UpdateCustomizationChoiceCommandValidator : AbstractValidator<UpdateCustomizationChoiceCommand>
{
    public UpdateCustomizationChoiceCommandValidator()
    {
        RuleFor(x => x.RestaurantId).NotEmpty();
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.ChoiceId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PriceAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PriceCurrency).NotEmpty().Length(3);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0).When(x => x.DisplayOrder.HasValue);
    }
}

public sealed class UpdateCustomizationChoiceCommandHandler : IRequestHandler<UpdateCustomizationChoiceCommand, Result>
{
    private readonly ICustomizationGroupRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCustomizationChoiceCommandHandler(ICustomizationGroupRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateCustomizationChoiceCommand request, CancellationToken cancellationToken)
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

        var price = new Money(request.PriceAmount, request.PriceCurrency);
        var result = group.UpdateChoice(
            ChoiceId.Create(request.ChoiceId),
            request.Name,
            price,
            request.IsDefault,
            request.DisplayOrder);

        if (result.IsFailure)
        {
            return result;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
