using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Restaurants.Commands.UpdateRestaurantBusinessHours;
using YummyZoom.Domain.RestaurantAggregate;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Restaurants.Commands;

public class UpdateRestaurantBusinessHoursTests : BaseTestFixture
{
    [Test]
    public async Task UpdateBusinessHours_WhenValid_Succeeds_AndPersists()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var newHours = "09:00-17:00";
        var cmd = new UpdateRestaurantBusinessHoursCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            BusinessHours: newHours);

        var result = await SendAsync(cmd);
        result.ShouldBeSuccessful();

        var agg = await FindAsync<Restaurant>(RestaurantId.Create(Testing.TestData.DefaultRestaurantId));
        agg!.BusinessHours.Hours.Should().Be(newHours);
    }

    [Test]
    public async Task UpdateBusinessHours_ForNonExistentRestaurant_ReturnsNotFound()
    {
        var nonExistentRestaurantId = Guid.NewGuid();
        await RunAsRestaurantStaffAsync("staff@restaurant.com", nonExistentRestaurantId);

        var cmd = new UpdateRestaurantBusinessHoursCommand(
            RestaurantId: nonExistentRestaurantId,
            BusinessHours: "08:00-18:00");

        var result = await SendAsync(cmd);
        result.ShouldBeFailure("Restaurant.NotFound");
    }

    [Test]
    public async Task UpdateBusinessHours_WithoutAuthorization_ThrowsForbidden()
    {
        await RunAsDefaultUserAsync();

        var cmd = new UpdateRestaurantBusinessHoursCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            BusinessHours: "10:00-22:00");

        await FluentActions.Invoking(() => SendAsync(cmd))
            .Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task UpdateBusinessHours_WithInvalidData_ThrowsValidation()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var cmd = new UpdateRestaurantBusinessHoursCommand(
            RestaurantId: Guid.Empty,
            BusinessHours: "");

        await FluentActions.Invoking(() => SendAsync(cmd))
            .Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task UpdateBusinessHours_WithInvalidFormat_ThrowsValidation()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var cmd = new UpdateRestaurantBusinessHoursCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            BusinessHours: "9-5");

        await FluentActions.Invoking(() => SendAsync(cmd))
            .Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task UpdateBusinessHours_WithInvalidTimeFormat_ThrowsValidation()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var cmd = new UpdateRestaurantBusinessHoursCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            BusinessHours: "25:00-17:00");

        await FluentActions.Invoking(() => SendAsync(cmd))
            .Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task UpdateBusinessHours_WithEndBeforeStart_ThrowsValidation()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var cmd = new UpdateRestaurantBusinessHoursCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            BusinessHours: "17:00-09:00");

        await FluentActions.Invoking(() => SendAsync(cmd))
            .Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task UpdateBusinessHours_WithValidEdgeCases_Succeeds()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        // Test early morning hours
        var cmd1 = new UpdateRestaurantBusinessHoursCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            BusinessHours: "00:00-23:59");

        var result1 = await SendAsync(cmd1);
        result1.ShouldBeSuccessful();

        // Test late night hours
        var cmd2 = new UpdateRestaurantBusinessHoursCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            BusinessHours: "18:30-23:45");

        var result2 = await SendAsync(cmd2);
        result2.ShouldBeSuccessful();
    }
}

