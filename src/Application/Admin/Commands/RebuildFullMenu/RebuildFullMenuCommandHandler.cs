
using YummyZoom.Application.Restaurants.Queries.Common;
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
        var (menuJson, rebuiltAt) = await _rebuilder.RebuildAsync(request.RestaurantId, cancellationToken);
        await _rebuilder.UpsertAsync(request.RestaurantId, menuJson, rebuiltAt, cancellationToken);
        return Result.Success();
    }
}
