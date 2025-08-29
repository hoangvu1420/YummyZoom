using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.MenuEntity;
using YummyZoom.Domain.MenuEntity.Errors;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.MenuCategories.Commands.UpdateMenuCategoryDetails;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public record UpdateMenuCategoryDetailsCommand(
    Guid RestaurantId,
    Guid MenuCategoryId,
    string Name,
    int DisplayOrder
) : IRequest<Result>, IRestaurantCommand
{
    RestaurantId IRestaurantCommand.RestaurantId => Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public class UpdateMenuCategoryDetailsCommandHandler : IRequestHandler<UpdateMenuCategoryDetailsCommand, Result>
{
    private readonly IMenuCategoryRepository _menuCategoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateMenuCategoryDetailsCommandHandler(
        IMenuCategoryRepository menuCategoryRepository,
        IUnitOfWork unitOfWork)
    {
        _menuCategoryRepository = menuCategoryRepository;
        _unitOfWork = unitOfWork;
    }

    public Task<Result> Handle(UpdateMenuCategoryDetailsCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var categoryId = MenuCategoryId.Create(request.MenuCategoryId);
            var category = await _menuCategoryRepository.GetByIdAsync(categoryId, cancellationToken);

            if (category is null)
            {
                return Result.Failure(MenuErrors.CategoryNotFound(request.MenuCategoryId.ToString()));
            }

            // Verify restaurant ownership by checking categories for the restaurant
            var restaurantId = RestaurantId.Create(request.RestaurantId);
            var categoriesForRestaurant = await _menuCategoryRepository.GetByRestaurantIdAsync(restaurantId, cancellationToken);
            
            if (!categoriesForRestaurant.Any(c => c.Id == categoryId))
            {
                return Result.Failure(MenuErrors.CategoryNotFound(request.MenuCategoryId.ToString()));
            }

            // Update name if different
            if (category.Name != request.Name)
            {
                var nameUpdateResult = category.UpdateName(request.Name);
                if (nameUpdateResult.IsFailure)
                {
                    return Result.Failure(nameUpdateResult.Error);
                }
            }

            // Update display order if different
            if (category.DisplayOrder != request.DisplayOrder)
            {
                var displayOrderUpdateResult = category.UpdateDisplayOrder(request.DisplayOrder);
                if (displayOrderUpdateResult.IsFailure)
                {
                    return Result.Failure(displayOrderUpdateResult.Error);
                }
            }

            _menuCategoryRepository.Update(category);

            return Result.Success();
        }, cancellationToken);
    }
}
