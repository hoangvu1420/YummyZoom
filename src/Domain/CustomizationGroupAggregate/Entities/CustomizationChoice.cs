using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.CustomizationGroupAggregate.Entities;

public sealed class CustomizationChoice : Entity<ChoiceId>
{
    public string Name { get; private set; }
    public Money PriceAdjustment { get; private set; }
    public bool IsDefault { get; private set; }

    private CustomizationChoice(
        ChoiceId id,
        string name,
        Money priceAdjustment,
        bool isDefault)
        : base(id)
    {
        Name = name;
        PriceAdjustment = priceAdjustment;
        IsDefault = isDefault;
    }

    public static Result<CustomizationChoice> Create(
        string name,
        Money priceAdjustment,
        bool isDefault = false)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<CustomizationChoice>(
                Error.Validation("CustomizationChoice.NameRequired", "Choice name is required."));
        }

        // PriceAdjustment is already validated by Money VO

        return Result.Success(new CustomizationChoice(
            ChoiceId.CreateUnique(),
            name.Trim(),
            priceAdjustment,
            isDefault));
    }

#pragma warning disable CS8618
    private CustomizationChoice() { }
#pragma warning restore CS8618
} 
