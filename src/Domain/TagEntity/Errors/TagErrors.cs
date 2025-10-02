using YummyZoom.Domain.TagEntity.Enums;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.TagEntity.Errors;

public static class TagErrors
{
    public static readonly Error NameIsRequired = new(
        "Tag.NameIsRequired",
        "Tag name is required.",
        ErrorType.Validation);

    public static readonly Error NameTooLong = new(
        "Tag.NameTooLong",
        "Tag name cannot exceed 100 characters.",
        ErrorType.Validation);



    public static readonly Error InvalidCategory = new(
        "Tag.InvalidCategory",
        $"Tag category must be one of: {string.Join(", ", TagCategoryExtensions.GetAllAsStrings())}.",
        ErrorType.Validation);

    public static readonly Error InvalidTagId = new(
        "Tag.InvalidTagId",
        "Invalid tag ID.",
        ErrorType.Validation);

    public static readonly Error DuplicateTagName = new(
        "Tag.DuplicateTagName",
        "A tag with this name already exists.",
        ErrorType.Conflict);
}
