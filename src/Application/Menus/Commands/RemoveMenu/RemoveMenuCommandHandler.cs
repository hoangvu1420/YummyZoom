using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.MenuEntity.Errors;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Menus.Commands.RemoveMenu;

public sealed class RemoveMenuCommandHandler : IRequestHandler<RemoveMenuCommand, Result>
{
    private readonly IMenuRepository _menuRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveMenuCommandHandler(
        IMenuRepository menuRepository,
        IUnitOfWork unitOfWork)
    {
        _menuRepository = menuRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RemoveMenuCommand request, CancellationToken cancellationToken)
    {
        var menuId = MenuId.Create(request.MenuId);
        var menu = await _menuRepository.GetByIdAsync(menuId, cancellationToken);

        if (menu is null)
        {
            return Result.Failure(MenuErrors.MenuNotFound);
        }

        if (menu.RestaurantId != RestaurantId.Create(request.RestaurantId))
        {
            // If the menu belongs to another restaurant, we treat it as Not Found for security/consistency
            return Result.Failure(MenuErrors.MenuNotFound);
        }

        // Ideally we'd use an IDateTimeProvider and ICurrentUser, but for now we rely on simple aggregation
        var result = menu.MarkAsDeleted(DateTimeOffset.UtcNow, null);
        if (result.IsFailure)
        {
            return result;
        }

        _menuRepository.Update(menu);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
