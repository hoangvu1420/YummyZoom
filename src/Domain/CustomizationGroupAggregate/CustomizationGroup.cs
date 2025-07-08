using YummyZoom.Domain.CustomizationGroupAggregate.Entities;
using YummyZoom.Domain.CustomizationGroupAggregate.Errors;
using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.Domain.Common.ValueObjects;

namespace YummyZoom.Domain.CustomizationGroupAggregate;

public sealed class CustomizationGroup : AggregateRoot<CustomizationGroupId, Guid>
{
    private readonly List<CustomizationChoice> _choices = new();

    public RestaurantId RestaurantId { get; private set; }
    public string GroupName { get; private set; }
    public int MinSelections { get; private set; }
    public int MaxSelections { get; private set; }
    public IReadOnlyList<CustomizationChoice> Choices => _choices
        .OrderBy(c => c.DisplayOrder)
        .ThenBy(c => c.Name)
        .ToList()
        .AsReadOnly();

    private CustomizationGroup(
        CustomizationGroupId id,
        RestaurantId restaurantId,
        string groupName,
        int minSelections,
        int maxSelections,
        List<CustomizationChoice>? choices = null)
        : base(id)
    {
        RestaurantId = restaurantId;
        GroupName = groupName;
        MinSelections = minSelections;
        MaxSelections = maxSelections;
        if (choices is not null)
            _choices = new List<CustomizationChoice>(choices);
    }

    public static Result<CustomizationGroup> Create(
        RestaurantId restaurantId,
        string groupName,
        int minSelections,
        int maxSelections,
        List<CustomizationChoice>? choices = null)
    {
        if (string.IsNullOrWhiteSpace(groupName))
        {
            return Result.Failure<CustomizationGroup>(CustomizationGroupErrors.GroupNameRequired);
        }
        if (maxSelections < minSelections)
        {
            return Result.Failure<CustomizationGroup>(CustomizationGroupErrors.InvalidSelectionRange);
        }
        // GroupName uniqueness should be enforced at the application/repository level

        var group = new CustomizationGroup(
            CustomizationGroupId.CreateUnique(),
            restaurantId,
            groupName.Trim(),
            minSelections,
            maxSelections,
            choices);

        var groupId = (CustomizationGroupId)group.Id;
        group.AddDomainEvent(new Events.CustomizationGroupCreated(groupId, group.RestaurantId, group.GroupName));
        return Result.Success(group);
    }

    public Result AddChoice(CustomizationChoice choice)
    {
        if (_choices.Any(c => c.Name == choice.Name))
        {
            return Result.Failure(CustomizationGroupErrors.ChoiceNameNotUnique);
        }

        // Note: We allow duplicate display orders - items with same order will be sorted by name
        _choices.Add(choice);
        var groupId = (CustomizationGroupId)Id;
        AddDomainEvent(new Events.CustomizationChoiceAdded(groupId, choice.Id, choice.Name));
        return Result.Success();
    }

    public Result AddChoiceWithAutoOrder(string name, Money priceAdjustment, bool isDefault = false)
    {
        if (_choices.Any(c => c.Name == name))
        {
            return Result.Failure(CustomizationGroupErrors.ChoiceNameNotUnique);
        }

        // Automatically assign the next available display order
        var nextDisplayOrder = _choices.Any() ? _choices.Max(c => c.DisplayOrder) + 1 : 1;

        var choiceResult = CustomizationChoice.Create(name, priceAdjustment, isDefault, nextDisplayOrder);
        if (choiceResult.IsFailure)
        {
            return Result.Failure(choiceResult.Error);
        }

        _choices.Add(choiceResult.Value);
        var groupId = (CustomizationGroupId)Id;
        AddDomainEvent(new Events.CustomizationChoiceAdded(groupId, choiceResult.Value.Id, name));
        return Result.Success();
    }

    public Result RemoveChoice(ChoiceId choiceId)
    {
        var choice = _choices.FirstOrDefault(c => c.Id == choiceId);
        if (choice is null)
        {
            return Result.Failure(CustomizationGroupErrors.InvalidChoiceId);
        }
        _choices.Remove(choice);
        
        var groupId = (CustomizationGroupId)Id;
        AddDomainEvent(new Events.CustomizationChoiceRemoved(groupId, choice.Id, choice.Name));
        
        return Result.Success();
    }

    public Result UpdateChoice(ChoiceId choiceId, string newName, Money newPriceAdjustment, bool isDefault, int? displayOrder = null)
    {
        var choice = _choices.FirstOrDefault(c => c.Id == choiceId);
        if (choice is null)
        {
            return Result.Failure(CustomizationGroupErrors.InvalidChoiceId);
        }
        if (_choices.Any(c => c.Id != choiceId && c.Name == newName))
        {
            return Result.Failure(CustomizationGroupErrors.ChoiceNameNotUnique);
        }

        var newDisplayOrder = displayOrder ?? choice.DisplayOrder;
        
        // Note: We allow duplicate display orders - items with same order will be sorted by name
        
        // Re-create the choice entity to ensure immutability of value objects
        var updated = CustomizationChoice.Create(choiceId, newName, newPriceAdjustment, isDefault, newDisplayOrder);
        if (updated.IsFailure)
        {
            return Result.Failure(updated.Error);
        }
        _choices[_choices.IndexOf(choice)] = updated.Value;
        
        var groupId = (CustomizationGroupId)Id;
        AddDomainEvent(new Events.CustomizationChoiceUpdated(groupId, choiceId, newName, newPriceAdjustment, isDefault, newDisplayOrder));
        
        return Result.Success();
    }

    public Result UpdateGroupDetails(string groupName, int minSelections, int maxSelections)
    {
        if (string.IsNullOrWhiteSpace(groupName))
        {
            return Result.Failure(CustomizationGroupErrors.GroupNameRequired);
        }
        if (maxSelections < minSelections)
        {
            return Result.Failure(CustomizationGroupErrors.InvalidSelectionRange);
        }
        GroupName = groupName.Trim();
        MinSelections = minSelections;
        MaxSelections = maxSelections;
        return Result.Success();
    }

    public Result ReorderChoices(List<(ChoiceId choiceId, int newDisplayOrder)> orderChanges)
    {
        if (orderChanges == null || !orderChanges.Any())
        {
            return Result.Success(); // Nothing to do
        }

        // Validate all choice IDs exist
        var invalidChoiceIds = orderChanges
            .Where(change => !_choices.Any(c => c.Id == change.choiceId))
            .Select(change => change.choiceId)
            .ToList();

        if (invalidChoiceIds.Any())
        {
            return Result.Failure(CustomizationGroupErrors.ChoiceNotFoundForReordering);
        }

        // Validate display orders are non-negative
        var invalidOrders = orderChanges.Where(change => change.newDisplayOrder < 0).ToList();
        if (invalidOrders.Any())
        {
            return Result.Failure(CustomizationGroupErrors.InvalidDisplayOrder);
        }

        // Check for duplicate display orders within the reorder operation
        var displayOrders = orderChanges.Select(change => change.newDisplayOrder).ToList();
        if (displayOrders.Count != displayOrders.Distinct().Count())
        {
            return Result.Failure(CustomizationGroupErrors.DuplicateDisplayOrder);
        }

        // Apply the reordering
        var reorderedChoices = new Dictionary<ChoiceId, int>();
        foreach (var (choiceId, newDisplayOrder) in orderChanges)
        {
            var choice = _choices.First(c => c.Id == choiceId);
            var updateResult = choice.UpdateDisplayOrder(newDisplayOrder);
            if (updateResult.IsFailure)
            {
                return Result.Failure(updateResult.Error);
            }
            reorderedChoices.Add(choiceId, newDisplayOrder);
        }

        var groupId = (CustomizationGroupId)Id;
        AddDomainEvent(new Events.CustomizationChoicesReordered(groupId, reorderedChoices));

        return Result.Success();
    }

    /// <summary>
    /// Marks this customization group as deleted. This is the single, authoritative way to delete this aggregate.
    /// </summary>
    /// <returns>A Result indicating success</returns>
    public Result MarkAsDeleted()
    {
        AddDomainEvent(new Events.CustomizationGroupDeleted((CustomizationGroupId)Id));

        return Result.Success();
    }

#pragma warning disable CS8618
    private CustomizationGroup() { }
#pragma warning restore CS8618
}
