namespace YummyZoom.Infrastructure.Persistence.EfCore.Seeding.Options;

/// <summary>
/// Configuration options for the Review seeder.
/// </summary>
public sealed class ReviewSeedingOptions
{
    /// <summary>
    /// Percentage of delivered orders that should receive reviews (0-100).
    /// </summary>
    public decimal ReviewPercentage { get; set; } = 40;

    /// <summary>
    /// Percentage of reviews that should receive a restaurant reply (0-100).
    /// </summary>
    public decimal ReplyPercentage { get; set; } = 50;

    /// <summary>
    /// When true, generates comments for reviews based on rating.
    /// When false, reviews have ratings only without comments.
    /// </summary>
    public bool GenerateComments { get; set; } = true;

    /// <summary>
    /// Pool of positive comments for 4-5 star reviews.
    /// </summary>
    public string[] PositiveComments { get; set; } = new[]
    {
        "Đồ ăn ngon và giao hàng nhanh!",
        "Chất lượng tuyệt vời, sẽ đặt lại!",
        "Ngon nhất quận!",
        "Khẩu phần vừa đủ và rất ngon!",
        "Rất đáng để thử!",
        "Dịch vụ xuất sắc!",
        "Vượt quá mong đợi!",
        "Nguyên liệu tươi ngon và hương vị tuyệt hảo!",
        "Bữa ăn hoàn hảo, cảm ơn!",
        "Chất lượng luôn ổn định!",
        "Món ăn đậm đà, đúng điệu!",
        "Giao nhanh, đồ ăn còn nóng!",
        "Rất hài lòng với chất lượng!"
    };

    /// <summary>
    /// Pool of neutral comments for 3 star reviews.
    /// </summary>
    public string[] NeutralComments { get; set; } = new[]
    {
        "Bữa ăn tạm được, không có gì đặc biệt.",
        "Ổn.",
        "Trung bình.",
        "Đồ ăn bình thường.",
        "Đúng như mong đợi.",
        "Chất lượng tiêu chuẩn.",
        "Không tệ, nhưng cũng không xuất sắc.",
        "Có thể tốt hơn."
    };

    /// <summary>
    /// Pool of negative comments for 1-2 star reviews.
    /// </summary>
    public string[] NegativeComments { get; set; } = new[]
    {
        "Đồ ăn đến lạnh.",
        "Giao hàng lâu quá.",
        "Không như mong đợi.",
        "Khẩu phần quá ít.",
        "Chất lượng có thể tốt hơn.",
        "Trải nghiệm đáng thất vọng.",
        "Không xứng đáng với giá tiền.",
        "Dưới mong đợi.",
        "Phải đợi quá lâu.",
        "Không tươi.",
        "Hơi mặn.",
        "Thiếu gia vị."
    };

    /// <summary>
    /// Pool of restaurant reply templates.
    /// {0} can be used as a placeholder for the customer name or generic greeting.
    /// </summary>
    public string[] ReplyTemplates { get; set; } = new[]
    {
        "Cảm ơn phản hồi của bạn!",
        "Rất vui vì bạn thích món ăn!",
        "Chúng tôi xin lỗi về trải nghiệm này. Vui lòng liên hệ để chúng tôi khắc phục.",
        "Cảm ơn đã chọn chúng tôi! Rất mong được phục vụ bạn lần sau.",
        "Chúng tôi trân trọng đánh giá của bạn và sẽ cố gắng cải thiện.",
        "Sự hài lòng của bạn là ưu tiên của chúng tôi. Cảm ơn đã cho chúng tôi biết!",
        "Chúng tôi rất tiếc về trải nghiệm của bạn. Chúng tôi muốn bù đắp cho bạn.",
        "Cảm ơn những lời khen! Điều này có ý nghĩa rất lớn với đội ngũ của chúng tôi.",
        "Chúng tôi đánh giá cao phản hồi của bạn và luôn nỗ lực cải thiện.",
        "Cảm ơn đã dành thời gian đánh giá!",
        "Rất vui vì món ăn hợp khẩu vị của bạn!",
        "Xin lỗi vì sự bất tiện. Chúng tôi sẽ cải thiện dịch vụ."
    };
}
