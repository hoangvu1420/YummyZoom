using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.OrderAggregate;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.Services;
using YummyZoom.Domain.TeamCartAggregate;
using YummyZoom.Domain.TeamCartAggregate.Enums;

namespace YummyZoom.Domain.UnitTests.Services.TeamCartConversionServiceTests;

/// <summary>
/// Base class for TeamCartConversionService tests providing common setup and helper methods.
/// This base class uses the REAL implementation of OrderFinancialService to ensure
/// the integration between the two services is tested correctly.
/// </summary>
public abstract class TeamCartConversionServiceTestsBase
{
    protected TeamCartConversionService TeamCartConversionService { get; private set; } = null!;

    [SetUp]
    public virtual void SetUp()
    {
        // Instantiate the real dependencies. No more mocks.
        var financialService = new Domain.Services.OrderFinancialService();
        TeamCartConversionService = new TeamCartConversionService(financialService);
    }

    /// <summary>
    /// Verifies the created Order has correct properties
    /// </summary>
    protected void VerifyOrderProperties(Order order, TeamCart teamCart, Money expectedTotal)
    {
        order.Should().NotBeNull();
        order.CustomerId.Should().Be(teamCart.HostUserId);
        order.RestaurantId.Should().Be(teamCart.RestaurantId);
        order.OrderItems.Should().HaveCount(teamCart.Items.Count);
        order.TotalAmount.Amount.Should().BeApproximately(expectedTotal.Amount, 0.01m);
        order.Status.Should().Be(OrderStatus.Placed);
    }

    /// <summary>
    /// Verifies payment transactions were created correctly after adjustment
    /// </summary>
    protected void VerifyPaymentTransactions(Order order, TeamCart teamCart)
    {
        var expectedTransactionCount = teamCart.MemberPayments.Count;
        order.PaymentTransactions.Should().HaveCount(expectedTransactionCount);

        // The sum of the created transactions must equal the order's final total.
        var transactionSum = order.PaymentTransactions.Sum(t => t.Amount.Amount);
        transactionSum.Should().BeApproximately(order.TotalAmount.Amount, 0.01m);

        foreach (var memberPayment in teamCart.MemberPayments)
        {
            var orderTransaction = order.PaymentTransactions
                .FirstOrDefault(t => t.PaidByUserId == memberPayment.UserId);

            orderTransaction.Should().NotBeNull();
            orderTransaction!.Status.Should().Be(YummyZoom.Domain.OrderAggregate.Enums.PaymentStatus.Succeeded);
        }
    }

    /// <summary>
    /// Verifies TeamCart state after conversion
    /// </summary>
    protected void VerifyTeamCartState(TeamCart teamCart)
    {
        teamCart.Status.Should().Be(TeamCartStatus.Converted);
    }
}
