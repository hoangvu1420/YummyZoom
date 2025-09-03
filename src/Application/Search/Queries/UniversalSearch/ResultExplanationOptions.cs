namespace YummyZoom.Application.Search.Queries.UniversalSearch;

public sealed class ResultExplanationOptions
{
    public double NearYouDistanceKm { get; init; } = 2.0;
    public int MinReviewsForRatingBadge { get; init; } = 10;
    public double HighRatingCutoff { get; init; } = 4.5; // reserved for future phrasing
}

