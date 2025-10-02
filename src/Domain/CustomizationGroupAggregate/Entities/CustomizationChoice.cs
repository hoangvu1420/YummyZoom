using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CustomizationGroupAggregate.Errors;
using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.CustomizationGroupAggregate.Entities;

public sealed class CustomizationChoice : Entity<ChoiceId>
{
    public string Name { get; private set; }
    public Money PriceAdjustment { get; private set; }
    public bool IsDefault { get; private set; }
    public int DisplayOrder { get; private set; }

    private CustomizationChoice(
        ChoiceId id,
        string name,
        Money priceAdjustment,
        bool isDefault,
        int displayOrder)
        : base(id)
    {
        Name = name;
        PriceAdjustment = priceAdjustment;
        IsDefault = isDefault;
        DisplayOrder = displayOrder;
    }

    public static Result<CustomizationChoice> Create(
        string name,
        Money priceAdjustment,
        bool isDefault = false,
        int displayOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<CustomizationChoice>(CustomizationGroupErrors.ChoiceNameRequired);
        }

        if (displayOrder < 0)
        {
            return Result.Failure<CustomizationChoice>(CustomizationGroupErrors.InvalidDisplayOrder);
        }

        // PriceAdjustment is already validated by Money VO

        return Result.Success(new CustomizationChoice(
            ChoiceId.CreateUnique(),
            name.Trim(),
            priceAdjustment,
            isDefault,
            displayOrder));
    }

    public static Result<CustomizationChoice> Create(
        ChoiceId id,
        string name,
        Money priceAdjustment,
        bool isDefault,
        int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<CustomizationChoice>(CustomizationGroupErrors.ChoiceNameRequired);
        }

        if (displayOrder < 0)
        {
            return Result.Failure<CustomizationChoice>(CustomizationGroupErrors.InvalidDisplayOrder);
        }

        return Result.Success(new CustomizationChoice(
            id,
            name.Trim(),
            priceAdjustment,
            isDefault,
            displayOrder));
    }

    public Result UpdateDisplayOrder(int displayOrder)
    {
        if (displayOrder < 0)
        {
            return Result.Failure(CustomizationGroupErrors.InvalidDisplayOrder);
        }
        DisplayOrder = displayOrder;
        return Result.Success();
    }

#pragma warning disable CS8618
    private CustomizationChoice() { }
#pragma warning restore CS8618
}
