using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.MenuEntity;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.Menus.Commands.CreateMenu;

[Authorize(Policy = Policies.MustBeRestaurantOwner)]
public record CreateMenuCommand(
    Guid RestaurantId,
    string Name,
    string Description,
    bool IsEnabled = true
) : IRequest<Result<CreateMenuResponse>>, IRestaurantCommand
{
    RestaurantId IRestaurantCommand.RestaurantId => Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public record CreateMenuResponse(Guid MenuId);

public class CreateMenuCommandHandler : IRequestHandler<CreateMenuCommand, Result<CreateMenuResponse>>
{
    private readonly IMenuRepository _menuRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateMenuCommandHandler(
        IMenuRepository menuRepository,
        IUnitOfWork unitOfWork)
    {
        _menuRepository = menuRepository ?? throw new ArgumentNullException(nameof(menuRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public Task<Result<CreateMenuResponse>> Handle(CreateMenuCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // 1) Create restaurant ID value object
            var restaurantId = RestaurantId.Create(request.RestaurantId);

            // 2) Create the menu using domain factory
            var menuResult = Menu.Create(
                restaurantId,
                request.Name,
                request.Description,
                request.IsEnabled);

            if (menuResult.IsFailure)
            {
                return Result.Failure<CreateMenuResponse>(menuResult.Error);
            }

            // 3) Persist the menu
            await _menuRepository.AddAsync(menuResult.Value, cancellationToken);

            return Result.Success(new CreateMenuResponse(menuResult.Value.Id.Value));
        }, cancellationToken);
    }
}
