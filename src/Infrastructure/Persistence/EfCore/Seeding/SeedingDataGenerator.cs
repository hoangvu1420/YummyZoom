using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore.Seeding.Options;

namespace YummyZoom.Infrastructure.Persistence.EfCore.Seeding;

/// <summary>
/// Utility methods for generating realistic seeding data.
/// </summary>
public static class SeedingDataGenerator
{
    private static readonly Random Random = new();

    #region Address Generation

    private static readonly string[] Streets = new[]
    {
        "24 Lê Văn Hưu, Phan Chu Trinh",
        "49 Bát Đàn, Phường Cửa Đông",
        "14 Chả Cá, Hàng Buồm",
        "67 Đường Thành, Hàng Bồ",
        "89 Nguyễn Thái Học",
        "123 Hoàng Diệu",
        "45 Lý Thường Kiệt",
        "78 Trần Hưng Đạo",
        "32 Đinh Tiên Hoàng",
        "91 Hàng Gai, Hoàn Kiếm"
    };

    private static readonly string[] Districts = new[]
    {
        "Hoàn Kiếm", "Ba Đình", "Đống Đa", "Hai Bà Trưng",
        "Thanh Xuân", "Cầu Giấy", "Tây Hồ", "Long Biên"
    };

    private static readonly Dictionary<string, string[]> ZipCodesByDistrict = new()
    {
        { "Hoàn Kiếm", new[] { "11016", "11017", "11018" } },
        { "Ba Đình", new[] { "10000", "10001", "10002" } },
        { "Đống Đa", new[] { "11310", "11311", "11312" } },
        { "Hai Bà Trưng", new[] { "11616", "11617", "11618" } },
        { "Thanh Xuân", new[] { "12116", "12117", "12118" } },
        { "Cầu Giấy", new[] { "11313", "11314", "11315" } },
        { "Tây Hồ", new[] { "11902", "11903", "11904" } },
        { "Long Biên", new[] { "12012", "12013", "12014" } }
    };

    public static DeliveryAddress GenerateRandomDeliveryAddress()
    {
        var district = Districts[Random.Next(Districts.Length)];
        var street = Streets[Random.Next(Streets.Length)];
        var zipCodes = ZipCodesByDistrict[district];
        var zipCode = zipCodes[Random.Next(zipCodes.Length)];

        var result = DeliveryAddress.Create(street, "Hà Nội", district, zipCode, "Vietnam");
        return result.IsSuccess ? result.Value : DeliveryAddress.Create("24 Lê Văn Hưu", "Hà Nội", "Hai Bà Trưng", "11616", "Vietnam").Value;
    }

    #endregion

    #region Special Instructions

    private static readonly string[] SpecialInstructions = new[]
    {
        "Vui lòng bấm chuông cửa",
        "Để ở cửa nhà",
        "Gọi điện khi tới",
        "Vui lòng cho thêm khăn giấy",
        "Không hành",
        "Thêm tương ớt riêng",
        "Vui lòng gõ cửa thay vì bấm chuông",
        "Giao hàng không tiếp xúc",
        "Bấm chuông căn hộ 5B",
        "Giao ở cửa sau",
        "Để trước cửa và nhắn tin",
        "Gọi điện trước 5 phút"
    };

    public static string? GenerateSpecialInstructions(decimal probability = 0.3m)
    {
        if (Random.NextDouble() > (double)probability)
            return null;

        return SpecialInstructions[Random.Next(SpecialInstructions.Length)];
    }

    #endregion

    #region Tip Generation

    public static decimal? GenerateTipAmount(decimal probability = 0.4m)
    {
        if (Random.NextDouble() > (double)probability)
            return null;

        // Generate tips between 5000 VND to 20000 VND
        var tipAmounts = new[] { 5000m, 10000m, 15000m, 20000m };
        return tipAmounts[Random.Next(tipAmounts.Length)];
    }

    #endregion

    #region Timestamp Generation

    public static DateTime GenerateRealisticTimestamp(int historyDays, DateTime? baseTime = null)
    {
        var now = baseTime ?? DateTime.UtcNow;
        var daysAgo = Random.Next(0, historyDays + 1);
        var hoursOffset = Random.Next(0, 24);
        var minutesOffset = Random.Next(0, 60);

        return now.AddDays(-daysAgo).AddHours(-hoursOffset).AddMinutes(-minutesOffset);
    }

    /// <summary>
    /// Generates a timestamp that's in the past but realistic for the given order status.
    /// For example, delivered orders should be older than in-progress orders.
    /// </summary>
    public static DateTime GenerateTimestampForOrderStatus(string status, int historyDays)
    {
        var now = DateTime.UtcNow;

        return status switch
        {
            "Delivered" => now.AddDays(-Random.Next(3, historyDays)),
            "Cancelled" => now.AddDays(-Random.Next(2, historyDays)),
            "Rejected" => now.AddDays(-Random.Next(1, historyDays / 2)),
            "ReadyForDelivery" => now.AddMinutes(-Random.Next(10, 60)),
            "Preparing" => now.AddMinutes(-Random.Next(20, 90)),
            "Accepted" => now.AddMinutes(-Random.Next(5, 45)),
            "Placed" => now.AddMinutes(-Random.Next(1, 30)),
            "AwaitingPayment" => now.AddMinutes(-Random.Next(1, 15)),
            _ => now.AddHours(-Random.Next(1, 24))
        };
    }

    #endregion

    #region Item Selection

    /// <summary>
    /// Randomly selects N items from a list.
    /// </summary>
    public static List<T> SelectRandomItems<T>(List<T> items, int min, int max)
    {
        if (items.Count == 0)
            return new List<T>();

        var count = Math.Min(Random.Next(min, max + 1), items.Count);
        var selected = new List<T>();
        var available = new List<T>(items);

        for (int i = 0; i < count; i++)
        {
            var index = Random.Next(available.Count);
            selected.Add(available[index]);
            available.RemoveAt(index);
        }

        return selected;
    }

    /// <summary>
    /// Selects items with a weighted probability distribution.
    /// </summary>
    public static T SelectByWeight<T>(Dictionary<T, int> weightedItems) where T : notnull
    {
        var totalWeight = weightedItems.Values.Sum();
        var randomValue = Random.Next(0, totalWeight);

        var cumulativeWeight = 0;
        foreach (var kvp in weightedItems)
        {
            cumulativeWeight += kvp.Value;
            if (randomValue < cumulativeWeight)
                return kvp.Key;
        }

        return weightedItems.Keys.First();
    }

    #endregion

    #region Payment Gateway References

    public static string GeneratePaymentGatewayReference(string prefix = "seed")
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var random = Random.Next(10000, 99999);
        return $"{prefix}_{timestamp}_{random}";
    }

    #endregion

    #region Quantity Generation

    public static int GenerateQuantity(int min = 1, int max = 3)
    {
        // Most orders have quantity 1, occasionally 2-3
        var weights = new Dictionary<int, int>();
        for (int i = min; i <= max; i++)
        {
            weights[i] = max - i + 1; // Higher weight for lower quantities
        }
        return SelectByWeight(weights);
    }

    #endregion

    #region Review Generation

    /// <summary>
    /// Selects a rating based on weighted distribution.
    /// </summary>
    /// <param name="distribution">Dictionary with rating as key and weight as value</param>
    /// <returns>Selected rating (1-5)</returns>
    public static int SelectRatingByWeight(Dictionary<string, int> distribution)
    {
        var ratingWeights = distribution.ToDictionary(kvp => int.Parse(kvp.Key), kvp => kvp.Value);
        return SelectByWeight(ratingWeights);
    }

    /// <summary>
    /// Determines if a review should have a comment based on percentage.
    /// </summary>
    /// <param name="commentPercentage">Percentage of reviews that should have comments</param>
    /// <returns>True if review should have a comment</returns>
    public static bool ShouldGenerateComment(decimal commentPercentage)
    {
        return Random.NextDouble() < (double)(commentPercentage / 100);
    }

    /// <summary>
    /// Selects a random comment template for the given rating.
    /// </summary>
    /// <param name="templates">Review templates organized by rating</param>
    /// <param name="rating">Rating value (1-5)</param>
    /// <returns>Selected comment template</returns>
    public static string SelectReviewComment(ReviewTemplates templates, int rating)
    {
        var ratingKey = rating.ToString();
        if (!templates.ByRating.TryGetValue(ratingKey, out var templateSet) || templateSet.Comments.Count == 0)
        {
            return $"Rating: {rating} stars"; // Fallback
        }
        
        return templateSet.Comments[Random.Next(templateSet.Comments.Count)];
    }

    /// <summary>
    /// Generates a realistic review timestamp after the delivery time.
    /// </summary>
    /// <param name="deliveryTime">When the order was delivered</param>
    /// <param name="minDays">Minimum days after delivery</param>
    /// <param name="maxDays">Maximum days after delivery</param>
    /// <returns>Random timestamp between min and max days after delivery</returns>
    public static DateTime GenerateReviewTimestamp(DateTime deliveryTime, int minDays, int maxDays)
    {
        var daysAfter = Random.Next(minDays, maxDays + 1);
        var hoursAfter = Random.Next(1, 24);
        var minutesAfter = Random.Next(0, 60);
        
        return deliveryTime.AddDays(daysAfter).AddHours(hoursAfter).AddMinutes(minutesAfter);
    }

    /// <summary>
    /// Determines if a review should receive a reply based on percentage.
    /// </summary>
    /// <param name="replyPercentage">Percentage of reviews that should get replies (0-100)</param>
    /// <returns>True if the review should get a reply</returns>
    public static bool ShouldGenerateReply(decimal replyPercentage)
    {
        return Random.NextDouble() * 100 < (double)replyPercentage;
    }

    /// <summary>
    /// Selects a random reply template from the available templates.
    /// </summary>
    /// <param name="replyTemplates">List of reply templates</param>
    /// <returns>Random reply template</returns>
    public static string SelectReplyTemplate(List<string> replyTemplates)
    {
        if (replyTemplates.Count == 0)
        {
            return "Cảm ơn bạn đã đánh giá!"; // Fallback
        }
        
        return replyTemplates[Random.Next(replyTemplates.Count)];
    }

    #endregion
}
