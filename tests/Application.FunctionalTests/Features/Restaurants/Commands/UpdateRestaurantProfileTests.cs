using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Restaurants.Commands.UpdateRestaurantProfile;
using YummyZoom.Domain.RestaurantAggregate;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Restaurants.Commands;

public class UpdateRestaurantProfileTests : BaseTestFixture
{
    [Test]
    public async Task UpdateProfile_NameAndDescription_Succeeds()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var cmd = new UpdateRestaurantProfileCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Name: "New Name",
            Description: "New Desc",
            LogoUrl: null,
            Phone: null,
            Email: null);

        var result = await SendAsync(cmd);
        result.ShouldBeSuccessful();

        var agg = await FindAsync<Restaurant>(RestaurantId.Create(Testing.TestData.DefaultRestaurantId));
        agg!.Name.Should().Be("New Name");
        agg.Description.Should().Be("New Desc");
    }

    [Test]
    public async Task UpdateProfile_Logo_Succeeds()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var cmd = new UpdateRestaurantProfileCommand(Testing.TestData.DefaultRestaurantId, null, null, "https://img.example.com/logo.png", null, null);
        var result = await SendAsync(cmd);
        result.ShouldBeSuccessful();
    }

    [Test]
    public async Task UpdateProfile_Contact_Succeeds()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var cmd = new UpdateRestaurantProfileCommand(Testing.TestData.DefaultRestaurantId, null, null, null, "+1-555-0000", "c@ex.com");
        var result = await SendAsync(cmd);
        result.ShouldBeSuccessful();
    }

    [Test]
    public async Task UpdateProfile_NoFields_ThrowsValidation()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var cmd = new UpdateRestaurantProfileCommand(Testing.TestData.DefaultRestaurantId, null, null, null, null, null);
        await FluentActions.Invoking(() => SendAsync(cmd)).Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task UpdateProfile_NotFound_ReturnsError()
    {
        var unknown = Guid.NewGuid();
        await RunAsRestaurantStaffAsync("staff@restaurant.com", unknown);
        var cmd = new UpdateRestaurantProfileCommand(unknown, "X", null, null, null, null);
        var result = await SendAsync(cmd);
        result.ShouldBeFailure("Restaurant.NotFound");
    }

    [Test]
    public async Task UpdateProfile_Forbidden_WhenNotStaff()
    {
        await RunAsDefaultUserAsync();
        var cmd = new UpdateRestaurantProfileCommand(Testing.TestData.DefaultRestaurantId, "X", null, null, null, null);
        await FluentActions.Invoking(() => SendAsync(cmd)).Should().ThrowAsync<ForbiddenAccessException>();
    }
}
