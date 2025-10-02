using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Features.RestaurantRegistrations.Utilities;
using YummyZoom.Application.RestaurantRegistrations.Commands.SubmitRestaurantRegistration;
using YummyZoom.Domain.RestaurantRegistrationAggregate;
using YummyZoom.Domain.RestaurantRegistrationAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.RestaurantRegistrations.Commands;

public class SubmitRestaurantRegistrationTests : BaseTestFixture
{
    [Test]
    public async Task SubmitRestaurantRegistration_WithValidData_ShouldSucceed()
    {
        // Arrange
        var submitterId = await RunAsDefaultUserAsync();
        var cmd = RestaurantRegistrationTestHelper.BuildValidSubmitCommand(UserId.Create(submitterId));

        // Act
        var result = await SendAsync(cmd);

        // Assert
        result.ShouldBeSuccessful();
        var reg = await FindAsync<RestaurantRegistration>(RestaurantRegistrationId.Create(result.Value.RegistrationId));
        reg.Should().NotBeNull();
        reg!.Status.ToString().Should().Be("Pending");
        reg.SubmitterUserId.Value.Should().Be(submitterId);
        reg.City.Should().Be("Seattle");
    }

    [Test]
    public async Task SubmitRestaurantRegistration_WhenUnauthenticated_ShouldThrowUnauthorized()
    {
        // Arrange
        SetUserId(null);
        var userId = Guid.NewGuid();
        var cmd = RestaurantRegistrationTestHelper.BuildValidSubmitCommand(UserId.Create(userId));

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(cmd))
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Test]
    public async Task SubmitRestaurantRegistration_WithInvalidData_ShouldThrowValidation()
    {
        // Arrange
        var submitterId = await RunAsDefaultUserAsync();
        var bad = new SubmitRestaurantRegistrationCommand(
            Name: "", // invalid
            Description: "",
            CuisineType: "",
            Street: "",
            City: "",
            State: "",
            ZipCode: "",
            Country: "",
            PhoneNumber: "",
            Email: "not-an-email",
            BusinessHours: "",
            LogoUrl: "htp://bad",
            Latitude: 200, // out of range
            Longitude: -181)
        { UserId = UserId.Create(submitterId) };

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(bad))
            .Should().ThrowAsync<ValidationException>();
    }
}
