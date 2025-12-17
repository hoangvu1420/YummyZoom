using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Tags.Queries.GetDietaryTags;

public sealed record GetDietaryTagsQuery() : IRequest<Result<IReadOnlyList<DietaryTagDto>>>;
