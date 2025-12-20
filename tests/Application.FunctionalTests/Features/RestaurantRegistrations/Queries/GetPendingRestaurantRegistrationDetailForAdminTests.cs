using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Features.RestaurantRegistrations.Utilities;
using YummyZoom.Application.RestaurantRegistrations.Commands.RejectRestaurantRegistration;
using YummyZoom.Application.RestaurantRegistrations.Queries.GetPendingRestaurantRegistrationDetailForAdmin;
using YummyZoom.Domain.RestaurantRegistrationAggregate.Errors;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.RestaurantRegistrations.Queries;

public class GetPendingRestaurantRegistrationDetailForAdminTests : BaseTestFixture
{
    [Test]
    public async Task GetPendingDetail_ShouldReturnFullDetails_WhenPending()
    {
        var submitterId = await RunAsDefaultUserAsync();
        var submit = RestaurantRegistrationTestHelper.BuildValidSubmitCommand(UserId.Create(submitterId), name: "Pasta Palace");
        var submitResult = await SendAsync(submit);
        var registrationId = submitResult.Value.RegistrationId;

        await RunAsAdministratorAsync();
        var result = await SendAsync(new GetPendingRestaurantRegistrationDetailForAdminQuery(registrationId));

        result.ShouldBeSuccessful();
        result.Value.RegistrationId.Should().Be(registrationId);
        result.Value.Name.Should().Be("Pasta Palace");
        result.Value.Description.Should().Be(submit.Description);
        result.Value.CuisineType.Should().Be(submit.CuisineType);
        result.Value.City.Should().Be(submit.City);
        result.Value.Status.Should().Be("Pending");
        result.Value.SubmitterUserId.Should().Be(submitterId);
        result.Value.SubmitterName.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task GetPendingDetail_WhenNotAdmin_ShouldThrowForbidden()
    {
        await RunAsDefaultUserAsync();
        await FluentActions.Invoking(() => SendAsync(new GetPendingRestaurantRegistrationDetailForAdminQuery(Guid.NewGuid())))
            .Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task GetPendingDetail_WhenNotPending_ShouldReturnConflict()
    {
        var submitterId = await RunAsDefaultUserAsync();
        var submit = RestaurantRegistrationTestHelper.BuildValidSubmitCommand(UserId.Create(submitterId));
        var submitResult = await SendAsync(submit);

        await RunAsAdministratorAsync();
        var rejectResult = await SendAsync(new RejectRestaurantRegistrationCommand(submitResult.Value.RegistrationId, "missing docs"));
        rejectResult.ShouldBeSuccessful();

        var result = await SendAsync(new GetPendingRestaurantRegistrationDetailForAdminQuery(submitResult.Value.RegistrationId));
        result.ShouldBeFailure(RestaurantRegistrationErrors.NotPending.Code);
    }
}
