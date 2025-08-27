using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.Domain.MenuEntity.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Admin.Commands.RebuildFullMenu;

public sealed class RebuildFullMenuCommandHandler : IRequestHandler<RebuildFullMenuCommand, Result>
{
    private readonly IMenuReadModelRebuilder _rebuilder;

    public RebuildFullMenuCommandHandler(IMenuReadModelRebuilder rebuilder)
    {
        _rebuilder = rebuilder;
    }

    public async Task<Result> Handle(RebuildFullMenuCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var (menuJson, rebuiltAt) = await _rebuilder.RebuildAsync(request.RestaurantId, cancellationToken);
            await _rebuilder.UpsertAsync(request.RestaurantId, menuJson, rebuiltAt, cancellationToken);
            return Result.Success();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Enabled menu not found"))
        {
            return Result.Failure(MenuErrors.NoEnabledMenuFound(request.RestaurantId));
        }
    }
}
