using FluentAssertions;
using Moq;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.ApplyCouponToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.CreateTeamCart;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.Events;

public class CouponAppliedToTeamCartEventHandlerTests : BaseTestFixture
{
    [Test]
    public async Task ApplyCoupon_Should_UpdateStore_And_Notify()
    {
        // Arrange: host creates cart and locks it
        await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;

        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host User"));
        create.IsSuccess.Should().BeTrue();
        await DrainOutboxAsync(); // process TeamCartCreated -> create VM

        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(create.Value.TeamCartId, burgerId, 1))).IsSuccess.Should().BeTrue();

        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(create.Value.TeamCartId)))
            .IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();

        // Create a coupon and mock notifier
        var couponCode = await CouponTestDataFactory.CreateTestCouponAsync(new CouponTestOptions());

        var notifierMock = new Mock<ITeamCartRealtimeNotifier>(MockBehavior.Strict);
        notifierMock
            .Setup(n => n.NotifyCartUpdated(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        ReplaceService<ITeamCartRealtimeNotifier>(notifierMock.Object);

        // Act: apply coupon
        (await SendAsync(new ApplyCouponToTeamCartCommand(create.Value.TeamCartId, couponCode))).IsSuccess.Should().BeTrue();

        await DrainOutboxAsync(); // process CouponAppliedToTeamCart

        // Assert: VM includes coupon info
        var store = GetService<ITeamCartStore>();
        var vm = await store.GetVmAsync(TeamCartId.Create(create.Value.TeamCartId));
        vm.Should().NotBeNull();
        vm!.CouponCode.Should().Be(couponCode);
        vm.DiscountAmount.Should().BeGreaterThanOrEqualTo(0m);
        vm.DiscountCurrency.Should().NotBeNullOrEmpty();

        notifierMock.Verify(n => n.NotifyCartUpdated(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()), Times.Once);

        await DrainOutboxAsync();
        notifierMock.Verify(n => n.NotifyCartUpdated(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
