using System.Net;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Reviews.Queries.Moderation;
using SharedResult = YummyZoom.SharedKernel.Result;
using YummyZoom.Web.ApiContractTests.Infrastructure;

namespace YummyZoom.Web.ApiContractTests.Admin;

public sealed class AdminReviewModerationContractTests
{
	[Test]
	public async Task ListReviews_WhenAuthorized_BindsFiltersAndReturnsPage()
	{
		var factory = new ApiContractWebAppFactory();
		var client = factory.CreateClient();
		client.DefaultRequestHeaders.Add("x-test-user-id", "admin-x");
		client.DefaultRequestHeaders.Add("x-test-roles", "Administrator");

		var items = new List<AdminModerationReviewDto>
		{
			new(Guid.NewGuid(), Guid.NewGuid(), "Rest A", Guid.NewGuid(), 2, "bad", DateTime.UtcNow, false, false, null),
			new(Guid.NewGuid(), Guid.NewGuid(), "Rest B", Guid.NewGuid(), 5, "great", DateTime.UtcNow.AddMinutes(-1), true, false, DateTime.UtcNow)
		};
		var page = new PaginatedList<AdminModerationReviewDto>(items, count: 7, pageNumber: 2, pageSize: 2);

		factory.Sender.RespondWith(request =>
		{
			request.Should().BeOfType<ListReviewsForModerationQuery>();
			var q = (ListReviewsForModerationQuery)request;
			q.PageNumber.Should().Be(2);
			q.PageSize.Should().Be(2);
			q.IsModerated.Should().BeTrue();
			q.IsHidden.Should().BeFalse();
			q.MinRating.Should().Be(1);
			q.MaxRating.Should().Be(3);
			q.HasTextOnly.Should().BeTrue();
			q.RestaurantId.Should().BeNull();
			q.Search.Should().Be("refund");
			q.SortBy.Should().Be(AdminReviewListSort.LowestRating);
			return SharedResult.Success(page);
		});

		var response = await client.GetAsync("/api/v1/admin/reviews?pageNumber=2&pageSize=2&isModerated=true&isHidden=false&minRating=1&maxRating=3&hasTextOnly=true&search=refund&sortBy=LowestRating");
		var payload = await response.Content.ReadAsStringAsync();

		TestContext.WriteLine($"RESPONSE {(int)response.StatusCode} {response.StatusCode}\n{payload}");

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		using var doc = JsonDocument.Parse(payload);
		doc.RootElement.GetProperty("totalCount").GetInt32().Should().Be(7);
		doc.RootElement.GetProperty("items").GetArrayLength().Should().Be(2);
	}

	[Test]
	public async Task GetDetail_WhenAuthorized_ReturnsDto()
	{
		var factory = new ApiContractWebAppFactory();
		var client = factory.CreateClient();
		client.DefaultRequestHeaders.Add("x-test-user-id", "admin-y");
		client.DefaultRequestHeaders.Add("x-test-roles", "Administrator");

		var reviewId = Guid.NewGuid();
		var dto = new AdminModerationReviewDetailDto(
			reviewId,
			Guid.NewGuid(),
			"Alpha",
			Guid.NewGuid(),
			4,
			"ok",
			"reply text",
			Guid.NewGuid(),
			4.2,
			120,
			DateTime.UtcNow,
			true,
			false,
			DateTime.UtcNow);

		factory.Sender.RespondWith(request =>
		{
			request.Should().BeOfType<GetReviewDetailForAdminQuery>();
			var q = (GetReviewDetailForAdminQuery)request;
			q.ReviewId.Should().Be(reviewId);
			return SharedResult.Success(dto);
		});

		var response = await client.GetAsync($"/api/v1/admin/reviews/{reviewId}");
		var payload = await response.Content.ReadAsStringAsync();
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		using var doc = JsonDocument.Parse(payload);
		doc.RootElement.GetProperty("reviewId").GetGuid().Should().Be(reviewId);
	}

	[Test]
	public async Task GetAudit_WhenAuthorized_ReturnsList()
	{
		var factory = new ApiContractWebAppFactory();
		var client = factory.CreateClient();
		client.DefaultRequestHeaders.Add("x-test-user-id", "admin-z");
		client.DefaultRequestHeaders.Add("x-test-roles", "Administrator");

		var reviewId = Guid.NewGuid();
		var items = new List<ReviewModerationAuditDto>
		{
			new("Moderate", "spam", Guid.NewGuid(), "Alice", DateTime.UtcNow.AddMinutes(-2)),
			new("Hide", "PII", Guid.NewGuid(), "Bob", DateTime.UtcNow.AddMinutes(-1))
		};

		factory.Sender.RespondWith(request =>
		{
			request.Should().BeOfType<GetReviewAuditTrailQuery>();
			var q = (GetReviewAuditTrailQuery)request;
			q.ReviewId.Should().Be(reviewId);
			return SharedResult.Success<IReadOnlyList<ReviewModerationAuditDto>>(items);
		});

		var response = await client.GetAsync($"/api/v1/admin/reviews/{reviewId}/audit");
		var payload = await response.Content.ReadAsStringAsync();
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		using var doc = JsonDocument.Parse(payload);
		doc.RootElement.GetArrayLength().Should().Be(2);
	}

	[Test]
	public async Task Endpoints_WhenMissingAdminRole_Return403()
	{
		var factory = new ApiContractWebAppFactory();
		var client = factory.CreateClient();
		client.DefaultRequestHeaders.Add("x-test-user-id", "user-no-admin");

		var listResponse = await client.GetAsync("/api/v1/admin/reviews");
		var detailResponse = await client.GetAsync($"/api/v1/admin/reviews/{Guid.NewGuid()}");
		var auditResponse = await client.GetAsync($"/api/v1/admin/reviews/{Guid.NewGuid()}/audit");

		listResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
		detailResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
		auditResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
	}
}
