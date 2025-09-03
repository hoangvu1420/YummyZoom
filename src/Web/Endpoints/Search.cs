using Microsoft.AspNetCore.Mvc;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Search.Queries.Autocomplete;
using YummyZoom.Application.Search.Queries.UniversalSearch;

namespace YummyZoom.Web.Endpoints;

public class Search : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var publicGroup = app.MapGroup(this);

        // GET /api/v1/search
        publicGroup.MapGet("/", async ([AsParameters] UniversalSearchRequestDto req, ISender sender) =>
        {
            // Apply defaults after binding to avoid Minimal API early 400s for missing value-type properties
            var pageNumber = req.PageNumber ?? 1;
            var pageSize = req.PageSize ?? 10;
            var includeFacets = req.IncludeFacets ?? false;

            var rq = new UniversalSearchQuery(
                req.Term,
                req.Lat,
                req.Lon,
                req.OpenNow,
                req.Cuisines,
                req.Tags,
                req.PriceBands,
                includeFacets,
                pageNumber,
                pageSize);
            
            var res = await sender.Send(rq);
            return res.IsSuccess ? Results.Ok(res.Value) : res.ToIResult();
        })
        .WithName("UniversalSearch")
        .WithSummary("Universal search")
        .WithDescription("Search across restaurants, menu items, and tags with optional location, open-now, cuisine, tag, and price filters. Returns paginated results and optional facets.")
        .WithStandardResults<UniversalSearchResponseDto>();

        // GET /api/v1/search/autocomplete
        publicGroup.MapGet("/autocomplete", async ([AsParameters] AutocompleteRequestDto req, ISender sender) =>
        {
            var term = req.Term ?? string.Empty; // avoid Minimal API 400 for missing non-nullable ref by coalescing here
            var res = await sender.Send(new AutocompleteQuery(term));
            return res.IsSuccess ? Results.Ok(res.Value) : res.ToIResult();
        })
        .WithName("Autocomplete")
        .WithSummary("Autocomplete suggestions")
        .WithDescription("Returns up to 10 suggestions for the given query term across searchable entities.")
        .WithStandardResults<IReadOnlyList<SuggestionDto>>();
    }
}

// Request DTOs (query-bound)
public sealed record UniversalSearchRequestDto
{
    public string? Term { get; init; }
    public double? Lat { get; init; }
    public double? Lon { get; init; }
    public bool? OpenNow { get; init; }
    public string[]? Cuisines { get; init; }
    public string[]? Tags { get; init; }
    public short[]? PriceBands { get; init; }
    public bool? IncludeFacets { get; init; }
    public int? PageNumber { get; init; }
    public int? PageSize { get; init; }
}

public sealed record AutocompleteRequestDto
{
    public string? Term { get; init; }
}
