using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.ReviewAggregate;
using YummyZoom.Domain.ReviewAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore.Seeding.Options;
using YummyZoom.SharedKernel;
using OrderAggregate = YummyZoom.Domain.OrderAggregate.Order;

namespace YummyZoom.Infrastructure.Persistence.EfCore.Seeding.Seeders.ReviewSeeders;

/// <summary>
/// Seeder for creating realistic review data based on delivered orders.
/// Uses algorithmic generation with configurable templates and rating distributions.
/// </summary>
public class ReviewSeeder : ISeeder
{
    private readonly IReviewRepository _reviewRepository;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ReviewSeeder> _logger;

    public ReviewSeeder(
        IReviewRepository reviewRepository,
        ApplicationDbContext dbContext,
        ILogger<ReviewSeeder> logger)
    {
        _reviewRepository = reviewRepository;
        _dbContext = dbContext;
        _logger = logger;
    }

    public string Name => "Review";
    public int Order => 125; // After Orders (120)

    public Task<bool> CanSeedAsync(SeedingContext context, CancellationToken cancellationToken = default)
    {
        // Check if we have seeded orders to base reviews on
        var hasSeededOrders = context.SharedData.ContainsKey("SeededOrders");
        
        if (!hasSeededOrders)
        {
            _logger.LogWarning("Cannot seed reviews: no seeded orders found in SharedData");
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    public async Task<Result> SeedAsync(SeedingContext context, CancellationToken cancellationToken = default)
    {
        var options = context.Configuration.GetReviewSeedingOptions();

        try
        {
            // Load seeded orders from SharedData
            var deliveredOrders = GetDeliveredOrders(context.SharedData);
            if (deliveredOrders.Count == 0)
            {
                _logger.LogWarning("[Review] No delivered orders found - skipping review seeding");
                return Result.Success();
            }

            // Apply coverage percentage - not all orders get reviews
            var reviewableOrders = SelectOrdersForReview(deliveredOrders, options.ReviewCoveragePercentage);
            _logger.LogInformation("[Review] Selected {ReviewableCount} out of {TotalCount} delivered orders for review generation with {CoveragePercentage}% coverage", 
                reviewableOrders.Count, deliveredOrders.Count, options.ReviewCoveragePercentage);

            var seededReviews = new List<Review>();
            var totalReviewsCreated = 0;

            foreach (var order in reviewableOrders)
            {
                try
                {
                    var review = GenerateReview(order, options);
                    if (review.IsSuccess)
                    {
                        // Clear domain events for seeding
                        review.Value.ClearDomainEvents();
                        await _reviewRepository.AddAsync(review.Value, cancellationToken);
                        seededReviews.Add(review.Value);
                        totalReviewsCreated++;
                    }
                    else
                    {
                        _logger.LogWarning("[Review] Failed to generate review for order {OrderId}: {Error}", 
                            order.Id.Value, review.Error.Description);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Review] Error generating review for order {OrderId}", order.Id.Value);
                }
            }

            // Save all changes to the database
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Store seeded reviews for potential use by other seeders
            context.SharedData["SeededReviews"] = seededReviews;

            _logger.LogInformation("[Review] Successfully completed review seeding: {TotalReviews} reviews created",
                totalReviewsCreated);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Review] Failed to seed reviews");
            return Result.Failure(Error.Failure("ReviewSeeding.Failed", "Failed to seed reviews"));
        }
    }

    private List<OrderAggregate> GetDeliveredOrders(Dictionary<string, object> sharedData)
    {
        if (!sharedData.TryGetValue("SeededOrders", out var ordersObj) || ordersObj is not List<OrderAggregate> allOrders)
        {
            return new List<OrderAggregate>();
        }

        // Only delivered orders are eligible for reviews
        return allOrders.Where(o => o.Status == OrderStatus.Delivered && o.ActualDeliveryTime.HasValue).ToList();
    }

    private List<OrderAggregate> SelectOrdersForReview(List<OrderAggregate> deliveredOrders, decimal coveragePercentage)
    {
        if (coveragePercentage >= 100)
        {
            return deliveredOrders;
        }

        var countToSelect = (int)Math.Round(deliveredOrders.Count * (coveragePercentage / 100));
        return SeedingDataGenerator.SelectRandomItems(deliveredOrders, countToSelect, countToSelect);
    }

    private Result<Review> GenerateReview(OrderAggregate order, ReviewSeedingOptions options)
    {
        try
        {
            // Select rating based on distribution
            var rating = SeedingDataGenerator.SelectRatingByWeight(options.RatingDistribution);
            var ratingResult = Rating.Create(rating);
            if (ratingResult.IsFailure)
            {
                return Result.Failure<Review>(ratingResult.Error);
            }

            // Generate comment if needed
            string? comment = null;
            if (SeedingDataGenerator.ShouldGenerateComment(options.CommentPercentage))
            {
                comment = SeedingDataGenerator.SelectReviewComment(options.Templates, rating);
            }

            // Generate realistic review timestamp
            var reviewTimestamp = SeedingDataGenerator.GenerateReviewTimestamp(
                order.ActualDeliveryTime!.Value,
                options.MinDaysAfterDelivery,
                options.MaxDaysAfterDelivery);

            // Create review using domain factory method
            var reviewResult = Review.Create(
                order.Id,
                order.CustomerId,
                order.RestaurantId,
                ratingResult.Value,
                comment);

            if (reviewResult.IsFailure)
            {
                return Result.Failure<Review>(reviewResult.Error);
            }

            // Update the submission timestamp for realistic seeding
            // Note: This uses reflection to set the private field, which is acceptable for seeding
            var review = reviewResult.Value;
            var timestampField = typeof(Review).GetField("<SubmissionTimestamp>k__BackingField", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            timestampField?.SetValue(review, reviewTimestamp);

            // Add reply if needed
            if (SeedingDataGenerator.ShouldGenerateReply(options.ReplyPercentage))
            {
                var replyText = SeedingDataGenerator.SelectReplyTemplate(options.ReplyTemplates);
                var replyResult = review.AddReply(replyText);
                if (replyResult.IsFailure)
                {
                    _logger.LogWarning("[Review] Failed to add reply to review {ReviewId}: {Error}", 
                        review.Id.Value, replyResult.Error.Description);
                }
            }

            return Result.Success(review);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Review] Exception occurred while generating review for order {OrderId}", order.Id.Value);
            return Result.Failure<Review>(Error.Failure("ReviewGeneration.Exception", "Exception occurred during review generation"));
        }
    }
}
