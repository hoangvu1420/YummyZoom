using FluentValidation;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Security;
using YummyZoom.Application.Common.Authorization;
using YummyZoom.Domain.CustomizationGroupAggregate;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

using YummyZoom.Domain.CustomizationGroupAggregate.Errors;
namespace YummyZoom.Application.CustomizationGroups.Commands.CreateCustomizationGroup;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record CreateCustomizationGroupCommand(
    Guid RestaurantId,
    string GroupName,
    int MinSelections,
    int MaxSelections) : IRequest<Result<Guid>>, IRestaurantCommand
{
    Domain.RestaurantAggregate.ValueObjects.RestaurantId IRestaurantCommand.RestaurantId => Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public sealed class CreateCustomizationGroupCommandValidator : AbstractValidator<CreateCustomizationGroupCommand>
{
    public CreateCustomizationGroupCommandValidator()
    {
        RuleFor(x => x.RestaurantId)
            .NotEmpty();

        RuleFor(x => x.GroupName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.MinSelections)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.MaxSelections)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x)
            .Must(x => x.MaxSelections >= x.MinSelections)
            .WithMessage("Max selections must be greater than or equal to min selections.");
    }
}

public sealed class CreateCustomizationGroupCommandHandler : IRequestHandler<CreateCustomizationGroupCommand, Result<Guid>>
{
    private readonly ICustomizationGroupRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCustomizationGroupCommandHandler(ICustomizationGroupRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateCustomizationGroupCommand request, CancellationToken cancellationToken)
    {
        var result = CustomizationGroup.Create(
            RestaurantId.Create(request.RestaurantId),
            request.GroupName,
            request.MinSelections,
            request.MaxSelections);

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        var group = result.Value;
        await _repository.AddAsync(group, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(group.Id.Value);
    }
}
