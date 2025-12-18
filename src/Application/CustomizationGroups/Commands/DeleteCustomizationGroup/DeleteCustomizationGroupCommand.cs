using FluentValidation;
using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.CustomizationGroupAggregate.Errors;
using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.CustomizationGroups.Commands.DeleteCustomizationGroup;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record DeleteCustomizationGroupCommand(
    Guid RestaurantId,
    Guid GroupId) : IRequest<Result>, IRestaurantCommand
{
    Domain.RestaurantAggregate.ValueObjects.RestaurantId IRestaurantCommand.RestaurantId => Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public sealed class DeleteCustomizationGroupCommandValidator : AbstractValidator<DeleteCustomizationGroupCommand>
{
    public DeleteCustomizationGroupCommandValidator()
    {
        RuleFor(x => x.RestaurantId).NotEmpty();
        RuleFor(x => x.GroupId).NotEmpty();
    }
}

public sealed class DeleteCustomizationGroupCommandHandler : IRequestHandler<DeleteCustomizationGroupCommand, Result>
{
    private readonly ICustomizationGroupRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUser _currentUser;

    public DeleteCustomizationGroupCommandHandler(
        ICustomizationGroupRepository repository,
        IUnitOfWork unitOfWork,
        IUser currentUser)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteCustomizationGroupCommand request, CancellationToken cancellationToken)
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

        var result = group.MarkAsDeleted(DateTimeOffset.UtcNow, _currentUser.Id);

        if (result.IsFailure)
        {
            return result;
        }

        // We use Update here because MarkAsDeleted modifies the entity state, it doesn't remove it from the DB (soft delete)
        _repository.Update(group);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
