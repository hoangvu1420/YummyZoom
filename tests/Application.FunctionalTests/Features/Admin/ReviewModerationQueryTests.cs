using YummyZoom.Application.Common.Models;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Reviews.Queries.Moderation;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Admin;

[TestFixture]
public sealed class ReviewModerationQueryTests : BaseTestFixture
{
    [Test]
    public async Task ListReviewsForModeration_ShouldSucceed_WhenNoData_ReturnsEmptyPage()
    {
        var result = await SendAsync(new ListReviewsForModerationQuery(
            PageNumber: 1,
            PageSize: 10,
            IsModerated: null,
            IsHidden: null,
            MinRating: null,
            MaxRating: null,
            HasTextOnly: null,
            RestaurantId: null,
            FromUtc: null,
            ToUtc: null,
            Search: string.Empty,
            SortBy: AdminReviewListSort.Newest));

        result.ShouldBeSuccessful();
        PaginatedList<AdminModerationReviewDto> page = result.Value;
        page.TotalCount.Should().Be(0);
        page.Items.Should().BeEmpty();
    }

    [Test]
    public async Task GetReviewDetailForAdmin_ShouldReturnNotFound_WhenMissing()
    {
        var missingId = Guid.NewGuid();
        var result = await SendAsync(new GetReviewDetailForAdminQuery(missingId));
        result.ShouldBeFailure();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Test]
    public async Task GetReviewAuditTrail_ShouldReturnEmpty_WhenNoAuditRows()
    {
        var anyId = Guid.NewGuid();
        var result = await SendAsync(new GetReviewAuditTrailQuery(anyId));
        result.ShouldBeSuccessful();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();
    }
}
