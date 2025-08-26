using Microsoft.AspNetCore.Http;

namespace YummyZoom.Web.Infrastructure.Http;

public static class HttpCaching
{
    public static string BuildWeakEtag(Guid restaurantId, DateTimeOffset lastRebuiltAt)
    {
        // W/"r:{id}:t:{ticks}"
        return $"W\"r:{restaurantId}:t:{lastRebuiltAt.UtcTicks}\"";
    }

    public static string ToRfc1123(DateTimeOffset dt) => dt.UtcDateTime.ToString("R");

    public static bool MatchesIfNoneMatch(HttpRequest request, string etag)
    {
        var header = request.Headers.IfNoneMatch.ToString();
        if (string.IsNullOrEmpty(header)) return false;
        // Basic compare for exact match; can be expanded for multiple values
        return header.Contains(etag, StringComparison.Ordinal);
    }

    public static bool NotModifiedSince(HttpRequest request, DateTimeOffset lastModified)
    {
        var header = request.Headers.IfModifiedSince.ToString();
        if (string.IsNullOrEmpty(header)) return false;
        if (!DateTimeOffset.TryParse(header, out var since)) return false;
        return lastModified <= since;
    }
}
