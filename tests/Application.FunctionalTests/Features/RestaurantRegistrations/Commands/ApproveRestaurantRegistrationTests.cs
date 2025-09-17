using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Menus.Commands.CreateMenu;
using YummyZoom.Application.RestaurantRegistrations.Commands.ApproveRestaurantRegistration;
using YummyZoom.Application.RestaurantRegistrations.Commands.SubmitRestaurantRegistration;
using YummyZoom.Domain.RestaurantAggregate;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantRegistrationAggregate;
using YummyZoom.Domain.RestaurantRegistrationAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;
using YummyZoom.Application.FunctionalTests.Features.RestaurantRegistrations.Utilities;
using YummyZoom.Domain.RestaurantRegistrationAggregate.Errors;

namespace YummyZoom.Application.FunctionalTests.Features.RestaurantRegistrations.Commands;

public class ApproveRestaurantRegistrationTests : BaseTestFixture
{
    [Test]
    public async Task ApproveRegistration_ShouldCreateVerifiedRestaurant_AndAssignOwner()
    {
        // Arrange submitter and submit a registration
        var submitterId = await RunAsDefaultUserAsync();
        var submitCmd = RestaurantRegistrationTestHelper.BuildValidSubmitCommand(UserId.Create(submitterId));
        var submitResult = await SendAsync(submitCmd);
        submitResult.ShouldBeSuccessful();
        var registrationId = submitResult.Value.RegistrationId;

        // Act as admin and approve
        await RunAsAdministratorAsync();
        var approve = new ApproveRestaurantRegistrationCommand(registrationId, "looks good");
        var approveResult = await SendAsync(approve);
        approveResult.ShouldBeSuccessful();
        var restaurantId = approveResult.Value.RestaurantId;

        // Drain outbox to flush any projection handlers (search, etc.)
        await DrainOutboxAsync();

        // Assert registration state
        var reg = await FindAsync<RestaurantRegistration>(RestaurantRegistrationId.Create(registrationId));
        reg!.Status.ToString().Should().Be("Approved");

        // Assert restaurant created and verified
        var restaurant = await FindAsync<Restaurant>(RestaurantId.Create(restaurantId));
        restaurant.Should().NotBeNull();
        restaurant!.IsVerified.Should().BeTrue();

        // Switch back to submitter, refresh claims, and assert owner permission by creating a menu
        SetUserId(submitterId);
        await RefreshUserClaimsAsync();

        var createMenu = new CreateMenuCommand(
            RestaurantId: restaurantId,
            Name: "Main",
            Description: "Main menu");

        var createMenuResult = await SendAsync(createMenu);
        createMenuResult.ShouldBeSuccessful();
    }

    [Test]
    public async Task ApproveRegistration_WhenNotAdmin_ShouldThrowForbidden()
    {
        // Arrange: submitter submits
        var submitterId = await RunAsDefaultUserAsync();
        var submitCmd = RestaurantRegistrationTestHelper.BuildValidSubmitCommand(UserId.Create(submitterId));
        var submitResult = await SendAsync(submitCmd);
        var registrationId = submitResult.Value.RegistrationId;

        // Act & Assert: try approve as the same user
        await FluentActions.Invoking(() => SendAsync(new ApproveRestaurantRegistrationCommand(registrationId, null)))
            .Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task ApproveRegistration_WhenNotFound_ShouldReturnNotFound()
    {
        // Arrange
        await RunAsAdministratorAsync();
        var randomId = Guid.NewGuid();

        // Act
        var result = await SendAsync(new ApproveRestaurantRegistrationCommand(randomId, null));

        // Assert
        result.ShouldBeFailure(RestaurantRegistrationErrors.NotFound(randomId).Code);
    }

    [Test]
    public async Task ApproveRegistration_WhenAlreadyRejected_ShouldFailNotPending()
    {
        // Arrange: submit and reject first
        var submitterId = await RunAsDefaultUserAsync();
        var submitCmd = RestaurantRegistrationTestHelper.BuildValidSubmitCommand(UserId.Create(submitterId));
        var submitResult = await SendAsync(submitCmd);
        var registrationId = submitResult.Value.RegistrationId;

        await RunAsAdministratorAsync();
        await SendAsync(new YummyZoom.Application.RestaurantRegistrations.Commands.RejectRestaurantRegistration.RejectRestaurantRegistrationCommand(registrationId, "incomplete"));

        // Act
        var approveResult = await SendAsync(new ApproveRestaurantRegistrationCommand(registrationId, null));

        // Assert
        approveResult.ShouldBeFailure(RestaurantRegistrationErrors.AlreadyRejected.Code);
    }
}
