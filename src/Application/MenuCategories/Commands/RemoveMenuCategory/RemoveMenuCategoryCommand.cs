using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.MenuEntity;
using YummyZoom.Domain.MenuEntity.Errors;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.MenuCategories.Commands.RemoveMenuCategory;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public record RemoveMenuCategoryCommand(
    Guid RestaurantId,
    Guid MenuCategoryId
) : IRequest<Result>, IRestaurantCommand
{
    RestaurantId IRestaurantCommand.RestaurantId => Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public class RemoveMenuCategoryCommandHandler : IRequestHandler<RemoveMenuCategoryCommand, Result>
{
    private readonly IMenuCategoryRepository _menuCategoryRepository;
    private readonly IMenuRepository _menuRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveMenuCategoryCommandHandler(
        IMenuCategoryRepository menuCategoryRepository,
        IMenuRepository menuRepository,
        IUnitOfWork unitOfWork)
    {
        _menuCategoryRepository = menuCategoryRepository;
        _menuRepository = menuRepository;
        _unitOfWork = unitOfWork;
    }

    public Task<Result> Handle(RemoveMenuCategoryCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var categoryId = MenuCategoryId.Create(request.MenuCategoryId);
            var category = await _menuCategoryRepository.GetByIdIncludingDeletedAsync(categoryId, cancellationToken);

            if (category is null)
            {
                return Result.Failure(MenuErrors.CategoryNotFound(request.MenuCategoryId.ToString()));
            }

            // Verify restaurant ownership by checking the category's menu
            // We need to check ownership even for deleted categories to ensure security
            var menu = await _menuRepository.GetByIdAsync(category.MenuId, cancellationToken);
            if (menu is null || menu.RestaurantId.Value != request.RestaurantId)
            {
                return Result.Failure(MenuErrors.CategoryNotFound(request.MenuCategoryId.ToString()));
            }

            // Mark the category as deleted (soft delete)
            var deleteResult = category.MarkAsDeleted(DateTimeOffset.UtcNow);
            if (deleteResult.IsFailure)
            {
                return Result.Failure(deleteResult.Error);
            }

            _menuCategoryRepository.Update(category);

            return Result.Success();
        }, cancellationToken);
    }
}
