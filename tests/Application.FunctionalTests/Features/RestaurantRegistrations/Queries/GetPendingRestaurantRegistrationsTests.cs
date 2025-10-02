using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Features.RestaurantRegistrations.Utilities;
using YummyZoom.Application.RestaurantRegistrations.Commands.SubmitRestaurantRegistration;
using YummyZoom.Application.RestaurantRegistrations.Queries.GetPendingRestaurantRegistrations;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.RestaurantRegistrations.Queries;

public class GetPendingRestaurantRegistrationsTests : BaseTestFixture
{
    [Test]
    public async Task GetPending_ShouldReturnOnlyPending_Paginated()
    {
        // Arrange: submit 3
        var userId = await RunAsDefaultUserAsync();
        await SendAsync(RestaurantRegistrationTestHelper.BuildValidSubmitCommand(UserId.Create(userId), name: "P1"));
        await SendAsync(RestaurantRegistrationTestHelper.BuildValidSubmitCommand(UserId.Create(userId), name: "P2"));
        await SendAsync(RestaurantRegistrationTestHelper.BuildValidSubmitCommand(UserId.Create(userId), name: "P3"));

        // Act as admin: page size 2
        await RunAsAdministratorAsync();
        var page1 = await SendAsync(new GetPendingRestaurantRegistrationsQuery(1, 2));
        var page2 = await SendAsync(new GetPendingRestaurantRegistrationsQuery(2, 2));

        // Assert
        page1.ShouldBeSuccessful();
        page2.ShouldBeSuccessful();
        page1.Value.Items.Should().HaveCount(2);
        page2.Value.Items.Should().HaveCount(1);
        page1.Value.TotalCount.Should().Be(3);
    }

    [Test]
    public async Task GetPending_WhenNotAdmin_ShouldThrowForbidden()
    {
        await RunAsDefaultUserAsync();
        await FluentActions.Invoking(() => SendAsync(new GetPendingRestaurantRegistrationsQuery(1, 10)))
            .Should().ThrowAsync<ForbiddenAccessException>();
    }
}

