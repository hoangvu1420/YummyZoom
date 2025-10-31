using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Restaurants.Queries.Public.GetMenuItemAvailability;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Restaurants.Queries;

[TestFixture]
public sealed class GetMenuItemAvailabilityQueryTests : BaseTestFixture
{
    [Test]
    public async Task Success_Available_WhenRestaurantAcceptingAndItemAvailable()
    {
        await ResetState();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var itemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);

        var res = await SendAsync(new GetMenuItemAvailabilityQuery(restaurantId, itemId));
        res.ShouldBeSuccessful();
        var dto = res.Value;
        dto.RestaurantId.Should().Be(restaurantId);
        dto.ItemId.Should().Be(itemId);
        dto.IsAvailable.Should().BeTrue();
        dto.TtlSeconds.Should().Be(15);
        dto.Stock.Should().BeNull();
    }

    [Test]
    public async Task Success_Unavailable_WhenItemUnavailable()
    {
        await ResetState();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var itemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.BuffaloWings);

        // Flip item availability to false via domain aggregate
        var menuItem = await FindAsync<MenuItem>(MenuItemId.Create(itemId));
        menuItem.Should().NotBeNull();
        menuItem!.ChangeAvailability(false);
        menuItem.ClearDomainEvents();
        await UpdateAsync(menuItem);

        var res = await SendAsync(new GetMenuItemAvailabilityQuery(restaurantId, itemId));
        res.ShouldBeSuccessful();
        res.Value.IsAvailable.Should().BeFalse();
    }

    [Test]
    public async Task Caching_ReturnsCachedValue_OnSecondCallEvenAfterChange()
    {
        await ResetState();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var itemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.MargheritaPizza);

        // First call caches available=true
        var first = await SendAndUnwrapAsync(new GetMenuItemAvailabilityQuery(restaurantId, itemId));
        first.IsAvailable.Should().BeTrue();

        // Immediately change availability to false in DB
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"MenuItems\" SET \"IsAvailable\" = {false} WHERE \"Id\" = {itemId}");
        }

        // Second call should still return cached true within TTL
        var second = await SendAndUnwrapAsync(new GetMenuItemAvailabilityQuery(restaurantId, itemId));
        second.IsAvailable.Should().BeTrue();
    }

    [Test]
    public async Task NotFound_WhenMissingOrDeleted()
    {
        await ResetState();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var resultMissing = await SendAsync(new GetMenuItemAvailabilityQuery(restaurantId, Guid.NewGuid()));
        resultMissing.IsFailure.Should().BeTrue();
        resultMissing.Error.Code.Should().Be("Public.MenuItemAvailability.NotFound");

        var itemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ChocolateCake);
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"MenuItems\" SET \"IsDeleted\" = {true}, \"DeletedOn\" = {DateTimeOffset.UtcNow} WHERE \"Id\" = {itemId}");
        }
        var resultDeleted = await SendAsync(new GetMenuItemAvailabilityQuery(restaurantId, itemId));
        resultDeleted.IsFailure.Should().BeTrue();
        resultDeleted.Error.Code.Should().Be("Public.MenuItemAvailability.NotFound");
    }

    [Test]
    public async Task Validation_EmptyIds_ShouldThrow()
    {
        var act1 = async () => await SendAsync(new GetMenuItemAvailabilityQuery(Guid.Empty, Guid.NewGuid()));
        await act1.Should().ThrowAsync<ValidationException>();

        var act2 = async () => await SendAsync(new GetMenuItemAvailabilityQuery(Guid.NewGuid(), Guid.Empty));
        await act2.Should().ThrowAsync<ValidationException>();
    }
}
