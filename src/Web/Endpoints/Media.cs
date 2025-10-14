using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using YummyZoom.Application.Common.Configuration;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Web.Infrastructure;

namespace YummyZoom.Web.Endpoints;

public sealed class Media : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(this);

        // GET /api/v1/media/proxy?url=...
        group.MapGet("/proxy", async (
            [FromQuery] string url,
            IImageProxyService proxy,
            IOptions<ImageProxyOptions> options,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "ImageProxy.InvalidUrl",
                    Detail = "Query parameter 'url' is required.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "ImageProxy.InvalidUrl",
                    Detail = "The provided URL is invalid.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var result = await proxy.GetAsync(uri, ct);
            if (result.IsFailure)
            {
                return result.ToIResult();
            }

            var img = result.Value;

            // Set permissive CORS for the proxied asset
            httpContext.Response.Headers["Access-Control-Allow-Origin"] = "*";
            httpContext.Response.Headers["Vary"] = "Origin";
            httpContext.Response.Headers["Cache-Control"] = $"public, max-age={Math.Max(0, options.Value.CacheSeconds)}";
            if (!string.IsNullOrEmpty(img.ETag))
            {
                httpContext.Response.Headers["ETag"] = img.ETag;
            }
            if (img.LastModified.HasValue)
            {
                httpContext.Response.Headers["Last-Modified"] = img.LastModified.Value.ToString("R");
            }

            return Results.Stream(img.Content, contentType: img.ContentType, fileDownloadName: null, lastModified: img.LastModified, enableRangeProcessing: false);
        })
        .WithName("ProxyImage")
        .WithSummary("Proxy remote images with permissive CORS")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status415UnsupportedMediaType)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireRateLimiting("image-proxy-ip");
    }
}

