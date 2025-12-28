using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.FunctionalTests.Common;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Orders.Commands.InitiateOrder;

/// <summary>
/// Base test class for all InitiateOrder command tests.
/// Provides common setup and shared functionality for testing the InitiateOrder command handler.
/// Uses service replacement for testing with mocked dependencies.
/// </summary>
public abstract class InitiateOrderTestBase : BaseTestFixture
{
    /// <summary>
    /// Mock for the payment gateway service, configured for each test.
    /// </summary>
    protected Mock<IPaymentGatewayService> PaymentGatewayMock { get; private set; } = null!;

    /// <summary>
    /// Sets up the test environment before each test.
    /// Configures user context and payment gateway mock.
    /// </summary>
    [SetUp]
    public virtual async Task SetUp()
    {
        // Set default customer as current user for all tests
        SetUserId(Testing.TestData.DefaultCustomerId);

        // Setup payment gateway mock for each test with successful response by default
        PaymentGatewayMock = InitiateOrderTestHelper.SetupSuccessfulPaymentGatewayMock();

        // Replace the payment gateway service with our mock
        ReplaceService(PaymentGatewayMock.Object);

        await Task.CompletedTask; // Placeholder for any async setup logic
    }

    /// <summary>
    /// Configures the payment gateway mock for successful operations.
    /// This can be called by derived test classes to reset the mock to successful state.
    /// </summary>
    protected async Task ConfigureSuccessfulPaymentGatewayAsync()
    {
        InitiateOrderTestHelper.ConfigurePaymentGatewayMockToSucceed(PaymentGatewayMock);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Configures the payment gateway mock for failed operations.
    /// </summary>
    /// <param name="errorMessage">The error message to return from the payment gateway</param>
    protected async Task ConfigureFailingPaymentGatewayAsync(string errorMessage = "Payment gateway error")
    {
        InitiateOrderTestHelper.ConfigurePaymentGatewayMockToFail(PaymentGatewayMock, errorMessage: errorMessage);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Configures the payment gateway mock with custom response.
    /// </summary>
    /// <param name="paymentIntentId">Custom payment intent ID</param>
    /// <param name="clientSecret">Custom client secret</param>
    protected async Task ConfigureCustomPaymentGatewayAsync(string paymentIntentId, string clientSecret)
    {
        InitiateOrderTestHelper.ConfigurePaymentGatewayMockToSucceed(PaymentGatewayMock, paymentIntentId, clientSecret);
        await Task.CompletedTask;
    }
}
