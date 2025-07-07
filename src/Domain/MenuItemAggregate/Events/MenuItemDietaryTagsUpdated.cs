using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.TagEntity.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.MenuItemAggregate.Events;

public record MenuItemDietaryTagsUpdated(
    MenuItemId MenuItemId,
    List<TagId> TagIds
) : IDomainEvent;
