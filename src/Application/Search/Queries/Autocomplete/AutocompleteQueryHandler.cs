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

        const string sql = """
            SELECT s."Id" AS Id, s."Type" AS Type, s."Name" AS Name
            FROM "SearchIndexItems" s
            WHERE s."SoftDeleted" = FALSE
              AND (
                    s."Name" ILIKE @prefix
                 OR similarity(s."Name", @q) > 0.2
              )
              AND (@types IS NULL OR s."Type" = ANY(@types))
            ORDER BY GREATEST(
                      similarity(s."Name", @q),
                      CASE WHEN s."Name" ILIKE @prefix THEN 1 ELSE 0 END
                   ) DESC,
                   s."UpdatedAt" DESC
            LIMIT @limit;
            """;

        var types = request.Types is { Length: > 0 }
            ? request.Types.Select(t => t.Trim().ToLowerInvariant()).ToArray()
            : null;

        var list = await conn.QueryAsync<SuggestionDto>(
            new CommandDefinition(sql, new { q = request.Term, prefix = request.Term + "%", limit = request.Limit, types }, cancellationToken: ct));

        return Result.Success((IReadOnlyList<SuggestionDto>)list.ToList());
    }
}
