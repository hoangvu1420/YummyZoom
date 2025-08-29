using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.MenuEntity;
using YummyZoom.Domain.MenuEntity.Errors;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.Menus.Commands.ChangeMenuAvailability;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public record ChangeMenuAvailabilityCommand(
    Guid RestaurantId,
    Guid MenuId,
    bool IsEnabled
) : IRequest<Result>, IRestaurantCommand
{
    RestaurantId IRestaurantCommand.RestaurantId => Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public class ChangeMenuAvailabilityCommandHandler : IRequestHandler<ChangeMenuAvailabilityCommand, Result>
{
    private readonly IMenuRepository _menuRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeMenuAvailabilityCommandHandler(IMenuRepository menuRepository, IUnitOfWork unitOfWork)
    {
        _menuRepository = menuRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ChangeMenuAvailabilityCommand request, CancellationToken cancellationToken)
    {
        var menuId = MenuId.Create(request.MenuId);
        var menu = await _menuRepository.GetByIdAsync(menuId, cancellationToken);

        if (menu is null)
        {
            return Result.Failure(MenuErrors.InvalidMenuId);
        }

        if (request.IsEnabled)
        {
            menu.Enable();
        }
        else
        {
            menu.Disable();
        }

        _menuRepository.Update(menu);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
