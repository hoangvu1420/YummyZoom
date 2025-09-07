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

        // Mock store and notifier
        var storeMock = new Mock<ITeamCartStore>(MockBehavior.Strict);
        TeamCartViewModel? createdVm = null;
        storeMock
            .Setup(s => s.CreateVmAsync(It.IsAny<TeamCartViewModel>(), It.IsAny<CancellationToken>()))
            .Callback<TeamCartViewModel, CancellationToken>((vm, _) => createdVm = vm)
            .Returns(Task.CompletedTask);
        // Other methods are not called in this handler

        var notifierMock = new Mock<ITeamCartRealtimeNotifier>(MockBehavior.Strict);
        notifierMock
            .Setup(n => n.NotifyCartUpdated(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ReplaceService<ITeamCartStore>(storeMock.Object);
        ReplaceService<ITeamCartRealtimeNotifier>(notifierMock.Object);

        // Act: Create team cart
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Alice Host"));
        create.IsSuccess.Should().BeTrue();

        // Drain outbox to process TeamCartCreated
        await DrainOutboxAsync();

        // Assert: store create called with expected VM and notifier called once
        storeMock.Verify(s => s.CreateVmAsync(It.IsAny<TeamCartViewModel>(), It.IsAny<CancellationToken>()), Times.Once);
        notifierMock.Verify(n => n.NotifyCartUpdated(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()), Times.Once);

        createdVm.Should().NotBeNull();
        createdVm!.CartId.Value.Should().Be(create.Value.TeamCartId);
        createdVm.RestaurantId.Should().Be(restaurantId);
        createdVm.Status.ToString().Should().Be("Open");
        createdVm.Version.Should().BeGreaterThanOrEqualTo(1);

        // Idempotency: re-drain should not duplicate
        await DrainOutboxAsync();
        storeMock.Verify(s => s.CreateVmAsync(It.IsAny<TeamCartViewModel>(), It.IsAny<CancellationToken>()), Times.Once);
        notifierMock.Verify(n => n.NotifyCartUpdated(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

