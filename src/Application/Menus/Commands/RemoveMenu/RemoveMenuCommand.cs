using FluentValidation;
using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.MenuEntity.Events;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.Menus.Commands.RemoveMenu;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record RemoveMenuCommand(Guid RestaurantId, Guid MenuId) : IRequest<Result>, IRestaurantCommand
{
    Domain.RestaurantAggregate.ValueObjects.RestaurantId IRestaurantCommand.RestaurantId =>
        Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public sealed class RemoveMenuCommandValidator : AbstractValidator<RemoveMenuCommand>
{
    public RemoveMenuCommandValidator()
    {
        RuleFor(x => x.RestaurantId).NotEmpty();
        RuleFor(x => x.MenuId).NotEmpty();
    }
}
