using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.CustomizationGroupAggregate.Errors;

public static class CustomizationGroupErrors
{
    public static readonly Error GroupNameRequired = new(
        "CustomizationGroup.GroupNameRequired",
        "Group name is required.",
        ErrorType.Validation);

    public static readonly Error GroupNameNotUnique = new(
        "CustomizationGroup.GroupNameNotUnique",
        "Group name must be unique within the restaurant.",
        ErrorType.Conflict);

    public static readonly Error InvalidSelectionRange = new(
        "CustomizationGroup.InvalidSelectionRange",
        "MaxSelections must be greater than or equal to MinSelections.",
        ErrorType.Validation);

    public static readonly Error ChoiceNameNotUnique = new(
        "CustomizationGroup.ChoiceNameNotUnique",
        "Choice name must be unique within the group.",
        ErrorType.Conflict);

    public static readonly Error ChoiceNameRequired = new(
        "CustomizationGroup.ChoiceNameRequired",
        "Choice name is required.",
        ErrorType.Validation);

    public static readonly Error InvalidChoiceId = new(
        "CustomizationGroup.InvalidChoiceId",
        "Invalid choice ID.",
        ErrorType.Validation);

    public static readonly Error InvalidDisplayOrder = new(
        "CustomizationGroup.InvalidDisplayOrder",
        "Display order must be non-negative.",
        ErrorType.Validation);

    public static readonly Error DuplicateDisplayOrder = new(
        "CustomizationGroup.DuplicateDisplayOrder",
        "Display order must be unique within the group.",
        ErrorType.Conflict);

    public static readonly Error ChoiceNotFoundForReordering = new(
        "CustomizationGroup.ChoiceNotFoundForReordering",
        "One or more choice IDs are invalid for reordering.",
        ErrorType.Validation);

    public static readonly Error NotFound = new(
        "CustomizationGroup.NotFound",
        "The customization group was not found.",
        ErrorType.NotFound);
}
