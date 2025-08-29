using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.MenuEntity;
using YummyZoom.Domain.MenuEntity.Errors;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.MenuCategories.Commands.AddMenuCategory;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public record AddMenuCategoryCommand(
    Guid RestaurantId,
    Guid MenuId,
    string Name
) : IRequest<Result<AddMenuCategoryResponse>>, IRestaurantCommand
{
    RestaurantId IRestaurantCommand.RestaurantId => Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public record AddMenuCategoryResponse(Guid MenuCategoryId);

public class AddMenuCategoryCommandHandler : IRequestHandler<AddMenuCategoryCommand, Result<AddMenuCategoryResponse>>
{
    private readonly IMenuRepository _menuRepository;
    private readonly IMenuCategoryRepository _menuCategoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddMenuCategoryCommandHandler(
        IMenuRepository menuRepository,
        IMenuCategoryRepository menuCategoryRepository,
        IUnitOfWork unitOfWork)
    {
        _menuRepository = menuRepository;
        _menuCategoryRepository = menuCategoryRepository;
        _unitOfWork = unitOfWork;
    }

    public Task<Result<AddMenuCategoryResponse>> Handle(AddMenuCategoryCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var menuId = MenuId.Create(request.MenuId);
            var menu = await _menuRepository.GetByIdAsync(menuId, cancellationToken);

            if (menu is null || menu.RestaurantId.Value != request.RestaurantId)
            {
                return Result.Failure<AddMenuCategoryResponse>(MenuErrors.InvalidMenuId);
            }

            var categories = await _menuCategoryRepository.GetByMenuIdAsync(menuId, cancellationToken);

            var displayOrder = categories.Count > 0 ? categories.Max(c => c.DisplayOrder) + 1 : 1;

            var menuCategoryResult = MenuCategory.Create(
                menuId,
                request.Name,
                displayOrder);

            if (menuCategoryResult.IsFailure)
            {
                return Result.Failure<AddMenuCategoryResponse>(menuCategoryResult.Error);
            }

            await _menuCategoryRepository.AddAsync(menuCategoryResult.Value, cancellationToken);

            return Result.Success(new AddMenuCategoryResponse(menuCategoryResult.Value.Id.Value));
        }, cancellationToken);
    }
}
