using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.RestaurantRegistrations.Queries.GetMyRestaurantRegistrations;
using YummyZoom.Application.RestaurantRegistrations.Commands.SubmitRestaurantRegistration;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;
using YummyZoom.Application.FunctionalTests.Features.RestaurantRegistrations.Utilities;

namespace YummyZoom.Application.FunctionalTests.Features.RestaurantRegistrations.Queries;

public class GetMyRestaurantRegistrationsTests : BaseTestFixture
{
    [Test]
    public async Task GetMine_ShouldReturnOwnRegistrations_NewestFirst()
    {
        // Arrange: submit two as same user
        var userId = await RunAsDefaultUserAsync();
        var cmd1 = RestaurantRegistrationTestHelper.BuildValidSubmitCommand(UserId.Create(userId), name: "A-Resto");
        var cmd2 = RestaurantRegistrationTestHelper.BuildValidSubmitCommand(UserId.Create(userId), name: "B-Resto");
        await SendAsync(cmd1);
        await Task.Delay(10); // ensure distinct timestamps
        await SendAsync(cmd2);

        // Act
        var result = await SendAsync(new GetMyRestaurantRegistrationsQuery());

        // Assert
        result.ShouldBeSuccessful();
        result.Value.Should().HaveCount(2);
        result.Value.First().Name.Should().Be("B-Resto");
        result.Value.Last().Name.Should().Be("A-Resto");
    }

    [Test]
    public async Task GetMine_WhenUnauthenticated_ShouldThrowUnauthorized()
    {
        SetUserId(null);
        await FluentActions.Invoking(() => SendAsync(new GetMyRestaurantRegistrationsQuery()))
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
