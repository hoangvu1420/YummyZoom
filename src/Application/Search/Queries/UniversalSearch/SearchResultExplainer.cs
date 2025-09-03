using System.Globalization;

namespace YummyZoom.Application.Search.Queries.UniversalSearch;

public static class SearchResultExplainer
{
    public static (IReadOnlyList<SearchBadgeDto> badges, string reason) Explain(
        bool isOpenNow,
        bool isAcceptingOrders,
        double? avgRating,
        int reviewCount,
        double? distanceKm,
        ResultExplanationOptions options)
    {
        var badges = new List<SearchBadgeDto>(3);
        var parts = new List<string>(3);

        // Open now
        if (isOpenNow && isAcceptingOrders)
        {
            badges.Add(new SearchBadgeDto("open_now", "Open now"));
            parts.Add("Open now");
        }

        // Near you
        if (distanceKm.HasValue && options is not null)
        {
            if (distanceKm.Value <= options.NearYouDistanceKm)
            {
                var label = FormatDistance(distanceKm.Value) + " away";
                badges.Add(new SearchBadgeDto("near_you", label, new { distanceKm }));
                parts.Add(label);
            }
        }

        // Rating
        if (avgRating.HasValue && reviewCount >= (options?.MinReviewsForRatingBadge ?? 10))
        {
            var ratingStr = avgRating.Value.ToString("0.0", CultureInfo.InvariantCulture);
            var countStr = AbbrevCount(reviewCount);
            var label = $"⭐ {ratingStr} ({countStr})";
            badges.Add(new SearchBadgeDto("rating", label, new { rating = avgRating, reviewCount }));
            parts.Add(label);
        }

        var reason = parts.Count > 0 ? string.Join(" · ", parts) : "Relevant match";
        return (badges, reason);
    }

    private static string FormatDistance(double km)
        => km < 1 ? (km * 1000).ToString("0", CultureInfo.InvariantCulture) + " m"
                  : km.ToString("0.0", CultureInfo.InvariantCulture) + " km";

    private static string AbbrevCount(int count)
    {
        if (count >= 1_000_000)
        {
            return (count / 1_000_000.0).ToString("0.#", CultureInfo.InvariantCulture) + "M";
        }
        if (count >= 1_000)
        {
            return (count / 1_000.0).ToString("0.#", CultureInfo.InvariantCulture) + "k";
        }
        return count.ToString(CultureInfo.InvariantCulture);
    }
}

