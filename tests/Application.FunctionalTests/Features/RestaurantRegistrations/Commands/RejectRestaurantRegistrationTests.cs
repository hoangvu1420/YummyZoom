using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.RestaurantRegistrations.Commands.RejectRestaurantRegistration;
using YummyZoom.Application.RestaurantRegistrations.Commands.SubmitRestaurantRegistration;
using YummyZoom.Domain.RestaurantAggregate;
using YummyZoom.Domain.RestaurantRegistrationAggregate;
using YummyZoom.Domain.RestaurantRegistrationAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;
using YummyZoom.Application.FunctionalTests.Features.RestaurantRegistrations.Utilities;

namespace YummyZoom.Application.FunctionalTests.Features.RestaurantRegistrations.Commands;

public class RejectRestaurantRegistrationTests : BaseTestFixture
{
    [Test]
    public async Task RejectRegistration_ShouldMarkRejected_AndNotCreateRestaurant()
    {
        // Arrange: submit
        var submitterId = await RunAsDefaultUserAsync();
        var submit = RestaurantRegistrationTestHelper.BuildValidSubmitCommand(UserId.Create(submitterId));
        var submitResult = await SendAsync(submit);
        var registrationId = submitResult.Value.RegistrationId;

        var beforeCount = await CountAsync<Restaurant>();

        // Act: reject as admin
        await RunAsAdministratorAsync();
        var rejectResult = await SendAsync(new RejectRestaurantRegistrationCommand(registrationId, "insufficient details"));
        rejectResult.ShouldBeSuccessful();

        // Assert registration state
        var reg = await FindAsync<RestaurantRegistration>(RestaurantRegistrationId.Create(registrationId));
        reg!.Status.ToString().Should().Be("Rejected");
        reg.ReviewNote.Should().Be("insufficient details");

        // Assert no restaurant created
        var afterCount = await CountAsync<Restaurant>();
        afterCount.Should().Be(beforeCount);
    }

    [Test]
    public async Task RejectRegistration_WhenNotAdmin_ShouldThrowForbidden()
    {
        var submitterId = await RunAsDefaultUserAsync();
        var submit = RestaurantRegistrationTestHelper.BuildValidSubmitCommand(UserId.Create(submitterId));
        var submitResult = await SendAsync(submit);
        var registrationId = submitResult.Value.RegistrationId;

        await FluentActions.Invoking(() => SendAsync(new RejectRestaurantRegistrationCommand(registrationId, "nope")))
            .Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task RejectRegistration_WithoutReason_ShouldThrowValidation()
    {
        await RunAsAdministratorAsync();
        var act = () => SendAsync(new RejectRestaurantRegistrationCommand(Guid.NewGuid(), ""));
        await act.Should().ThrowAsync<ValidationException>();
    }
}

