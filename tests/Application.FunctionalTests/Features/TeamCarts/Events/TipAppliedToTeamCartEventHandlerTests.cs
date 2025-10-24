using FluentAssertions;
using Moq;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.FunctionalTests.Authorization;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.ApplyTipToTeamCart;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.Events;

public class TipAppliedToTeamCartEventHandlerTests : BaseTestFixture
{
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await TeamCartRoleTestHelper.SetupTeamCartAuthorizationTestsAsync();
    }

    [Test]
    public async Task ApplyTip_Should_UpdateStore_And_Notify()
    {
        // Arrange: Create team cart scenario with host using builder
        var scenario = await TeamCartTestBuilder.Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .BuildAsync();

        await DrainOutboxAsync(); // process TeamCartCreated -> create VM

        // Add one item so we can lock the cart (as host)
        await scenario.ActAsHost();
        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 1))).IsSuccess.Should().BeTrue();

        // Lock the cart for payment (as host)
        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync(); // process lock -> VM status update when handler exists

        // Mock notifier to verify a single notification
        var notifierMock = new Mock<ITeamCartRealtimeNotifier>(MockBehavior.Strict);
        notifierMock
            .Setup(n => n.NotifyCartUpdated(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        ReplaceService<ITeamCartRealtimeNotifier>(notifierMock.Object);

        // Act: Apply a tip of 5.00 (as host)
        const decimal tipAmount = 5.00m;
        (await SendAsync(new ApplyTipToTeamCartCommand(scenario.TeamCartId, tipAmount))).IsSuccess.Should().BeTrue();

        await DrainOutboxAsync(); // process TipAppliedToTeamCart -> update VM

        // Assert: VM reflects tip
        var store = GetService<ITeamCartStore>();
        var vm = await store.GetVmAsync(TeamCartId.Create(scenario.TeamCartId));
        vm.Should().NotBeNull();
        vm!.TipAmount.Should().Be(tipAmount);
        vm.TipCurrency.Should().NotBeNullOrEmpty(); // default currency from domain

        // Expect 2 notifications: one from TipAppliedToTeamCart and one from TeamCartQuoteUpdated
        notifierMock.Verify(n => n.NotifyCartUpdated(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}

