using Dapper;
using FluentValidation;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Search.Queries.Autocomplete;

// Request + DTO
public sealed record AutocompleteQuery(string Term, int Limit = 10, string[]? Types = null) : IRequest<Result<IReadOnlyList<SuggestionDto>>>;

public sealed record SuggestionDto(Guid Id, string Type, string Name);

// Validator
public sealed class AutocompleteQueryValidator : AbstractValidator<AutocompleteQuery>
{
    public AutocompleteQueryValidator()
    {
        RuleFor(x => x.Term).NotEmpty().MinimumLength(1).MaximumLength(64);
        RuleFor(x => x.Limit).InclusiveBetween(1, 50);
        RuleForEach(x => x.Types!).Must(t => !string.IsNullOrWhiteSpace(t)).When(x => x.Types is { Length: > 0 });
    }
}

// Handler
public sealed class AutocompleteQueryHandler
    : IRequestHandler<AutocompleteQuery, Result<IReadOnlyList<SuggestionDto>>>
{
    private readonly IDbConnectionFactory _db;
    public AutocompleteQueryHandler(IDbConnectionFactory db) => _db = db;

    public async Task<Result<IReadOnlyList<SuggestionDto>>> Handle(AutocompleteQuery request, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();

        var term = request.Term.Trim();
        var includeFts = term.Length <= 2;

        const string sql = """
WITH ranked AS (
    SELECT
        s."Id" AS Id,
        s."Type" AS Type,
        s."Name" AS Name,
        s."UpdatedAt" AS UpdatedAt,
        (s."Name" ILIKE @prefix) AS name_prefix,
        similarity(s."Name", @q) AS name_similarity,
        CASE
            WHEN @includeFts THEN (s."TsAll" @@ plainto_tsquery('simple', unaccent(@q)))
            ELSE FALSE
        END AS fts_match
    FROM "SearchIndexItems" s
    WHERE s."SoftDeleted" = FALSE
      AND (@filterByTypes = FALSE OR s."Type" = ANY(@types))
)
SELECT Id, Type, Name
FROM ranked
WHERE name_prefix
   OR name_similarity > 0.2
   OR fts_match
ORDER BY GREATEST(
          name_similarity,
          CASE WHEN name_prefix THEN 1 ELSE 0 END,
          CASE WHEN fts_match THEN 0.8 ELSE 0 END
       ) DESC,
       UpdatedAt DESC
LIMIT @limit;
""";

        var types = request.Types is { Length: > 0 }
            ? request.Types.Select(t => t.Trim().ToLowerInvariant()).ToArray()
            : Array.Empty<string>();
        var filterByTypes = types.Length > 0;

        var list = await conn.QueryAsync<SuggestionDto>(
            new CommandDefinition(
                sql,
                new
                {
                    q = term,
                    prefix = term + "%",
                    limit = request.Limit,
                    types,
                    filterByTypes,
                    includeFts
                },
                cancellationToken: ct));

        return Result.Success((IReadOnlyList<SuggestionDto>)list.ToList());
    }
}
