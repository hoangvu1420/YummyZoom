namespace YummyZoom.Infrastructure.Data.ReadModels.Reviews;

public class RestaurantReviewSummary
{
    public Guid RestaurantId { get; set; }
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
}
