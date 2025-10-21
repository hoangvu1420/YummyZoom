# Review Seeding Module Design

## Overview
Algorithmic review generation based on seeded orders, using template-driven content generation with configurable patterns and distributions.

## Architecture

### Dependencies
- **Input**: Seeded orders from `context.SharedData["SeededOrders"]`
- **Order**: 125 (After Orders: 120)
- **Repository**: `IReviewRepository`

### Core Strategy
1. Filter delivered orders from seeded data
2. Apply coverage percentage (not all orders get reviews)
3. Generate reviews using weighted templates and rating distributions
4. Create reviews with realistic timestamps (1-14 days after delivery)

## Implementation Files

```
src/Infrastructure/Persistence/EfCore/Seeding/
├── Options/
│   └── ReviewSeedingOptions.cs         ⭐ ENHANCED
├── Seeders/
│   └── ReviewSeeders/
│       └── ReviewSeeder.cs             ⭐ NEW
└── SeedingDataGenerator.cs             ⭐ EXTENDED (add review methods)
```

## ReviewSeedingOptions.cs Structure

```csharp
public sealed class ReviewSeedingOptions
{
    // Coverage
    public decimal ReviewCoveragePercentage { get; set; } = 65;
    
    // Rating Distribution
    public Dictionary<string, int> RatingDistribution { get; set; } = new()
    {
        { "1", 3 }, { "2", 7 }, { "3", 20 }, { "4", 35 }, { "5", 35 }
    };
    
    // Content Generation
    public decimal CommentPercentage { get; set; } = 75;
    public ReviewTemplates Templates { get; set; } = new();
    
    // Timing
    public int MinDaysAfterDelivery { get; set; } = 1;
    public int MaxDaysAfterDelivery { get; set; } = 14;
}

public sealed class ReviewTemplates
{
    public Dictionary<string, ReviewTemplateSet> ByRating { get; set; } = new()
    {
        { "5", new ReviewTemplateSet { Weight = 35, Comments = [
            "Excellent food and fast delivery!",
            "Outstanding quality, highly recommend!",
            "Amazing flavors, will order again!"
        ]}},
        { "4", new ReviewTemplateSet { Weight = 35, Comments = [
            "Good food, reasonable price.",
            "Nice flavors, delivery on time.",
            "Solid choice, satisfied with order."
        ]}},
        { "3", new ReviewTemplateSet { Weight = 20, Comments = [
            "Food was okay, nothing special.",
            "Average quality, decent portion.",
            "Not bad, but room for improvement."
        ]}},
        { "2", new ReviewTemplateSet { Weight = 7, Comments = [
            "Food was cold when delivered.",
            "Disappointing quality for the price.",
            "Long delivery time, food lukewarm."
        ]}},
        { "1", new ReviewTemplateSet { Weight = 3, Comments = [
            "Very poor quality, cold food.",
            "Terrible experience, would not recommend.",
            "Food was inedible, waste of money."
        ]}}
    };
}

public sealed class ReviewTemplateSet
{
    public int Weight { get; set; } = 1;
    public List<string> Comments { get; set; } = new();
}
```

## ReviewSeeder.cs Implementation

```csharp
public class ReviewSeeder : ISeeder
{
    public string Name => "Review";
    public int Order => 125;
    
    public async Task<Result> SeedAsync(SeedingContext context, CancellationToken cancellationToken)
    {
        var options = context.Configuration.GetReviewSeedingOptions();
        
        // 1. Load seeded orders
        var orders = GetDeliveredOrders(context.SharedData);
        
        // 2. Apply coverage percentage
        var reviewableOrders = SelectOrdersForReview(orders, options.ReviewCoveragePercentage);
        
        // 3. Generate reviews
        foreach (var order in reviewableOrders)
        {
            var review = GenerateReview(order, options);
            await _reviewRepository.AddAsync(review, cancellationToken);
        }
        
        await context.DbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
    
    private Review GenerateReview(Order order, ReviewSeedingOptions options)
    {
        // Select rating based on distribution
        var rating = SeedingDataGenerator.SelectRatingByWeight(options.RatingDistribution);
        
        // Generate comment if needed
        string? comment = null;
        if (SeedingDataGenerator.ShouldGenerateComment(options.CommentPercentage))
        {
            comment = SeedingDataGenerator.SelectReviewComment(options.Templates, rating);
        }
        
        // Create review with realistic timestamp
        var reviewTimestamp = SeedingDataGenerator.GenerateReviewTimestamp(
            order.ActualDeliveryTime!.Value,
            options.MinDaysAfterDelivery,
            options.MaxDaysAfterDelivery);
            
        return Review.Create(
            order.Id,
            order.CustomerId,
            order.RestaurantId,
            Rating.Create(rating).Value,
            comment).Value;
    }
}
```

## SeedingDataGenerator Extensions

```csharp
public static class SeedingDataGenerator
{
    public static int SelectRatingByWeight(Dictionary<string, int> distribution)
    {
        var items = distribution.Select(kvp => (int.Parse(kvp.Key), kvp.Value)).ToList();
        return SelectByWeight(items);
    }
    
    public static bool ShouldGenerateComment(decimal commentPercentage)
    {
        return Random.Shared.NextDouble() < (double)(commentPercentage / 100);
    }
    
    public static string SelectReviewComment(ReviewTemplates templates, int rating)
    {
        var templateSet = templates.ByRating[rating.ToString()];
        return templateSet.Comments[Random.Shared.Next(templateSet.Comments.Count)];
    }
    
    public static DateTime GenerateReviewTimestamp(DateTime deliveryTime, int minDays, int maxDays)
    {
        var daysAfter = Random.Shared.Next(minDays, maxDays + 1);
        var hoursAfter = Random.Shared.Next(1, 24);
        return deliveryTime.AddDays(daysAfter).AddHours(hoursAfter);
    }
}
```

## Configuration Extension

```csharp
public static ReviewSeedingOptions GetReviewSeedingOptions(this SeedingConfiguration config)
{
    // Similar pattern to other seeding options
    // Handle JSON parsing, fallback to defaults
}
```

## Business Rules Compliance

- **Domain Validation**: Use `Review.Create()` and `Rating.Create()` factory methods
- **Order Relationship**: Only delivered orders eligible for reviews
- **Temporal Logic**: Reviews created after delivery timestamp
- **Uniqueness**: One review per order (handled by domain)
- **Data Integrity**: Customer and restaurant IDs match order

## Expected Output

- **Coverage**: ~65% of delivered orders receive reviews
- **Distribution**: Realistic rating spread (70% positive, 20% neutral, 10% negative)
- **Content**: Template-based comments with natural variation
- **Timing**: Reviews spread realistically over 1-14 days post-delivery

## Key Benefits

1. **Simple**: No complex bundle files or external dependencies
2. **Configurable**: All patterns defined in options class
3. **Realistic**: Weighted distributions and temporal logic
4. **Maintainable**: Template-based content generation
5. **Performance**: Direct algorithmic generation, no file I/O
6. **Consistent**: Follows established seeding patterns