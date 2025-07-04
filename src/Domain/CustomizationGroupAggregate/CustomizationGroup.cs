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
    public IReadOnlyList<CustomizationChoice> Choices => _choices.AsReadOnly();

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
            _choices = choices;
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
        _choices.Add(choice);
        var groupId = (CustomizationGroupId)Id;
        AddDomainEvent(new Events.CustomizationChoiceAdded(groupId, choice.Id, choice.Name));
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

    public Result UpdateChoice(ChoiceId choiceId, string newName, Money newPriceAdjustment, bool isDefault)
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
        // Re-create the choice entity to ensure immutability of value objects
        var updated = CustomizationChoice.Create(newName, newPriceAdjustment, isDefault);
        if (updated.IsFailure)
        {
            return Result.Failure(updated.Error);
        }
        _choices[_choices.IndexOf(choice)] = updated.Value;
        
        var groupId = (CustomizationGroupId)Id;
        AddDomainEvent(new Events.CustomizationChoiceUpdated(groupId, choiceId, newName, newPriceAdjustment, isDefault));
        
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

#pragma warning disable CS8618
    private CustomizationGroup() { }
#pragma warning restore CS8618
} 
