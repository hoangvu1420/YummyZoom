using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.MenuEntity.Errors;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Application.MenuCategories.Commands.ReorderMenuCategories;

public sealed class ReorderMenuCategoriesCommandHandler
    : IRequestHandler<ReorderMenuCategoriesCommand, Result>
{
    private readonly IMenuCategoryRepository _menuCategoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReorderMenuCategoriesCommandHandler(
        IMenuCategoryRepository menuCategoryRepository,
        IUnitOfWork unitOfWork)
    {
        _menuCategoryRepository = menuCategoryRepository;
        _unitOfWork = unitOfWork;
    }

    public Task<Result> Handle(ReorderMenuCategoriesCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var restaurantId = RestaurantId.Create(request.RestaurantId);
            var categoriesForRestaurant = await _menuCategoryRepository.GetByRestaurantIdAsync(restaurantId, cancellationToken);

            var totalCategories = categoriesForRestaurant.Count;
            if (request.CategoryOrders.Count != totalCategories)
            {
                return Result.Failure(ReorderMenuCategoriesErrors.IncompleteCategoryList);
            }

            // Validate all provided categories belong to the restaurant and are not deleted
            var categoryLookup = categoriesForRestaurant.ToDictionary(c => c.Id, c => c);
            foreach (var order in request.CategoryOrders)
            {
                var catId = MenuCategoryId.Create(order.CategoryId);
                if (!categoryLookup.ContainsKey(catId))
                {
                    return Result.Failure(ReorderMenuCategoriesErrors.CategoryNotFound(order.CategoryId));
                }
            }

            // Validate display orders form a contiguous sequence 1..N
            var displayOrders = request.CategoryOrders.Select(o => o.DisplayOrder).ToList();
            var hasDistinct = displayOrders.Count == displayOrders.Distinct().Count();
            var min = displayOrders.Min();
            var max = displayOrders.Max();
            if (!hasDistinct || min != 1 || max != totalCategories)
            {
                return Result.Failure(ReorderMenuCategoriesErrors.InvalidDisplayOrderRange(totalCategories));
            }

            // Apply display order updates only when changed
            foreach (var order in request.CategoryOrders)
            {
                var catId = MenuCategoryId.Create(order.CategoryId);
                var category = categoryLookup[catId];

                if (category.DisplayOrder == order.DisplayOrder)
                {
                    continue;
                }

                var updateResult = category.UpdateDisplayOrder(order.DisplayOrder);
                if (updateResult.IsFailure)
                {
                    return Result.Failure(updateResult.Error);
                }

                _menuCategoryRepository.Update(category);
            }

            return Result.Success();
        }, cancellationToken);
    }
}