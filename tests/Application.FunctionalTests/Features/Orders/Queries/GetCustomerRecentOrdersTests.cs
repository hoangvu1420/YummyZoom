using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Features.Orders.Commands.InitiateOrder;
using static YummyZoom.Application.FunctionalTests.Testing;
using YummyZoom.Application.Orders.Queries.GetCustomerRecentOrders;
using YummyZoom.Application.FunctionalTests.Common;

namespace YummyZoom.Application.FunctionalTests.Features.Orders.Queries;

[TestFixture]
public class GetCustomerRecentOrdersTests : BaseTestFixture
{
    [SetUp]
    public void SetUpUser()
    {
        // Authenticate as default customer for order creation
        SetUserId(Testing.TestData.DefaultCustomerId);
    }
    private static GetCustomerRecentOrdersQuery BuildQuery(int page, int size) => new(page, size);

    private async Task<Guid> CreateOrderAsync()
    {
        var command = InitiateOrderTestHelper.BuildValidCommand();
        var result = await SendAsync(command);
        result.IsSuccess.Should().BeTrue(result.Error?.ToString());
        return result.Value.OrderId.Value;
    }

    private async Task<List<Guid>> CreateOrdersAsync(int count)
    {
        var ids = new List<Guid>(count);
        for (int i = 0; i < count; i++)
        {
            // Minimal delay only if flakiness encountered; currently omitted for speed.
            ids.Add(await CreateOrderAsync());
        }
        return ids;
    }

    [Test]
    public async Task HappyPath_FirstPage_ReturnsMostRecentDescending()
    {
        // Arrange
        await CreateOrdersAsync(6);

        // Act
        var result = await SendAsync(BuildQuery(1, 5));

        // Assert
        result.IsSuccess.Should().BeTrue(result.Error?.ToString());
        var page = result.Value;
        page.Items.Count.Should().Be(5);
        page.TotalCount.Should().Be(6);
        page.PageNumber.Should().Be(1);
        page.HasNextPage.Should().BeTrue();
        // Ordering check: timestamps descending
        var placements = page.Items.Select(o => o.PlacementTimestamp).ToList();
        placements.Should().BeInDescendingOrder();
    }

    [Test]
    public async Task SecondPage_ReturnsRemainingAndCountsStable()
    {
        // Arrange
        await CreateOrdersAsync(6);

        // Act
        var page1 = await SendAsync(BuildQuery(1, 5));
        var page2 = await SendAsync(BuildQuery(2, 5));

        // Assert
        page1.IsSuccess.Should().BeTrue();
        page2.IsSuccess.Should().BeTrue();
        page2.Value.Items.Count.Should().Be(1);
        page2.Value.TotalCount.Should().Be(6);
        page2.Value.PageNumber.Should().Be(2);
        page2.Value.HasPreviousPage.Should().BeTrue();
        page2.Value.HasNextPage.Should().BeFalse();
        // Combine ids and ensure uniqueness & full coverage
        var idsAll = page1.Value.Items.Select(i => i.OrderId).Concat(page2.Value.Items.Select(i => i.OrderId)).ToList();
        idsAll.Distinct().Count().Should().Be(6);
    }

    [Test]
    public async Task PageBeyondRange_ReturnsEmpty()
    {
        // Arrange
        await CreateOrdersAsync(6);

        // Act
        var result = await SendAsync(BuildQuery(3, 5)); // pages: 1(5),2(1),3(empty)

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(6);
        result.Value.PageNumber.Should().Be(3);
    }

    [Test]
    public async Task PageSizeBoundary_Size1_Works()
    {
        // Arrange
        await CreateOrdersAsync(2);

        // Act
        var page1 = await SendAsync(BuildQuery(1, 1));
        var page2 = await SendAsync(BuildQuery(2, 1));

        // Assert
        page1.IsSuccess.Should().BeTrue();
        page2.IsSuccess.Should().BeTrue();
        page1.Value.Items.Count.Should().Be(1);
        page2.Value.Items.Count.Should().Be(1);
        page1.Value.TotalCount.Should().Be(2);
        page2.Value.TotalCount.Should().Be(2);
        page1.Value.Items.Single().OrderId.Should().NotBe(page2.Value.Items.Single().OrderId);
    }

    [Test]
    public async Task PageSizeBoundary_MaxAllowed_Works()
    {
        // Arrange - create fewer than max for quick test
        await CreateOrdersAsync(7);

        // Act
        var result = await SendAsync(BuildQuery(1, 100));

        // Assert
        result.IsSuccess.Should().BeTrue(result.Error?.ToString());
        result.Value.Items.Count.Should().Be(7);
        result.Value.TotalCount.Should().Be(7);
        result.Value.PageNumber.Should().Be(1);
        result.Value.TotalPages.Should().Be(1);
    }

    [Test]
    public async Task OwnershipIsolation_AnotherUserCannotSeeMyOrders()
    {
        // Arrange create some orders for default customer
        await CreateOrdersAsync(3);

        // Switch to another user with no roles
        await RunAsUserAsync("otheruser@yummyzoom.test", "Other User", Array.Empty<string>());

        // Act
        var result = await SendAsync(BuildQuery(1, 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(0);
        result.Value.Items.Should().BeEmpty();
    }

    [Test]
    public async Task EmptyResult_NewUserNoOrders()
    {
        // Arrange switch to new user first (no orders at all)
        await RunAsUserAsync("newuser@yummyzoom.test", "New User", Array.Empty<string>());

        // Act
        var result = await SendAsync(BuildQuery(1, 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
        result.Value.PageNumber.Should().Be(1);
    }

    [Test]
    public async Task Validation_InvalidPageSize_ShouldFail()
    {
    // Act + Assert (expect validation exception thrown by pipeline)
    var act = async () => await SendAsync(BuildQuery(1, 0));
    await act.Should().ThrowAsync<ValidationException>();
    }
}
