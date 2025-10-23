using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.Coupons.Queries.FastCheck;
using YummyZoom.Application.FunctionalTests.Common;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Coupons.Queries;

[TestFixture]
public class FastCouponCheckTests : BaseTestFixture
{
    [Test]
    public async Task FastCouponCheck_WithValidCart_ShouldReturnSuggestions()
    {
        // Arrange
        SetUserId(Testing.TestData.DefaultCustomerId);

        var query = new FastCouponCheckQuery(
            Testing.TestData.DefaultRestaurantId,
            new List<FastCouponCheckItemDto>
            {
                new(Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger), 
                    Testing.TestData.GetMenuCategoryId("Main Dishes"), 
                    2, 15.99m, "USD")
            });

        // Act
        var result = await SendAsync(query);

        // Assert
        result.ShouldBeSuccessful();
        result.Value.Should().NotBeNull();
        result.Value.CartSummary.Subtotal.Should().Be(31.98m);
        result.Value.CartSummary.ItemCount.Should().Be(2);
        result.Value.CartSummary.Currency.Should().Be("USD");
    }

    [Test]
    public async Task FastCouponCheck_WithEmptyCart_ShouldReturnValidationError()
    {
        // Arrange
        SetUserId(Testing.TestData.DefaultCustomerId);

        var query = new FastCouponCheckQuery(
            Testing.TestData.DefaultRestaurantId,
            Array.Empty<FastCouponCheckItemDto>());

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(query))
            .Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task FastCouponCheck_WithInvalidItems_ShouldReturnValidationError()
    {
        // Arrange
        SetUserId(Testing.TestData.DefaultCustomerId);

        var query = new FastCouponCheckQuery(
            Testing.TestData.DefaultRestaurantId,
            new List<FastCouponCheckItemDto>
            {
                new(Guid.Empty, Guid.Empty, 0, -1m, "")
            });

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(query))
            .Should().ThrowAsync<ValidationException>();
    }
}
