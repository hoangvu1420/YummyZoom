using YummyZoom.Application.FunctionalTests.Authorization;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.ApplyTipToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.LockTeamCartForPayment;
using YummyZoom.Application.Common.Interfaces.IServices;
using static YummyZoom.Application.FunctionalTests.Testing;
using YummyZoom.Application.FunctionalTests.Common;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.Events;

public class TeamCartQuoteUpdatedEventHandlerTests : BaseTestFixture
{
    [Test]
    public async Task Lock_Then_DrainOutbox_Updates_Quote_In_VM()
    {
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host")
            .WithGuest("Guest")
            .BuildAsync();
        await DrainOutboxAsync();

        // Add items for both members
        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);

        await scenario.ActAsHost();
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 2))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();

        await scenario.ActAsGuest("Guest");
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 1))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();

        // Before lock, quote version should be 0
        var store = GetService<ITeamCartStore>();
        var vmBefore = await store.GetVmAsync(Domain.TeamCartAggregate.ValueObjects.TeamCartId.Create(scenario.TeamCartId));
        vmBefore.Should().NotBeNull();
        (vmBefore!.QuoteVersion <= 0).Should().BeTrue();

        // Lock (computes quote); without draining outbox, VM should not yet have quote
        await scenario.ActAsHost();
        (await SendAsync(new LockTeamCartForPaymentCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();

        var vmNoDrain = await store.GetVmAsync(Domain.TeamCartAggregate.ValueObjects.TeamCartId.Create(scenario.TeamCartId));
        vmNoDrain.Should().NotBeNull();
        vmNoDrain!.QuoteVersion.Should().Be(vmBefore.QuoteVersion); // unchanged until outbox processed

        // Drain outbox -> TeamCartQuoteUpdated handler updates VM
        await DrainOutboxAsync();

        var vmAfter = await store.GetVmAsync(Domain.TeamCartAggregate.ValueObjects.TeamCartId.Create(scenario.TeamCartId));
        vmAfter.Should().NotBeNull();
        vmAfter!.QuoteVersion.Should().BeGreaterThan(0);
        // Quoted amounts set for members present
        vmAfter!.Members.Should().OnlyContain(m => m.QuotedAmount >= 0);
    }

    [Test]
    public async Task ApplyTip_Increments_QuoteVersion_And_Is_Idempotent()
    {
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host")
            .WithGuest("Guest")
            .BuildAsync();
        await DrainOutboxAsync();

        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        await scenario.ActAsHost();
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 1))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();

        (await SendAsync(new LockTeamCartForPaymentCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();

        var store = GetService<ITeamCartStore>();
        var vmBeforeTip = await store.GetVmAsync(Domain.TeamCartAggregate.ValueObjects.TeamCartId.Create(scenario.TeamCartId));
        vmBeforeTip.Should().NotBeNull();
        var versionBefore = vmBeforeTip!.QuoteVersion;
        versionBefore.Should().BeGreaterThan(0);

        // Apply tip without draining: version unchanged
        (await SendAsync(new ApplyTipToTeamCartCommand(scenario.TeamCartId, 5.00m))).IsSuccess.Should().BeTrue();
        var vmNoDrain = await store.GetVmAsync(Domain.TeamCartAggregate.ValueObjects.TeamCartId.Create(scenario.TeamCartId));
        vmNoDrain!.QuoteVersion.Should().Be(versionBefore);

        // Drain once: version increments
        await DrainOutboxAsync();
        var vmAfter = await store.GetVmAsync(Domain.TeamCartAggregate.ValueObjects.TeamCartId.Create(scenario.TeamCartId));
        vmAfter!.QuoteVersion.Should().BeGreaterThan(versionBefore);
        var versionAfter = vmAfter.QuoteVersion;

        // Drain again (no new messages): version stays the same (idempotent)
        await DrainOutboxAsync();
        var vmAfterSecondDrain = await store.GetVmAsync(Domain.TeamCartAggregate.ValueObjects.TeamCartId.Create(scenario.TeamCartId));
        vmAfterSecondDrain!.QuoteVersion.Should().Be(versionAfter);
    }
}
