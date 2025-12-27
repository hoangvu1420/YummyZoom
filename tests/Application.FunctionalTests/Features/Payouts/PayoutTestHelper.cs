using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.PayoutAggregate;
using YummyZoom.Domain.PayoutAggregate.Enums;
using YummyZoom.Domain.RestaurantAccountAggregate;
using YummyZoom.Domain.RestaurantAccountAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Application.FunctionalTests.Features.Payouts;

public static class PayoutTestHelper
{
    public static async Task<RestaurantAccount> CreateAccountWithBalanceAsync(
        Guid restaurantId,
        decimal balance,
        bool withPayoutMethod)
    {
        return await TestDatabaseManager.ExecuteInScopeAsync(async db =>
        {
            var accountResult = RestaurantAccount.Create(RestaurantId.Create(restaurantId));
            accountResult.IsSuccess.Should().BeTrue();
            var account = accountResult.Value;

            if (balance > 0)
            {
                var revenueResult = account.RecordRevenue(
                    new Money(balance, Currencies.Default),
                    OrderId.CreateUnique());
                revenueResult.IsSuccess.Should().BeTrue();
            }

            if (withPayoutMethod)
            {
                var payoutMethod = PayoutMethodDetails.Create("demo-bank");
                payoutMethod.IsSuccess.Should().BeTrue();
                account.UpdatePayoutMethod(payoutMethod.Value);
            }

            db.RestaurantAccounts.Add(account);
            await db.SaveChangesAsync();
            return account;
        });
    }

    public static async Task<Payout> CreatePayoutAsync(
        RestaurantAccountId accountId,
        RestaurantId restaurantId,
        decimal amount,
        PayoutStatus status,
        DateTimeOffset? requestedAt = null)
    {
        return await TestDatabaseManager.ExecuteInScopeAsync(async db =>
        {
            var payoutResult = Payout.Create(
                accountId,
                restaurantId,
                new Money(amount, Currencies.Default),
                Guid.NewGuid().ToString("N"),
                requestedAt ?? DateTimeOffset.UtcNow);
            payoutResult.IsSuccess.Should().BeTrue();
            var payout = payoutResult.Value;

            switch (status)
            {
                case PayoutStatus.Processing:
                    payout.MarkProcessing("mock_ref").IsSuccess.Should().BeTrue();
                    break;
                case PayoutStatus.Completed:
                    payout.MarkProcessing("mock_ref").IsSuccess.Should().BeTrue();
                    payout.MarkCompleted().IsSuccess.Should().BeTrue();
                    break;
                case PayoutStatus.Failed:
                    payout.MarkProcessing("mock_ref").IsSuccess.Should().BeTrue();
                    payout.MarkFailed("failed").IsSuccess.Should().BeTrue();
                    break;
                case PayoutStatus.Requested:
                default:
                    break;
            }

            db.Payouts.Add(payout);
            await db.SaveChangesAsync();
            return payout;
        });
    }

    public static async Task<RestaurantAccount> ReserveHoldAsync(Guid restaurantId, decimal holdAmount)
    {
        return await TestDatabaseManager.ExecuteInScopeAsync(async db =>
        {
            var account = await db.RestaurantAccounts.FirstAsync(a => a.RestaurantId == RestaurantId.Create(restaurantId));
            var reserveResult = account.ReservePayout(new Money(holdAmount, account.CurrentBalance.Currency));
            reserveResult.IsSuccess.Should().BeTrue();
            await db.SaveChangesAsync();
            return account;
        });
    }
}
