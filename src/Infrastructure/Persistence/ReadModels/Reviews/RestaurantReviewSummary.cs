namespace YummyZoom.Infrastructure.Persistence.ReadModels.Reviews;

public class RestaurantReviewSummary
{
    public Guid RestaurantId { get; set; }
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }

    // Distribution counts
    public int Ratings1 { get; set; }
    public int Ratings2 { get; set; }
    public int Ratings3 { get; set; }
    public int Ratings4 { get; set; }
    public int Ratings5 { get; set; }

    // Additional aggregates
    public int TotalWithText { get; set; }
    public DateTime? LastReviewAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
