using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.FunctionalTests.Authorization;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.ApplyCouponToTeamCart;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.Events;

public class CouponAppliedToTeamCartEventHandlerTests : BaseTestFixture
{
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await TeamCartRoleTestHelper.SetupTeamCartAuthorizationTestsAsync();
    }

    [Test]
    public async Task ApplyCoupon_Should_UpdateStore_And_Notify()
    {
        using (var scope = CreateScope())
        {
            var cleanupDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await cleanupDb.Database.ExecuteSqlRawAsync("DELETE FROM \"OutboxMessages\";");
        }

        // Arrange: Create team cart scenario with host using builder
        var scenario = await TeamCartTestBuilder.Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .BuildAsync();

        await DrainOutboxAsync(); // process TeamCartCreated -> create VM

        // Add item to cart (as host)
        await scenario.ActAsHost();
        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 1))).IsSuccess.Should().BeTrue();

        // Lock cart for payment (as host)
        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(scenario.TeamCartId)))
            .IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();

        // Create a coupon and mock notifier
        var couponCode = await CouponTestDataFactory.CreateTestCouponAsync(new CouponTestOptions());

        var notifierMock = new Mock<ITeamCartRealtimeNotifier>(MockBehavior.Strict);
        notifierMock
            .Setup(n => n.NotifyCartUpdated(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        ReplaceService<ITeamCartRealtimeNotifier>(notifierMock.Object);

        // Act: apply coupon (as host)
        (await SendAsync(new ApplyCouponToTeamCartCommand(scenario.TeamCartId, couponCode))).IsSuccess.Should().BeTrue();

        using (var scope = CreateScope())
        {
            var cleanupDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await cleanupDb.Database.ExecuteSqlRawAsync(
                "DELETE FROM \"OutboxMessages\" WHERE \"Content\"::text NOT LIKE {0};",
                $"%{scenario.TeamCartId}%");
        }

        await DrainOutboxAsync(); // process CouponAppliedToTeamCart

        // Assert: VM includes coupon info
        var store = GetService<ITeamCartStore>();
        var vm = await store.GetVmAsync(TeamCartId.Create(scenario.TeamCartId));
        vm.Should().NotBeNull();
        vm!.CouponCode.Should().Be(couponCode);
        vm.DiscountAmount.Should().BeGreaterThanOrEqualTo(0m);
        vm.DiscountCurrency.Should().NotBeNullOrEmpty();

        // Expect 2 notifications: one from CouponAppliedToTeamCart and one from TeamCartQuoteUpdated
        notifierMock.Verify(n => n.NotifyCartUpdated(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
