using Dapper;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Domain.TagEntity.Enums;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Tags.Queries.GetDietaryTags;

public sealed class GetDietaryTagsQueryHandler : IRequestHandler<GetDietaryTagsQuery, Result<IReadOnlyList<DietaryTagDto>>>
{
    private readonly IDbConnectionFactory _db;

    public GetDietaryTagsQueryHandler(IDbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<Result<IReadOnlyList<DietaryTagDto>>> Handle(GetDietaryTagsQuery request, CancellationToken cancellationToken)
    {
        using var connection = _db.CreateConnection();

        const string sql = """
            SELECT
                "Id" AS TagId,
                "TagName" AS Name
            FROM "Tags"
            WHERE "IsDeleted" = FALSE
            ORDER BY "TagName";
        """;

        var tags = await connection.QueryAsync<DietaryTagDto>(sql);

        return Result.Success<IReadOnlyList<DietaryTagDto>>(tags.AsList());
    }
}
