
using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;

namespace YummyZoom.Domain.MenuAggregate.ValueObjects;

public sealed class AppliedCustomization : ValueObject
{
    public CustomizationGroupId CustomizationGroupId { get; private set; }
    public string DisplayTitle { get; private set; }
    public int DisplayOrder { get; private set; }

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
    private AppliedCustomization() { }
#pragma warning restore CS8618
}
