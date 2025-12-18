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
namespace YummyZoom.Application.CustomizationGroups.Commands.AddCustomizationChoice;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record AddCustomizationChoiceCommand(
    Guid RestaurantId,
    Guid GroupId,
    string Name,
    decimal PriceAmount,
    string PriceCurrency,
    bool IsDefault,
    int? DisplayOrder) : IRequest<Result<Guid>>, IRestaurantCommand
{
    Domain.RestaurantAggregate.ValueObjects.RestaurantId IRestaurantCommand.RestaurantId => Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public sealed class AddCustomizationChoiceCommandValidator : AbstractValidator<AddCustomizationChoiceCommand>
{
    public AddCustomizationChoiceCommandValidator()
    {
        RuleFor(x => x.RestaurantId).NotEmpty();
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PriceAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PriceCurrency).NotEmpty().Length(3);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0).When(x => x.DisplayOrder.HasValue);
    }
}

public sealed class AddCustomizationChoiceCommandHandler : IRequestHandler<AddCustomizationChoiceCommand, Result<Guid>>
{
    private readonly ICustomizationGroupRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public AddCustomizationChoiceCommandHandler(ICustomizationGroupRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(AddCustomizationChoiceCommand request, CancellationToken cancellationToken)
    {
        var group = await _repository.GetByIdAsync(CustomizationGroupId.Create(request.GroupId), cancellationToken);

        if (group is null)
        {
            return Result.Failure<Guid>(CustomizationGroupErrors.NotFound);
        }

        if (group.RestaurantId.Value != request.RestaurantId)
        {
            return Result.Failure<Guid>(CustomizationGroupErrors.NotFound);
        }

        var price = new Money(request.PriceAmount, request.PriceCurrency);

        // If DisplayOrder is not provided, use auto-order logic
        var result = request.DisplayOrder.HasValue
            ? group.AddChoice(request.Name, price, request.IsDefault, request.DisplayOrder.Value)
            : group.AddChoiceWithAutoOrder(request.Name, price, request.IsDefault);

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Value.Id.Value);
    }
}
