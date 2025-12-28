using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Authorization;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.ApplyTipToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.CommitToCodPayment;
using YummyZoom.Application.TeamCarts.Commands.FinalizePricing;
using YummyZoom.Application.TeamCarts.Commands.LockTeamCartForPayment;
using YummyZoom.Application.TeamCarts.Queries.GetTeamCartDetails;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.Queries;

[TestFixture]
public class GetTeamCartDetailsQueryTests : BaseTestFixture
{
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await TeamCartRoleTestHelper.SetupTeamCartAuthorizationTestsAsync();
    }

    [Test]
    public async Task GetTeamCartDetails_WithValidCartAndMember_ShouldReturnDetailsWithMembersAndItems()
    {
        // Arrange: Create team cart scenario with host and guest
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Alice Host")
            .WithGuest("Bob Guest")
            .BuildAsync();

        // Add items
        await scenario.ActAsHost();
        var itemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, itemId, 2));

        await scenario.ActAsGuest("Bob Guest");
        await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, itemId, 1));

        // Lock cart and add tip as host
        await scenario.ActAsHost();
        await SendAsync(new LockTeamCartForPaymentCommand(scenario.TeamCartId));
        await SendAsync(new ApplyTipToTeamCartCommand(scenario.TeamCartId, 5.00m));

        // Act: Query as host (who is a member)
        var result = await SendAsync(new GetTeamCartDetailsQuery(scenario.TeamCartId));

        // Assert
        result.ShouldBeSuccessful();
        var teamCart = result.Value.TeamCart;

        teamCart.TeamCartId.Should().Be(scenario.TeamCartId);
        teamCart.RestaurantId.Should().Be(Testing.TestData.DefaultRestaurantId);
        teamCart.HostUserId.Should().Be(scenario.HostUserId);
        teamCart.Status.Should().Be(TeamCartStatus.Locked);
        teamCart.ShareTokenMasked.Should().NotBeNullOrEmpty();
        teamCart.ShareTokenMasked.Should().StartWith("***");
        teamCart.TipAmount.Should().Be(5.00m);
        teamCart.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        teamCart.ExpiresAt.Should().BeAfter(DateTime.UtcNow);

        // Verify members
        teamCart.Members.Should().HaveCount(2);
        teamCart.Members.Should().Contain(m => m.UserId == scenario.HostUserId && m.Name == "Alice Host" && m.Role == "Host");
        teamCart.Members.Should().Contain(m => m.UserId == scenario.GetGuestUserId("Bob Guest") && m.Name == "Bob Guest" && m.Role == "Guest");

        // Verify items
        teamCart.Items.Should().HaveCount(2);
        teamCart.Items.Should().Contain(i => i.AddedByUserId == scenario.HostUserId && i.Quantity == 2);
        teamCart.Items.Should().Contain(i => i.AddedByUserId == scenario.GetGuestUserId("Bob Guest") && i.Quantity == 1);

        // Verify financial calculations
        var expectedSubtotal = teamCart.Items.Sum(i => i.BasePriceAmount * i.Quantity);
        teamCart.Subtotal.Should().Be(expectedSubtotal);
        teamCart.Total.Should().Be(expectedSubtotal + 5.00m); // Subtotal + tip
    }

    [Test]
    public async Task GetTeamCartDetails_WithPayments_ShouldIncludePaymentDetails()
    {
        // Arrange: Create team cart scenario and add payment
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .BuildAsync();

        // Add items, lock cart, and commit payment as host
        await scenario.ActAsHost();
        var itemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, itemId, 1));
        await SendAsync(new LockTeamCartForPaymentCommand(scenario.TeamCartId));
        await SendAsync(new FinalizePricingCommand(scenario.TeamCartId));
        await SendAsync(new CommitToCodPaymentCommand(scenario.TeamCartId));

        // Act: Query as host (who is a member)
        var result = await SendAsync(new GetTeamCartDetailsQuery(scenario.TeamCartId));

        // Assert
        result.ShouldBeSuccessful();
        var teamCart = result.Value.TeamCart;

        teamCart.Status.Should().BeOneOf(TeamCartStatus.Finalized, TeamCartStatus.ReadyToConfirm);
        teamCart.MemberPayments.Should().ContainSingle();

        var payment = teamCart.MemberPayments.First();
        payment.UserId.Should().Be(scenario.HostUserId);
        payment.PaymentMethod.Should().Be("CashOnDelivery");
        payment.PaymentStatus.Should().Be("CommittedToCOD");
        payment.Amount.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task GetTeamCartDetails_AsGuestMember_ShouldSucceed()
    {
        // Arrange: Create team cart scenario with host and guest
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .WithGuest("Guest User")
            .BuildAsync();

        // Act: Query as guest (who is a member)
        await scenario.ActAsGuest("Guest User");
        var result = await SendAsync(new GetTeamCartDetailsQuery(scenario.TeamCartId));

        // Assert: Guest should be able to see details
        result.ShouldBeSuccessful();
        result.Value.TeamCart.TeamCartId.Should().Be(scenario.TeamCartId);
        result.Value.TeamCart.Members.Should().Contain(m => m.UserId == scenario.GetGuestUserId("Guest User"));
    }

    [Test]
    public async Task GetTeamCartDetails_WithNonExistentCart_ShouldReturnNotFound()
    {
        // Arrange: Use a non-existent TeamCart ID with authenticated user
        await RunAsDefaultUserAsync();
        var nonExistentTeamCartId = Guid.NewGuid();

        // Act
        var result = await SendAsync(new GetTeamCartDetailsQuery(nonExistentTeamCartId));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("GetTeamCartDetails.NotFound");
    }

    [Test]
    public async Task GetTeamCartDetails_AsNonMember_ShouldReturnNotMember()
    {
        // Arrange: Create team cart with host
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .BuildAsync();

        // Switch to a different user who is not a member
        var nonMemberUserId = await CreateUserAsync("nonmember@example.com", "Password123!");
        SetUserId(nonMemberUserId);

        // Act
        var result = await SendAsync(new GetTeamCartDetailsQuery(scenario.TeamCartId));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("GetTeamCartDetails.NotMember");
    }

    [Test]
    public async Task GetTeamCartDetails_WithEmptyGuid_ShouldThrowValidationException()
    {
        await RunAsDefaultUserAsync();

        var act = async () => await SendAsync(new GetTeamCartDetailsQuery(Guid.Empty));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task GetTeamCartDetails_WithoutAuthentication_ShouldThrowUnauthorizedException()
    {
        var teamCartId = Guid.NewGuid();

        var act = async () => await SendAsync(new GetTeamCartDetailsQuery(teamCartId));

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Test]
    public async Task GetTeamCartDetails_WithComplexScenario_ShouldReturnAccurateData()
    {
        // Arrange: Complex scenario with multiple members, items, and financial operations
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Alice Host")
            .WithGuest("Bob Guest1")
            .WithGuest("Charlie Guest2")
            .BuildAsync();

        // Add various items using the actual member users
        var burger = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        var pizza = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.MargheritaPizza);

        await scenario.ActAsHost();
        await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burger, 2)); // Host adds burgers

        await scenario.ActAsGuest("Bob Guest1");
        await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, pizza, 1)); // Guest1 adds pizza

        await scenario.ActAsGuest("Charlie Guest2");
        await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burger, 1)); // Guest2 adds burger

        // Lock cart and apply tip as host  
        await scenario.ActAsHost();
        await SendAsync(new LockTeamCartForPaymentCommand(scenario.TeamCartId));
        await SendAsync(new ApplyTipToTeamCartCommand(scenario.TeamCartId, 10.00m));

        // Act: Query as host (who is a member)
        var result = await SendAsync(new GetTeamCartDetailsQuery(scenario.TeamCartId));

        // Assert detailed validation
        result.ShouldBeSuccessful();
        var teamCart = result.Value.TeamCart;

        // Verify structure
        teamCart.Members.Should().HaveCount(3);
        teamCart.Items.Should().HaveCount(3);
        teamCart.TipAmount.Should().Be(10.00m);

        // Verify host role
        teamCart.Members.Should().ContainSingle(m => m.Role == "Host" && m.UserId == scenario.HostUserId);

        // Verify guests
        teamCart.Members.Count(m => m.Role == "Guest").Should().Be(2);

        // Verify items belong to correct users
        teamCart.Items.Count(i => i.AddedByUserId == scenario.HostUserId).Should().Be(1);
        teamCart.Items.Count(i => i.AddedByUserId == scenario.GetGuestUserId("Bob Guest1")).Should().Be(1);
        teamCart.Items.Count(i => i.AddedByUserId == scenario.GetGuestUserId("Charlie Guest2")).Should().Be(1);

        // Verify total calculation includes tip
        var subtotal = teamCart.Items.Sum(i => i.BasePriceAmount * i.Quantity);
        teamCart.Subtotal.Should().Be(subtotal);
        teamCart.Total.Should().Be(subtotal + 10.00m);
    }
}
