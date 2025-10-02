using System.Text.Json.Serialization;

using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;

namespace YummyZoom.Domain.MenuItemAggregate.ValueObjects;

public sealed class AppliedCustomization : ValueObject
{
    public CustomizationGroupId CustomizationGroupId { get; private set; }
    public string DisplayTitle { get; private set; }
    public int DisplayOrder { get; private set; }

    [JsonConstructor]
    private AppliedCustomization(CustomizationGroupId customizationGroupId, string displayTitle, int displayOrder)
    {
        CustomizationGroupId = customizationGroupId;
        DisplayTitle = displayTitle;
        DisplayOrder = displayOrder;
    }

    public static AppliedCustomization Create(CustomizationGroupId customizationGroupId, string displayTitle, int displayOrder)
    {
        return new AppliedCustomization(customizationGroupId, displayTitle, displayOrder);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return CustomizationGroupId;
        yield return DisplayTitle;
        yield return DisplayOrder;
    }

#pragma warning disable CS8618
    // Internal parameterless constructor for EF Core and JSON deserialization
    internal AppliedCustomization() { }
#pragma warning restore CS8618
}
