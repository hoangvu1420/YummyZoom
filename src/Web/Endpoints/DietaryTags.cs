using YummyZoom.Application.Tags.Queries.GetDietaryTags;
using YummyZoom.Web.Infrastructure;

namespace YummyZoom.Web.Endpoints;

public sealed class DietaryTags : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(this);

        // GET /api/v1/dietary-tags
        group.MapGet("", async (ISender sender) =>
        {
            var result = await sender.Send(new GetDietaryTagsQuery());
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("GetDietaryTags")
        .WithSummary("Get system dietary tags")
        .WithDescription("Returns the list of available dietary tags (e.g., Vegan, Spicy, Gluten-Free).")
        .Produces<IReadOnlyList<DietaryTagDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
