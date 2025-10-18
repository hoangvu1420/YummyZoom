namespace YummyZoom.Infrastructure.Persistence.EfCore.Seeding.Options;

/// <summary>
/// Configuration options for the Review seeder.
/// </summary>
public sealed class ReviewSeedingOptions
{
    /// <summary>
    /// Percentage of delivered orders that should receive reviews (0-100).
    /// </summary>
    public decimal ReviewCoveragePercentage { get; set; } = 70;

    /// <summary>
    /// Distribution of review ratings as percentages (must sum to 100).
    /// Key is the rating value (1-5), value is the percentage (0-100).
    /// </summary>
    public Dictionary<string, int> RatingDistribution { get; set; } = new()
    {
        { "1", 3 },   // 3%
        { "2", 7 },   // 7%
        { "3", 20 },  // 20%
        { "4", 35 },  // 35%
        { "5", 35 }   // 35%
    };

    /// <summary>
    /// Percentage of reviews that should have comments (vs rating-only) (0-100).
    /// </summary>
    public decimal CommentPercentage { get; set; } = 75;

    /// <summary>
    /// Review content templates organized by rating.
    /// </summary>
    public ReviewTemplates Templates { get; set; } = new();

    /// <summary>
    /// Minimum days after delivery when review can be submitted.
    /// </summary>
    public int MinDaysAfterDelivery { get; set; } = 1;

    /// <summary>
    /// Maximum days after delivery when review can be submitted.
    /// </summary>
    public int MaxDaysAfterDelivery { get; set; } = 14;

    /// <summary>
    /// Percentage of reviews that should receive replies from restaurants (0-100).
    /// </summary>
    public decimal ReplyPercentage { get; set; } = 90;

    /// <summary>
    /// Reply templates for restaurant responses.
    /// </summary>
    public List<string> ReplyTemplates { get; set; } = new()
    {
        "Cảm ơn bạn đã đánh giá! Chúng tôi rất vui khi bạn hài lòng với dịch vụ.",
        "Xin chân thành cảm ơn phản hồi tích cực của bạn. Chúng tôi sẽ tiếp tục cố gắng!",
        "Cảm ơn bạn đã tin tưởng và ủng hộ nhà hàng. Hẹn gặp lại bạn lần sau!",
        "Rất cảm ơn đánh giá của bạn. Chúng tôi luôn nỗ lực để mang đến trải nghiệm tốt nhất.",
        "Cảm ơn bạn đã dành thời gian đánh giá. Phản hồi của bạn rất có ý nghĩa với chúng tôi!",
        "Chúng tôi rất vui khi được phục vụ bạn. Cảm ơn những lời nhận xét tuyệt vời!",
        "Xin cảm ơn bạn đã chia sẻ trải nghiệm. Chúng tôi sẽ cố gắng cải thiện hơn nữa!",
        "Cảm ơn phản hồi của bạn. Chúng tôi sẽ chuyển lời khen đến đội ngũ bếp và giao hàng.",
        "Rất cảm ơn bạn đã đánh giá cao dịch vụ của chúng tôi. Hẹn sớm được phục vụ bạn lại!",
        "Cảm ơn bạn đã luôn ủng hộ nhà hàng. Những phản hồi như thế này động viên chúng tôi rất nhiều!",
        "Xin lỗi vì trải nghiệm chưa được như mong đợi. Chúng tôi sẽ cải thiện để phục vụ bạn tốt hơn.",
        "Cảm ơn phản hồi của bạn. Chúng tôi đã ghi nhận và sẽ khắc phục những điểm chưa tốt.",
        "Xin lỗi vì sự bất tiện này. Chúng tôi sẽ rút kinh nghiệm để không tái diễn tình trạng tương tự.",
        "Cảm ơn bạn đã góp ý. Chúng tôi sẽ cố gắng cải thiện chất lượng món ăn và dịch vụ giao hàng.",
        "Xin lỗi vì đã làm bạn thất vọng. Chúng tôi rất trân trọng phản hồi và sẽ cải thiện ngay."
    };
}

/// <summary>
/// Review content templates organized by rating.
/// </summary>
public sealed class ReviewTemplates
{
    /// <summary>
    /// Template sets indexed by rating value (1-5).
    /// </summary>
    public Dictionary<string, ReviewTemplateSet> ByRating { get; set; } = new()
    {
        { "5", new ReviewTemplateSet 
        { 
            Weight = 35, 
            Comments = new List<string>
            {
                "Đồ ăn tuyệt vời và giao hàng nhanh!",
                "Chất lượng xuất sắc, rất khuyến nghị!",
                "Hương vị tuyệt vời, sẽ đặt lại!",
                "Bữa ăn hoàn hảo, vượt mong đợi!",
                "Món ăn ngon, dịch vụ tuyệt vời!",
                "Chất lượng tuyệt hảo, đáng tiền!",
                "Bữa ăn ngon nhất từ lâu rồi!",
                "Nguyên liệu tươi, chế biến đẹp mắt!",
                "Giao nhanh, đồ ăn nóng và ngon!",
                "Hoàn toàn hài lòng, năm sao!"
            }
        }},
        { "4", new ReviewTemplateSet 
        { 
            Weight = 35, 
            Comments = new List<string>
            {
                "Đồ ăn ngon, giá cả hợp lý.",
                "Hương vị tốt, giao đúng giờ.",
                "Lựa chọn tốt, hài lòng với đơn hàng.",
                "Khá ngon, sẽ đặt lại.",
                "Món ăn ngon, phần ăn vừa phải.",
                "Chất lượng tốt, phục vụ nhanh.",
                "Bữa ăn ngon, đáng giá tiền.",
                "Chế biến kỹ, nguyên liệu tươi.",
                "Trải nghiệm tốt nói chung.",
                "Chất lượng ổn định, hài lòng với đơn hàng."
            }
        }},
        { "3", new ReviewTemplateSet 
        { 
            Weight = 20, 
            Comments = new List<string>
            {
                "Đồ ăn tạm được, không có gì đặc biệt.",
                "Chất lượng trung bình, phần ăn ổn.",
                "Không tệ, nhưng cần cải thiện.",
                "Bữa ăn chấp nhận được, dịch vụ bình thường.",
                "Ổn thôi, đúng mong đợi.",
                "Đồ ăn tạm được, có thể tốt hơn.",
                "Trải nghiệm bình thường, không nổi bật.",
                "Hương vị trung bình, giá hợp lý.",
                "Chất lượng tiêu chuẩn, giao hàng ổn.",
                "Bữa ăn tạm được, có thể cân nhắc lại."
            }
        }},
        { "2", new ReviewTemplateSet 
        { 
            Weight = 7, 
            Comments = new List<string>
            {
                "Đồ ăn lạnh khi giao đến.",
                "Chất lượng thất vọng so với giá cả.",
                "Giao lâu, đồ ăn nguội.",
                "Không như mong đợi, dưới trung bình.",
                "Đồ ăn nhạt nhẽo, giá cao.",
                "Chất lượng kém, giao chậm.",
                "Trải nghiệm không như mong đợi.",
                "Đồ ăn lạnh, vị không ngon.",
                "Không đáng tiền, dịch vụ kém.",
                "Mong đợi tốt hơn, khá thất vọng."
            }
        }},
        { "1", new ReviewTemplateSet 
        { 
            Weight = 3, 
            Comments = new List<string>
            {
                "Chất lượng rất kém, đồ ăn lạnh.",
                "Trải nghiệm tệ hại, không khuyến nghị.",
                "Đồ ăn không thể ăn được, lãng phí tiền.",
                "Bữa ăn tệ hại, hoàn toàn thất vọng.",
                "Chất lượng khủng khiếp, dịch vụ tệ.",
                "Trải nghiệm giao đồ ăn tệ nhất từ trước đến nay.",
                "Hoàn toàn không hài lòng, tránh xa chỗ này.",
                "Đồ ăn kinh tởm, chất lượng cực kém.",
                "Vị tệ hại, đồ ăn lạnh và ôi thiu.",
                "Hoàn toàn tệ hại, hối hận vì đã đặt."
            }
        }}
    };
}

/// <summary>
/// Template set for a specific rating with weighted selection.
/// </summary>
public sealed class ReviewTemplateSet
{
    /// <summary>
    /// Weight for random selection (higher = more likely).
    /// </summary>
    public int Weight { get; set; } = 1;

    /// <summary>
    /// List of comment templates for this rating.
    /// </summary>
    public List<string> Comments { get; set; } = new();
}
