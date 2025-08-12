namespace YummyZoom.Infrastructure.Data.Models;

public class RestaurantReviewSummary
{
    public Guid RestaurantId { get; set; }
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
}
