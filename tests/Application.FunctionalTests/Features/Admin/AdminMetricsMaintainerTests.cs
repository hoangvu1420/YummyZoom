using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.Common.Caching;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Features.Orders.Commands.Lifecycle;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using YummyZoom.Domain.AccountTransactionEntity;
using YummyZoom.Domain.AccountTransactionEntity.Enums;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAccountAggregate;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.SupportTicketAggregate;
using YummyZoom.Domain.SupportTicketAggregate.Enums;
using YummyZoom.Domain.SupportTicketAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate;
using YummyZoom.Infrastructure.Persistence.ReadModels.Admin;
using YummyZoom.Infrastructure.Persistence.ReadModels.Reviews;
using static YummyZoom.Application.FunctionalTests.Testing;
using TestDataFactory = YummyZoom.Application.FunctionalTests.TestData.TestDataFactory;

namespace YummyZoom.Application.FunctionalTests.Features.Admin;

[TestFixture]
public class AdminMetricsMaintainerTests : BaseTestFixture
{
    private RecordingInvalidationPublisher _publisher = null!;

    [SetUp]
    public void AdminMetricsSetUp()
    {
        _publisher = new RecordingInvalidationPublisher();
        ReplaceService<ICacheInvalidationPublisher>(_publisher);
    }

    [TearDown]
    public async Task AdminMetricsTearDown()
    {
        await ResetServiceReplacements();
    }

    [Test]
    public async Task RecomputeAllAsync_WithLiveData_ShouldRefreshPlatformSnapshot()
    {
        // Arrange
        var deliveredOrderId = await OrderLifecycleTestHelper.CreateReadyOrderAsync();
        (await OrderLifecycleTestHelper.MarkDeliveredAsync(deliveredOrderId, DateTime.UtcNow.AddMinutes(-5))).IsSuccess.Should().BeTrue();

        var preparingOrderId = await OrderLifecycleTestHelper.CreatePreparingOrderAsync();
        await OrderLifecycleTestHelper.CreatePlacedOrderAsync();

        await SeedRefundAsync(deliveredOrderId, 12.34m);
        await SeedSupportTicketAsync(preparingOrderId);
        await UpsertReviewSummaryAsync(Testing.TestData.DefaultRestaurantId, averageRating: 4.2, totalReviews: 9);

        await DrainOutboxAsync();

        var expected = await ComputePlatformMetricsAsync();
        expected.LastOrderAtUtc.Should().NotBeNull();

        // Act
        var maintainer = GetService<IAdminMetricsMaintainer>();
        await maintainer.RecomputeAllAsync(dailySeriesWindowDays: 7);

        // Assert
        var snapshot = await GetPlatformSnapshotAsync();
        snapshot.Should().NotBeNull();
        snapshot!.TotalOrders.Should().Be(expected.TotalOrders);
        snapshot.ActiveOrders.Should().Be(expected.ActiveOrders);
        snapshot.DeliveredOrders.Should().Be(expected.DeliveredOrders);
        snapshot.GrossMerchandiseVolume.Should().Be(expected.GrossMerchandiseVolume);
        snapshot.TotalRefunds.Should().Be(expected.TotalRefunds);
        snapshot.ActiveRestaurants.Should().Be(expected.ActiveRestaurants);
        snapshot.ActiveCustomers.Should().Be(expected.ActiveCustomers);
        snapshot.OpenSupportTickets.Should().Be(expected.OpenSupportTickets);
        snapshot.TotalReviews.Should().Be(expected.TotalReviews);
        snapshot.LastOrderAtUtc.Should().BeCloseTo(expected.LastOrderAtUtc!.Value, TimeSpan.FromSeconds(1));
        snapshot.UpdatedAtUtc.Should().BeAfter(DateTime.UtcNow.AddMinutes(-5));

        _publisher.Messages.Should().ContainSingle();
        var message = _publisher.Messages.Single();
        message.Tags.Should().NotBeNull();
        message.Tags!.Should().Contain("cache:admin:platform-metrics");
        message.SourceEvent.Should().Be("AdminMetricsMaintainer");
    }

    [Test]
    public async Task RecomputeAllAsync_ShouldRebuildDailySeriesWithinWindow()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var day0 = now.Date;
        var day1 = day0.AddDays(-1);
        var day2 = day0.AddDays(-2);
        var outside = day0.AddDays(-5);

        await CreateDeliveredOrderAtAsync(day0.AddHours(11), totalOverride: 22m);
        await CreateDeliveredOrderAtAsync(day1.AddHours(9), totalOverride: 30m);
        await CreateDeliveredOrderAtAsync(day1.AddHours(18), totalOverride: 18m);
        await CreateDeliveredOrderAtAsync(day2.AddHours(13), totalOverride: 12m);
        await CreateDeliveredOrderAtAsync(outside.AddHours(10), totalOverride: 40m);

        await SeedRefundAsync(amount: 5m, occurredAt: day0.AddHours(12));
        await SeedRefundAsync(amount: 3m, occurredAt: day1.AddHours(15));

        await CreateUserCreatedAtAsync(day0.AddHours(8));
        await CreateUserCreatedAtAsync(day1.AddHours(8));
        await CreateUserCreatedAtAsync(outside.AddHours(8));

        await CreateRestaurantCreatedAtAsync(day2.AddHours(7));
        await CreateRestaurantCreatedAtAsync(outside.AddHours(7));

        await ClearDailySeriesAsync();

        // Act
        var maintainer = GetService<IAdminMetricsMaintainer>();
        await maintainer.RecomputeAllAsync(dailySeriesWindowDays: 3);

        // Assert
        var series = await GetDailySeriesAsync();
        series.Should().HaveCount(3);

        var bucket0 = series.Single(x => x.BucketDate == DateOnly.FromDateTime(day0));
        bucket0.TotalOrders.Should().Be(1);
        bucket0.DeliveredOrders.Should().Be(1);
        bucket0.GrossMerchandiseVolume.Should().Be(22m);
        bucket0.TotalRefunds.Should().Be(5m);
        bucket0.NewCustomers.Should().Be(3); // 1 test user + 2 seeded users (hoangnguyenvu1420@gmail.com, hoangnguyenvu1220@gmail.com)
        bucket0.NewRestaurants.Should().Be(1); // 1 seeded restaurant from TestDataFactory

        var bucket1 = series.Single(x => x.BucketDate == DateOnly.FromDateTime(day1));
        bucket1.TotalOrders.Should().Be(2);
        bucket1.DeliveredOrders.Should().Be(2);
        bucket1.GrossMerchandiseVolume.Should().Be(48m);
        bucket1.TotalRefunds.Should().Be(3m);
        bucket1.NewCustomers.Should().Be(1);
        bucket1.NewRestaurants.Should().Be(0);

        var bucket2 = series.Single(x => x.BucketDate == DateOnly.FromDateTime(day2));
        bucket2.TotalOrders.Should().Be(1);
        bucket2.DeliveredOrders.Should().Be(1);
        bucket2.GrossMerchandiseVolume.Should().Be(12m);
        bucket2.TotalRefunds.Should().Be(0m);
        bucket2.NewCustomers.Should().Be(0);
        bucket2.NewRestaurants.Should().Be(1);
    }

    [Test]
    public async Task RecomputeAllAsync_ShouldRefreshRestaurantHealthSummaries()
    {
        // Arrange
        var now = DateTime.UtcNow;
        await UpsertReviewSummaryAsync(Testing.TestData.DefaultRestaurantId, 4.6, 12);
        await UpdateRestaurantBalanceAsync(Testing.TestData.DefaultRestaurantId, 125.50m);

        var order7d = await CreateDeliveredOrderAtAsync(now.AddDays(-3), totalOverride: 35m);
        await SetOrderCouponAsync(order7d);

        await CreateDeliveredOrderAtAsync(now.AddDays(-10), totalOverride: 20m);
        await CreateDeliveredOrderAtAsync(now.AddDays(-40), totalOverride: 15m);

        // Act
        var maintainer = GetService<IAdminMetricsMaintainer>();
        await maintainer.RecomputeAllAsync(dailySeriesWindowDays: 14);

        // Assert
        var summary = await GetRestaurantHealthSummaryAsync(Testing.TestData.DefaultRestaurantId);
        summary.Should().NotBeNull();
        summary!.RestaurantId.Should().Be(Testing.TestData.DefaultRestaurantId);
        summary.OrdersLast7Days.Should().Be(1);
        summary.OrdersLast30Days.Should().Be(2);
        summary.RevenueLast30Days.Should().Be(55m);
        summary.CouponRedemptionsLast30Days.Should().Be(1);
        summary.AverageRating.Should().Be(4.6);
        summary.TotalReviews.Should().Be(12);
        summary.OutstandingBalance.Should().Be(125.50m);
        summary.LastOrderAtUtc.Should().NotBeNull();
        summary.LastOrderAtUtc!.Value.Should().BeCloseTo(now.AddDays(-3), TimeSpan.FromSeconds(1));
    }

    [Test]
    public async Task RecomputeAllAsync_ShouldBeIdempotent()
    {
        // Arrange
        await CreateDeliveredOrderAtAsync(DateTime.UtcNow.AddMinutes(-30));

        var maintainer = GetService<IAdminMetricsMaintainer>();
        await maintainer.RecomputeAllAsync(dailySeriesWindowDays: 2);

        var initialSnapshot = await GetPlatformSnapshotAsync();
        var initialSeries = await GetDailySeriesAsync();
        var initialSummary = await GetRestaurantHealthSummaryAsync(Testing.TestData.DefaultRestaurantId);

        await Task.Delay(50);

        // Act
        await maintainer.RecomputeAllAsync(dailySeriesWindowDays: 2);

        // Assert
        var nextSnapshot = await GetPlatformSnapshotAsync();
        nextSnapshot.Should().NotBeNull();
        nextSnapshot!.TotalOrders.Should().Be(initialSnapshot!.TotalOrders);
        nextSnapshot.ActiveOrders.Should().Be(initialSnapshot.ActiveOrders);
        nextSnapshot.DeliveredOrders.Should().Be(initialSnapshot.DeliveredOrders);
        nextSnapshot.GrossMerchandiseVolume.Should().Be(initialSnapshot.GrossMerchandiseVolume);
        nextSnapshot.UpdatedAtUtc.Should().BeOnOrAfter(initialSnapshot.UpdatedAtUtc);

        var nextSeries = await GetDailySeriesAsync();
        nextSeries.Should().HaveSameCount(initialSeries);
        nextSeries.Select(x => x.BucketDate).Should().BeEquivalentTo(initialSeries.Select(x => x.BucketDate));

        var nextSummary = await GetRestaurantHealthSummaryAsync(Testing.TestData.DefaultRestaurantId);
        nextSummary.Should().NotBeNull();
        nextSummary!.OrdersLast30Days.Should().Be(initialSummary!.OrdersLast30Days);
        nextSummary.UpdatedAtUtc.Should().BeOnOrAfter(initialSummary.UpdatedAtUtc);
    }

    [Test]
    public async Task RecomputeAllAsync_ShouldHandleCacheInvalidationPublisherFailures()
    {
        // Arrange
        _publisher.ThrowOnPublish = true;
        await CreateDeliveredOrderAtAsync(DateTime.UtcNow.AddMinutes(-10));
        var maintainer = GetService<IAdminMetricsMaintainer>();

        // Act
        Func<Task> act = async () => await maintainer.RecomputeAllAsync(dailySeriesWindowDays: 1);

        // Assert
        await act.Should().NotThrowAsync();
        _publisher.PublishAttempts.Should().Be(1);
        _publisher.Messages.Should().BeEmpty();

        _publisher.ThrowOnPublish = false;
        await maintainer.RecomputeAllAsync(dailySeriesWindowDays: 1);
        _publisher.Messages.Should().ContainSingle();
    }

    private static async Task<PlatformMetrics> ComputePlatformMetricsAsync()
    {
        return await TestDatabaseManager.ExecuteInScopeAsync(async db =>
        {
            var activeStatuses = new[]
            {
                OrderStatus.Placed,
                OrderStatus.Accepted,
                OrderStatus.Preparing,
                OrderStatus.ReadyForDelivery
            };

            var totalOrders = await db.Orders.CountAsync();
            var activeOrders = await db.Orders.CountAsync(o => activeStatuses.Contains(o.Status));
            var deliveredOrders = await db.Orders.CountAsync(o => o.Status == OrderStatus.Delivered);
            var gmv = await db.Orders
                .Where(o => o.Status == OrderStatus.Delivered)
                .SumAsync(o => o.TotalAmount.Amount);
            var refunds = await db.AccountTransactions
                .Where(a => a.Type == TransactionType.RefundDeduction)
                .SumAsync(a => -a.Amount.Amount);
            var activeRestaurants = await db.Restaurants.CountAsync(r => !r.IsDeleted && r.IsVerified);
            var activeCustomers = await db.DomainUsers.CountAsync(u => !u.IsDeleted && u.IsActive);
            var openSupportTickets = await db.SupportTickets.CountAsync(t => t.Status == SupportTicketStatus.Open || t.Status == SupportTicketStatus.InProgress || t.Status == SupportTicketStatus.PendingCustomerResponse);
            var totalReviews = await db.RestaurantReviewSummaries.SumAsync(r => r.TotalReviews);
            var lastOrderAt = await db.Orders.OrderByDescending(o => o.PlacementTimestamp).Select(o => (DateTime?)o.PlacementTimestamp).FirstOrDefaultAsync();

            return new PlatformMetrics(totalOrders, activeOrders, deliveredOrders, gmv, refunds, activeRestaurants, activeCustomers, openSupportTickets, totalReviews, lastOrderAt);
        });
    }

    private static async Task<AdminPlatformMetricsSnapshot?> GetPlatformSnapshotAsync()
    {
        return await TestDatabaseManager.ExecuteInScopeAsync(async db =>
            await db.AdminPlatformMetricsSnapshots.AsNoTracking().SingleOrDefaultAsync(x => x.SnapshotId == "platform"));
    }

    private static async Task<List<AdminDailyPerformanceSeries>> GetDailySeriesAsync()
    {
        return await TestDatabaseManager.ExecuteInScopeAsync(async db =>
            await db.AdminDailyPerformanceSeries.AsNoTracking().OrderBy(x => x.BucketDate).ToListAsync());
    }

    private static async Task<AdminRestaurantHealthSummary?> GetRestaurantHealthSummaryAsync(Guid restaurantId)
    {
        return await TestDatabaseManager.ExecuteInScopeAsync(async db =>
            await db.AdminRestaurantHealthSummaries.AsNoTracking().SingleOrDefaultAsync(x => x.RestaurantId == restaurantId));
    }

    private static async Task SeedSupportTicketAsync(OrderId relatedOrderId)
    {
        var linkResult = ContextLink.Create(ContextEntityType.Order, relatedOrderId.Value);
        linkResult.IsSuccess.Should().BeTrue();

        var ticketResult = SupportTicket.Create(
            subject: "Where is my order?",
            type: SupportTicketType.GeneralInquiry,
            priority: SupportTicketPriority.Normal,
            contextLinks: new[] { linkResult.Value },
            initialMessage: "Please assist",
            authorId: Testing.TestData.DefaultCustomerId,
            authorType: AuthorType.Customer,
            ticketSequenceNumber: 1234);
        ticketResult.IsSuccess.Should().BeTrue();

        await AddAsync(ticketResult.Value);
    }

    private static async Task SeedRefundAsync(OrderId orderId, decimal amount)
    {
        await SeedRefundAsync(amount, DateTime.UtcNow, orderId);
    }

    private static async Task SeedRefundAsync(decimal amount, DateTime occurredAt, OrderId? orderId = null)
    {
        // Ensure a RestaurantAccount exists for the default restaurant (only create if it doesn't exist)
        await TestDatabaseManager.ExecuteInScopeAsync(async db =>
        {
            var accountExists = await db.RestaurantAccounts.AnyAsync();

            if (!accountExists)
            {
                var restaurantAccount = RestaurantAccount.Create(RestaurantId.Create(Testing.TestData.DefaultRestaurantId));
                restaurantAccount.IsSuccess.Should().BeTrue();
                db.RestaurantAccounts.Add(restaurantAccount.Value);
                await db.SaveChangesAsync();
            }
        });

        var refundAmount = new Money(-Math.Abs(amount), Currencies.Default);
        await TestDatabaseManager.ExecuteInScopeAsync(async db =>
        {
            var account = await db.RestaurantAccounts.FirstAsync();
            var txResult = AccountTransaction.Create(account.Id, TransactionType.RefundDeduction, refundAmount, orderId);
            txResult.IsSuccess.Should().BeTrue();
            var transaction = txResult.Value;
            db.AccountTransactions.Add(transaction);
            await db.SaveChangesAsync();
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"AccountTransactions\" SET \"Timestamp\" = {occurredAt} WHERE \"Id\" = {transaction.Id.Value}");
        });
    }

    private static async Task UpsertReviewSummaryAsync(Guid restaurantId, double averageRating, int totalReviews)
    {
        await TestDatabaseManager.ExecuteInScopeAsync(async db =>
        {
            var summary = await db.RestaurantReviewSummaries.FindAsync(restaurantId);
            if (summary is null)
            {
                summary = new RestaurantReviewSummary
                {
                    RestaurantId = restaurantId,
                    AverageRating = averageRating,
                    TotalReviews = totalReviews,
                    Ratings1 = 0,
                    Ratings2 = 0,
                    Ratings3 = 0,
                    Ratings4 = 0,
                    Ratings5 = totalReviews,
                    TotalWithText = totalReviews,
                    LastReviewAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };
                db.RestaurantReviewSummaries.Add(summary);
            }
            else
            {
                summary.AverageRating = averageRating;
                summary.TotalReviews = totalReviews;
                summary.Ratings5 = totalReviews;
                summary.TotalWithText = totalReviews;
                summary.LastReviewAtUtc = DateTime.UtcNow;
                summary.UpdatedAtUtc = DateTime.UtcNow;
                db.RestaurantReviewSummaries.Update(summary);
            }

            await db.SaveChangesAsync();
        });
    }

    private static async Task ClearDailySeriesAsync()
    {
        await TestDatabaseManager.ExecuteInScopeAsync(async db =>
        {
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"AdminDailyPerformanceSeries\";");
        });
    }

    private static async Task<OrderId> CreateDeliveredOrderAtAsync(DateTime placementTimestamp, decimal? totalOverride = null)
    {
        var orderId = await OrderLifecycleTestHelper.CreateReadyOrderAsync();
        // Ensure delivery time is in the past to pass validation (DeliveredAtUtc <= DateTime.UtcNow.AddMinutes(5))
        var deliveredAt = placementTimestamp > DateTime.UtcNow.AddMinutes(-5)
            ? DateTime.UtcNow.AddMinutes(-1) // If placement is recent, deliver 1 minute ago
            : DateTime.UtcNow.AddMinutes(-2); // If placement is old, still deliver in the past

        (await OrderLifecycleTestHelper.MarkDeliveredAsync(orderId, deliveredAt)).IsSuccess.Should().BeTrue();

        await TestDatabaseManager.ExecuteInScopeAsync(async db =>
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"Orders\" SET \"PlacementTimestamp\" = {placementTimestamp}, \"ActualDeliveryTime\" = {deliveredAt} WHERE \"Id\" = {orderId.Value}");
            if (totalOverride.HasValue)
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"Orders\" SET \"TotalAmount_Amount\" = {totalOverride.Value} WHERE \"Id\" = {orderId.Value}");
            }
        });

        return orderId;
    }

    private static async Task CreateUserCreatedAtAsync(DateTime timestamp)
    {
        var userResult = User.Create($"User-{Guid.NewGuid():N}", $"user-{Guid.NewGuid():N}@yummyzoom.test");
        userResult.IsSuccess.Should().BeTrue();
        var user = userResult.Value;
        user.Created = new DateTimeOffset(timestamp, TimeSpan.Zero);
        user.LastModified = user.Created;
        await AddAsync(user);
        await TestDatabaseManager.ExecuteInScopeAsync(async db =>
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"DomainUsers\" SET \"Created\" = {timestamp}, \"LastModified\" = {timestamp}, \"IsActive\" = TRUE WHERE \"Id\" = {user.Id.Value}");
        });
    }

    private static async Task CreateRestaurantCreatedAtAsync(DateTime timestamp)
    {
        var scenario = await TestDataFactory.CreateSecondRestaurantWithMenuItemsAsync();
        await TestDatabaseManager.ExecuteInScopeAsync(async db =>
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"Restaurants\" SET \"Created\" = {timestamp}, \"IsVerified\" = TRUE, \"IsAcceptingOrders\" = TRUE WHERE \"Id\" = {scenario.RestaurantId}");
        });
    }

    private static async Task UpdateRestaurantBalanceAsync(Guid restaurantId, decimal balance)
    {
        // First ensure a RestaurantAccount exists for this restaurant
        var restaurantAccount = RestaurantAccount.Create(RestaurantId.Create(restaurantId));
        restaurantAccount.IsSuccess.Should().BeTrue();
        await AddAsync(restaurantAccount.Value);

        // Then update the balance
        await TestDatabaseManager.ExecuteInScopeAsync(async db =>
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"RestaurantAccounts\" SET \"CurrentBalance_Amount\" = {balance} WHERE \"RestaurantId\" = {restaurantId}");
        });
    }

    private static async Task SetOrderCouponAsync(OrderId orderId)
    {
        await TestDatabaseManager.ExecuteInScopeAsync(async db =>
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"Orders\" SET \"AppliedCouponId\" = {Testing.TestData.DefaultCouponId} WHERE \"Id\" = {orderId.Value}");
        });
    }

    private sealed record PlatformMetrics(
        int TotalOrders,
        int ActiveOrders,
        int DeliveredOrders,
        decimal GrossMerchandiseVolume,
        decimal TotalRefunds,
        int ActiveRestaurants,
        int ActiveCustomers,
        int OpenSupportTickets,
        int TotalReviews,
        DateTime? LastOrderAtUtc);

    private sealed class RecordingInvalidationPublisher : ICacheInvalidationPublisher
    {
        public bool ThrowOnPublish { get; set; }
        public int PublishAttempts { get; private set; }
        public List<CacheInvalidationMessage> Messages { get; } = new();

        public Task PublishAsync(CacheInvalidationMessage message, CancellationToken ct = default)
        {
            PublishAttempts++;
            if (ThrowOnPublish)
            {
                ThrowOnPublish = false;
                throw new InvalidOperationException("Simulated publish failure");
            }

            Messages.Add(message);
            return Task.CompletedTask;
        }
    }
}









