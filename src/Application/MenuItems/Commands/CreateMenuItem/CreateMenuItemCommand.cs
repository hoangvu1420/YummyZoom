using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.MenuItemAggregate.Errors;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.TagEntity.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.MenuItems.Commands.CreateMenuItem;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public record CreateMenuItemCommand(
    Guid RestaurantId,
    Guid MenuCategoryId,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    string? ImageUrl = null,
    bool IsAvailable = true,
    List<Guid>? DietaryTagIds = null
) : IRequest<Result<CreateMenuItemResponse>>, IRestaurantCommand
{
    RestaurantId IRestaurantCommand.RestaurantId => Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public record CreateMenuItemResponse(Guid MenuItemId);

public class CreateMenuItemCommandHandler : IRequestHandler<CreateMenuItemCommand, Result<CreateMenuItemResponse>>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IMenuCategoryRepository _menuCategoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateMenuItemCommandHandler(
        IMenuItemRepository menuItemRepository,
        IMenuCategoryRepository menuCategoryRepository,
        IUnitOfWork unitOfWork)
    {
        _menuItemRepository = menuItemRepository ?? throw new ArgumentNullException(nameof(menuItemRepository));
        _menuCategoryRepository = menuCategoryRepository ?? throw new ArgumentNullException(nameof(menuCategoryRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public Task<Result<CreateMenuItemResponse>> Handle(CreateMenuItemCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // 1) Validate that the menu category exists and belongs to the restaurant
            var categoryId = MenuCategoryId.Create(request.MenuCategoryId);
            var category = await _menuCategoryRepository.GetByIdAsync(categoryId, cancellationToken);
            
            if (category is null)
            {
                return Result.Failure<CreateMenuItemResponse>(
                    MenuItemErrors.CategoryNotFound(request.MenuCategoryId));
            }

            // 2) Validate restaurant ownership through category's menu
            var restaurantId = RestaurantId.Create(request.RestaurantId);
            var categoriesForRestaurant = await _menuCategoryRepository.GetByRestaurantIdAsync(restaurantId, cancellationToken);
            
            if (!categoriesForRestaurant.Any(c => c.Id == categoryId))
            {
                return Result.Failure<CreateMenuItemResponse>(
                    MenuItemErrors.CategoryNotBelongsToRestaurant(request.MenuCategoryId, request.RestaurantId));
            }

            // 3) Convert dietary tag IDs to domain value objects
            List<TagId>? dietaryTagIds = null;
            if (request.DietaryTagIds?.Count > 0)
            {
                dietaryTagIds = request.DietaryTagIds.Select(TagId.Create).ToList();
            }

            // 4) Create money value object
            var basePrice = new Money(request.Price, request.Currency);

            // 5) Create the menu item using domain factory
            var menuItemResult = MenuItem.Create(
                restaurantId,
                categoryId,
                request.Name,
                request.Description,
                basePrice,
                request.ImageUrl,
                request.IsAvailable,
                dietaryTagIds);

            if (menuItemResult.IsFailure)
            {
                return Result.Failure<CreateMenuItemResponse>(menuItemResult.Error);
            }

            // 6) Persist the menu item
            await _menuItemRepository.AddAsync(menuItemResult.Value, cancellationToken);

            return Result.Success(new CreateMenuItemResponse(menuItemResult.Value.Id.Value));
        }, cancellationToken);
    }
}
