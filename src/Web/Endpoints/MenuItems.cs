using Microsoft.AspNetCore.Mvc;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.MenuItems.Queries.Feed;

namespace YummyZoom.Web.Endpoints;

public sealed class MenuItems : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var publicGroup = app.MapGroup(this);

        // GET /api/v1/menu-items/feed
        publicGroup.MapGet("/feed", async ([AsParameters] MenuItemsFeedRequestDto req, ISender sender) =>
        {
            var pageNumber = req.PageNumber ?? 1;
            var pageSize = req.PageSize ?? 20;
            var tab = req.Tab ?? "popular";

            var res = await sender.Send(new GetMenuItemsFeedQuery(tab, pageNumber, pageSize));
            return res.IsSuccess ? Results.Ok(res.Value) : res.ToIResult();
        })
        .WithName("MenuItemsFeed")
        .WithSummary("Menu items feed")
        .WithDescription(
            "Curated menu items feed for the home page.\n\n"
          + "Query parameters:\n"
          + "- tab: popular (default).\n"
          + "- pageNumber: default 1.\n"
          + "- pageSize: default 20; range 1..50.")
        .WithStandardResults<PaginatedList<MenuItemFeedDto>>();
    }
}

public sealed record MenuItemsFeedRequestDto
{
    public string? Tab { get; init; }
    public int? PageNumber { get; init; }
    public int? PageSize { get; init; }
}

