using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.Coupons.Queries.FastCheck;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
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
                new(Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger), 2)
            });

        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        var burger = await FindAsync<MenuItem>(MenuItemId.Create(burgerId));
        burger.Should().NotBeNull();

        // Act
        var result = await SendAsync(query);

        // Assert
        result.ShouldBeSuccessful();
        result.Value.Should().NotBeNull();
        result.Value.CartSummary.Subtotal.Should().Be(burger!.BasePrice.Amount * 2);
        result.Value.CartSummary.ItemCount.Should().Be(2);
        result.Value.CartSummary.Currency.Should().Be(burger.BasePrice.Currency);
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
                new(Guid.Empty, 0)
            });

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(query))
            .Should().ThrowAsync<ValidationException>();
    }
}
