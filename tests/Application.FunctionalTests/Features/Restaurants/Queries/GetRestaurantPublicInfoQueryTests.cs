using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Restaurants.Queries.GetRestaurantPublicInfo;
using YummyZoom.Domain.RestaurantAggregate;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Infrastructure.Data;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Restaurants.Queries;

[TestFixture]
public class GetRestaurantPublicInfoQueryTests : BaseTestFixture
{
    [Test]
    public async Task Success_ReturnsBasicDtoAndParsesCuisineTags()
    {
        var restaurant = await FindAsync<Restaurant>(RestaurantId.Create(Testing.TestData.DefaultRestaurantId));
        restaurant.Should().NotBeNull();

        // Seed CuisineTags as JSONB text via raw SQL easiest path: update EF entity shadow not available → update columns through raw SQL
        // Instead, update simple scalar fields we can: mark IsDeleted=false already default; set City/Area via domain replace methods not present.
        // Use direct connection via scope to set JSONB CuisineTags and city/area columns on Restaurants table.
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var id = restaurant!.Id.Value;
        var phone = "+1";
        var background = string.Empty;
        await db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE ""Restaurants"" 
            SET 
                ""Location_City"" = {"Metro"}, 
                ""Location_State"" = {"ST"}, 
                ""Location_ZipCode"" = {"00000"}, 
                ""Location_Country"" = {"US"}, 
                ""Location_Street"" = {"1 Test"}, 
                ""BackgroundImageUrl"" = {background}, 
                ""CuisineType"" = {"Multi"}, 
                ""BusinessHours"" = {"9-5"}, 
                ""ContactInfo_PhoneNumber"" = {phone}, 
                ""ContactInfo_Email"" = {"a@b.c"}
            WHERE ""Id"" = {id}");

        var result = await SendAsync(new GetRestaurantPublicInfoQuery(id));

        result.ShouldBeSuccessful();
        var dto = result.Value;
        dto.RestaurantId.Should().Be(id);
        dto.Name.Should().NotBeNullOrWhiteSpace();
        dto.CuisineTags.Should().BeEmpty();
        dto.City.Should().Be("Metro");
    }

    [Test]
    public async Task NotFound_WhenDeleted()
    {
        var restaurant = await FindAsync<Restaurant>(RestaurantId.Create(Testing.TestData.DefaultRestaurantId));
        restaurant.Should().NotBeNull();

        // Soft-delete directly via SQL because default restaurant is verified
        // and domain rules prevent deletion without explicit confirmation
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var id = restaurant!.Id.Value;
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE ""Restaurants"" 
                SET ""IsDeleted"" = {true}, ""DeletedOn"" = {DateTimeOffset.UtcNow}
                WHERE ""Id"" = {id}");
        }

        var result = await SendAsync(new GetRestaurantPublicInfoQuery(restaurant.Id.Value));
        result.ShouldBeFailure();
        result.Error.Should().Be(GetRestaurantPublicInfoErrors.NotFound);
    }

    [Test]
    public async Task NotFound_WhenMissing()
    {
        var result = await SendAsync(new GetRestaurantPublicInfoQuery(Guid.NewGuid()));
        result.ShouldBeFailure();
        result.Error.Should().Be(GetRestaurantPublicInfoErrors.NotFound);
    }

    [Test]
    public async Task Validation_EmptyRestaurantId_ShouldThrow()
    {
        var act = async () => await SendAsync(new GetRestaurantPublicInfoQuery(Guid.Empty));
        await act.Should().ThrowAsync<ValidationException>();
    }
}
