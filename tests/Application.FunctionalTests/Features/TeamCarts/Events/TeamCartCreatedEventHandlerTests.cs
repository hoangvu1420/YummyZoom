using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.TeamCarts.Commands.CreateTeamCart;
using YummyZoom.Application.TeamCarts.Models;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Infrastructure.Data;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.Events;

/// <summary>
/// Functional tests for TeamCartCreatedEventHandler verifying:
/// 1) Outbox -> handler creates VM in ITeamCartStore and
/// 2) Notifies via ITeamCartRealtimeNotifier once (idempotent on re-drain).
/// </summary>
public class TeamCartCreatedEventHandlerTests : BaseTestFixture
{
    [Test]
    public async Task CreateTeamCart_Should_CreateVm_And_NotifyRealtime()
    {
        // Arrange authenticated user
        await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;

        // Use real Redis-backed store from test infrastructure; only notifier is mocked
        var notifierMock = new Mock<ITeamCartRealtimeNotifier>(MockBehavior.Strict);
        notifierMock
            .Setup(n => n.NotifyCartUpdated(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        ReplaceService<ITeamCartRealtimeNotifier>(notifierMock.Object);

        // Act: Create team cart
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Alice Host"));
        create.IsSuccess.Should().BeTrue();

        // Drain outbox to process TeamCartCreated
        await DrainOutboxAsync();

        // Assert: VM actually created in Redis store and notifier called once
        var store = GetService<ITeamCartStore>();
        var vm = await store.GetVmAsync(TeamCartId.Create(create.Value.TeamCartId));
        vm.Should().NotBeNull();
        vm!.CartId.Value.Should().Be(create.Value.TeamCartId);
        vm.RestaurantId.Should().Be(restaurantId);
        vm.Status.ToString().Should().Be("Open");
        vm.Version.Should().BeGreaterThanOrEqualTo(1);
        notifierMock.Verify(n => n.NotifyCartUpdated(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()), Times.Once);

        // Idempotency: re-drain should not duplicate
        await DrainOutboxAsync();
        notifierMock.Verify(n => n.NotifyCartUpdated(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
