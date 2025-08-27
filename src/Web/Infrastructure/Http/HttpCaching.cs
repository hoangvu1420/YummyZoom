using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace YummyZoom.Web.Infrastructure.Http;

public static class HttpCaching
{
    public static EntityTagHeaderValue BuildWeakEtag(Guid restaurantId, DateTimeOffset lastRebuiltAt)
    {
        // Create weak ETag with format: W/"r:{id}:t:{ticks}"
        // EntityTagHeaderValue constructor requires the value to be quoted
        var value = $"\"r:{restaurantId}:t:{lastRebuiltAt.UtcTicks}\"";
        return new EntityTagHeaderValue(value, isWeak: true);
    }

    public static string ToRfc1123(DateTimeOffset dt) => dt.UtcDateTime.ToString("R");

    public static bool MatchesIfNoneMatch(HttpRequest request, EntityTagHeaderValue etag)
    {
        var ifNoneMatchHeader = request.GetTypedHeaders().IfNoneMatch;
        if (ifNoneMatchHeader == null || !ifNoneMatchHeader.Any()) return false;

        // Check if any of the ETags in If-None-Match header match our ETag
        // For weak ETags, we compare the values directly
        return ifNoneMatchHeader.Any(headerEtag =>
            string.Equals(etag.Tag.ToString(), headerEtag.Tag.ToString(), StringComparison.Ordinal));
    }

    public static bool NotModifiedSince(HttpRequest request, DateTimeOffset lastModified)
    {
        var ifModifiedSinceHeader = request.GetTypedHeaders().IfModifiedSince;
        if (ifModifiedSinceHeader == null) return false;

        // Return true if the resource hasn't been modified since the specified date
        return lastModified <= ifModifiedSinceHeader.Value;
    }
}
