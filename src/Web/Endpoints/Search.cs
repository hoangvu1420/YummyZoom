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
        publicGroup.MapGet("/", async (string? term, double? lat, double? lon, bool? openNow, string[]? cuisines, int pageNumber, int pageSize, ISender sender) =>
        {
            var rq = new UniversalSearchQuery(term, lat, lon, openNow, cuisines, pageNumber, pageSize);
            var res = await sender.Send(rq);
            return res.IsSuccess ? Results.Ok(res.Value) : res.ToIResult();
        })
        .WithName("UniversalSearch")
        .WithSummary("Universal search")
        .Produces<PaginatedList<SearchResultDto>>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // GET /api/v1/search/autocomplete
        publicGroup.MapGet("/autocomplete", async (string q, ISender sender) =>
        {
            var res = await sender.Send(new AutocompleteQuery(q));
            return res.IsSuccess ? Results.Ok(res.Value) : res.ToIResult();
        })
        .WithName("Autocomplete")
        .WithSummary("Autocomplete suggestions")
        .Produces<IReadOnlyList<SuggestionDto>>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}

