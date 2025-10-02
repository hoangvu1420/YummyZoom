using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Authorization;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.ApplyTipToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.CommitToCodPayment;
using YummyZoom.Application.TeamCarts.Commands.LockTeamCartForPayment;
using YummyZoom.Application.TeamCarts.Queries.GetTeamCartDetails;
using YummyZoom.Application.TeamCarts.Queries.GetTeamCartRealTimeViewModel;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.Queries;

/// <summary>
/// Functional tests for GetTeamCartRealTimeViewModelQuery covering:
/// 1. Basic retrieval from Redis cache
/// 2. Authorization (member-only access)
/// 3. Real-time updates after domain events
/// 4. Complex scenarios with multiple users and state changes
/// 5. Error handling for non-existent carts
/// </summary>
public class GetTeamCartRealTimeViewModelQueryTests : BaseTestFixture
{
    [Test]
    public async Task GetRealTimeViewModel_WithBasicCart_ShouldReturnFromRedis()
    {
        // Arrange: Create team cart scenario and drain outbox to populate Redis
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .BuildAsync();

        // Drain outbox to ensure Redis VM is created
        await DrainOutboxAsync();

        // Act: Get real-time view model
        var result = await SendAsync(new GetTeamCartRealTimeViewModelQuery(scenario.TeamCartId));

        // Assert: Should return the view model from Redis
        result.IsSuccess.Should().BeTrue();
        var vm = result.Value.TeamCart;

        vm.CartId.Value.Should().Be(scenario.TeamCartId);
        vm.RestaurantId.Should().Be(Testing.TestData.DefaultRestaurantId);
        vm.Status.Should().Be(TeamCartStatus.Open);
        vm.Members.Should().HaveCount(1);
        vm.Members[0].UserId.Should().Be(scenario.HostUserId);
        vm.Members[0].Name.Should().Be("Host User");
        vm.Members[0].Role.Should().Be("Host");
        vm.Items.Should().BeEmpty();
        vm.Version.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task GetRealTimeViewModel_WithItemsAndMultipleMembers_ShouldReturnUpdatedState()
    {
        // Arrange: Create cart scenario with host and guest
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .WithGuest("Guest User")
            .BuildAsync();
        await DrainOutboxAsync();

        // Add items as host
        await scenario.ActAsHost();
        var itemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, itemId, 2));
        await DrainOutboxAsync();

        // Add items as guest
        await scenario.ActAsGuest("Guest User");
        await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, itemId, 1));
        await DrainOutboxAsync();

        // Act: Get real-time view model as guest
        var result = await SendAsync(new GetTeamCartRealTimeViewModelQuery(scenario.TeamCartId));

        // Assert: Should show updated state with both members and items
        result.IsSuccess.Should().BeTrue();
        var vm = result.Value.TeamCart;

        vm.Members.Should().HaveCount(2);
        vm.Members.Should().Contain(m => m.UserId == scenario.HostUserId && m.Name == "Host User" && m.Role == "Host");
        vm.Members.Should().Contain(m => m.UserId == scenario.GetGuestUserId("Guest User") && m.Name == "Guest User" && m.Role == "Guest");

        vm.Items.Should().HaveCount(2);
        vm.Items.Should().Contain(i => i.AddedByUserId == scenario.HostUserId && i.Quantity == 2);
        vm.Items.Should().Contain(i => i.AddedByUserId == scenario.GetGuestUserId("Guest User") && i.Quantity == 1);

        vm.Subtotal.Should().BeGreaterThan(0);
        vm.Total.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task GetRealTimeViewModel_WithPaymentFlow_ShouldReflectPaymentStatus()
    {
        // Arrange: Create cart scenario with host and guest
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .WithGuest("Guest User")
            .BuildAsync();
        await DrainOutboxAsync();

        // Add items as host
        await scenario.ActAsHost();
        var itemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, itemId, 2));
        await DrainOutboxAsync();

        // Lock cart as host
        await SendAsync(new LockTeamCartForPaymentCommand(scenario.TeamCartId));
        await DrainOutboxAsync();

        // Apply tip
        await SendAsync(new ApplyTipToTeamCartCommand(scenario.TeamCartId, 5.00m));
        await DrainOutboxAsync();

        // Commit COD payment as host
        await SendAsync(new CommitToCodPaymentCommand(scenario.TeamCartId));
        await DrainOutboxAsync();

        // Act: Get real-time view model
        var result = await SendAsync(new GetTeamCartRealTimeViewModelQuery(scenario.TeamCartId));

        // Assert: Should reflect locked status and payment information
        result.IsSuccess.Should().BeTrue();
        var vm = result.Value.TeamCart;

        vm.Status.Should().BeOneOf(TeamCartStatus.Locked, TeamCartStatus.ReadyToConfirm);
        vm.TipAmount.Should().Be(5.00m);
        vm.Total.Should().BeGreaterThan(vm.Subtotal); // Should include tip

        // At least the host should have payment status updated
        vm.Members.Should().Contain(m => m.UserId == scenario.HostUserId && m.PaymentStatus != "Pending");
    }

    [Test]
    public async Task GetRealTimeViewModel_ForNonMember_ShouldReturnNotMember()
    {
        // Arrange: Create team cart scenario
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .BuildAsync();
        await DrainOutboxAsync();

        // Switch to a different user (non-member)
        var nonMemberUserId = await CreateUserAsync("nonmember@example.com", "Password123!");
        SetUserId(nonMemberUserId);

        // Act & Assert: Should fail with NotMember error
        var result = await SendAsync(new GetTeamCartRealTimeViewModelQuery(scenario.TeamCartId));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("GetTeamCartRealTimeViewModel.NotMember");
    }

    [Test]
    public async Task GetRealTimeViewModel_ForNonExistentCart_ShouldReturnNotFound()
    {
        // Arrange: Authenticate user
        await RunAsDefaultUserAsync();
        var nonExistentCartId = Guid.NewGuid();

        // Act & Assert: Should fail with NotFound error
        var result = await SendAsync(new GetTeamCartRealTimeViewModelQuery(nonExistentCartId));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("GetTeamCartRealTimeViewModel.NotFound");
    }

    [Test]
    public async Task GetRealTimeViewModel_WithoutAuthentication_ShouldThrowUnauthorized()
    {
        // Arrange: Create team cart scenario
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .BuildAsync();
        await DrainOutboxAsync();

        // Remove authentication
        SetUserId(null);

        // Act & Assert: Should throw UnauthorizedAccessException
        var act = async () => await SendAsync(new GetTeamCartRealTimeViewModelQuery(scenario.TeamCartId));
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Test]
    public async Task GetRealTimeViewModel_WithEmptyGuid_ShouldThrowValidationException()
    {
        // Arrange: Authenticate user
        await RunAsDefaultUserAsync();

        // Act & Assert: Should throw ValidationException for empty GUID
        var act = async () => await SendAsync(new GetTeamCartRealTimeViewModelQuery(Guid.Empty));
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task GetRealTimeViewModel_ComparedToSqlQuery_ShouldHaveConsistentBasicData()
    {
        // Arrange: Create cart scenario with complexity
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .WithGuest("Guest User")
            .BuildAsync();
        await DrainOutboxAsync();

        // Add items as host
        await scenario.ActAsHost();
        var itemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, itemId, 1));
        await DrainOutboxAsync();

        // Act: Get both SQL and Redis views
        var sqlResult = await SendAsync(new GetTeamCartDetailsQuery(scenario.TeamCartId));
        var redisResult = await SendAsync(new GetTeamCartRealTimeViewModelQuery(scenario.TeamCartId));

        // Assert: Basic data should be consistent
        sqlResult.IsSuccess.Should().BeTrue();
        redisResult.IsSuccess.Should().BeTrue();

        var sqlDto = sqlResult.Value.TeamCart;
        var redisVm = redisResult.Value.TeamCart;

        redisVm.CartId.Value.Should().Be(sqlDto.TeamCartId);
        redisVm.RestaurantId.Should().Be(sqlDto.RestaurantId);
        redisVm.Status.Should().Be(sqlDto.Status);
        redisVm.Members.Should().HaveCount(sqlDto.Members.Count);
        redisVm.Items.Should().HaveCount(sqlDto.Items.Count);

        // Member data consistency
        foreach (var sqlMember in sqlDto.Members)
        {
            redisVm.Members.Should().Contain(m =>
                m.UserId == sqlMember.UserId &&
                m.Name == sqlMember.Name &&
                m.Role == sqlMember.Role);
        }

        // Item data consistency  
        foreach (var sqlItem in sqlDto.Items)
        {
            redisVm.Items.Should().Contain(i =>
                i.AddedByUserId == sqlItem.AddedByUserId &&
                i.Quantity == sqlItem.Quantity);
        }
    }
}
