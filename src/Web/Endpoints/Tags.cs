using Microsoft.AspNetCore.Mvc;
using YummyZoom.Application.Tags.Queries.ListTopTags;
using YummyZoom.Domain.TagEntity.Enums;
using YummyZoom.Web.Infrastructure;

namespace YummyZoom.Web.Endpoints;

public sealed class Tags : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(this);

        // GET /api/v1/tags/top?categories=Dietary,Cuisine&limit=10
        group.MapGet("/top", async (
            [FromQuery] string? categories,
            [FromQuery] int? limit,
            ISender sender) =>
        {
            List<TagCategory>? parsedCategories = null;
            
            if (!string.IsNullOrWhiteSpace(categories))
            {
                var categoryNames = categories.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                parsedCategories = new List<TagCategory>();
                
                foreach (var categoryName in categoryNames)
                {
                    if (!TagCategoryExtensions.TryParse(categoryName, out var parsed))
                    {
                        return Results.BadRequest(new ProblemDetails
                        {
                            Title = "Tags.Top.InvalidCategory",
                            Detail = $"Unknown category '{categoryName}'. Valid values: {string.Join(", ", TagCategoryExtensions.GetAllAsStrings())}.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    parsedCategories.Add(parsed);
                }
            }

            var q = new ListTopTagsQuery(parsedCategories?.AsReadOnly(), limit ?? 10);
            var result = await sender.Send(q);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("ListTopTags")
        .WithSummary("List top tags by usage")
        .WithDescription("Returns the most-used tags across available menu items in verified restaurants. " +
                        "Supports filtering by single category (?categories=Dietary) or multiple categories (?categories=Dietary,Cuisine).")
        .Produces<IReadOnlyList<TopTagDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}

