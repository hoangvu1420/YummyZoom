using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Orders.Commands.InitiateOrder;
using YummyZoom.Infrastructure.Persistence.EfCore;
using YummyZoom.Infrastructure.Persistence.EfCore.Models;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Orders.Commands.InitiateOrder;

public class InitiateOrderCouponOutboxTests : InitiateOrderTestBase
{
    [Test]
    public async Task InitiateOrder_WithCoupon_Should_EnqueueAndProcess_CouponUsed_OutboxEvent()
    {
        // Arrange
        using (var preScope = CreateScope())
        {
            var preDb = preScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await preDb.Database.ExecuteSqlRawAsync("DELETE FROM \"OutboxMessages\";");
        }

        var command = InitiateOrderTestHelper.BuildValidCommandWithCoupon(Testing.TestData.DefaultCouponCode);

        // Act
        var result = await SendAsync(command);
        result.ShouldBeSuccessful();

        // Drain outbox to ensure processing occurs
        await DrainOutboxAsync();

        // Assert: Outbox contains processed CouponUsed message
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var couponUsedOutbox = await db.Set<OutboxMessage>()
            .Where(m => m.Type.Contains("CouponUsed") && m.ProcessedOnUtc != null && m.Error == null)
            .ToListAsync();

        couponUsedOutbox.Should().NotBeEmpty();
    }
}
