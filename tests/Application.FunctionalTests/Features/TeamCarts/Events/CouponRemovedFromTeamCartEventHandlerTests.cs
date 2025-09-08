using FluentAssertions;
using Moq;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.ApplyCouponToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.CreateTeamCart;
using YummyZoom.Application.TeamCarts.Commands.LockTeamCartForPayment;
using YummyZoom.Application.TeamCarts.Commands.RemoveCouponFromTeamCart;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.Events;

public class CouponRemovedFromTeamCartEventHandlerTests : BaseTestFixture
{
    [Test]
    public async Task RemoveCoupon_Should_UpdateStore_And_Notify()
    {
        // Arrange: host creates cart and locks it
        await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;

        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host User"));
        create.IsSuccess.Should().BeTrue();
        await DrainOutboxAsync(); // process TeamCartCreated -> create VM

        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(create.Value.TeamCartId, burgerId, 1))).IsSuccess.Should().BeTrue();

        (await SendAsync(new LockTeamCartForPaymentCommand(create.Value.TeamCartId))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();

        // Apply a coupon first
        var couponCode = await CouponTestDataFactory.CreateTestCouponAsync(new CouponTestOptions());
        (await SendAsync(new ApplyCouponToTeamCartCommand(create.Value.TeamCartId, couponCode))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync(); // process CouponAppliedToTeamCart

        // Mock notifier to verify single notification on removal
        var notifierMock = new Mock<ITeamCartRealtimeNotifier>(MockBehavior.Strict);
        notifierMock
            .Setup(n => n.NotifyCartUpdated(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        ReplaceService<ITeamCartRealtimeNotifier>(notifierMock.Object);

        // Act: remove coupon
        (await SendAsync(new RemoveCouponFromTeamCartCommand(create.Value.TeamCartId))).IsSuccess.Should().BeTrue();

        await DrainOutboxAsync(); // process CouponRemovedFromTeamCart

        // Assert: VM reflects removal
        var store = GetService<ITeamCartStore>();
        var vm = await store.GetVmAsync(TeamCartId.Create(create.Value.TeamCartId));
        vm.Should().NotBeNull();
        vm!.CouponCode.Should().BeNull();
        vm.DiscountAmount.Should().Be(0m);

        notifierMock.Verify(n => n.NotifyCartUpdated(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()), Times.Once);

        await DrainOutboxAsync();
        notifierMock.Verify(n => n.NotifyCartUpdated(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

