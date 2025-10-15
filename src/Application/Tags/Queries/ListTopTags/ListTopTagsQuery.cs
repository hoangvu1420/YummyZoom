using YummyZoom.Domain.TagEntity.Enums;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Tags.Queries.ListTopTags;

public sealed record ListTopTagsQuery(
    IReadOnlyList<TagCategory>? Categories,
    int Limit
) : IRequest<Result<IReadOnlyList<TopTagDto>>>;

public sealed record TopTagDto(
    Guid TagId,
    string TagName,
    TagCategory TagCategory,
    int UsageCount
);

public static class ListTopTagsErrors
{
    public static readonly Error InvalidLimit = Error.Validation(
        "Tags.Top.InvalidLimit",
        "Limit must be between 1 and 100.");
}

