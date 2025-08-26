using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Admin.Commands.RebuildFullMenu;

public sealed record RebuildFullMenuCommand(Guid RestaurantId) : IRequest<Result>;


