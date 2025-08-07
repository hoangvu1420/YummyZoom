using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Orders.Commands.InitiateOrder;

/// <summary>
/// Tests for InitiateOrder command authentication and authorization.
/// </summary>
public class InitiateOrderAuthorizationTests : InitiateOrderTestBase
{
    [Test]
    public async Task InitiateOrder_WithoutAuthentication_ShouldFailWithUnauthorized()
    {
        // Arrange
        ClearUserId(); // Remove authentication
        var command = InitiateOrderTestHelper.BuildValidCommand();

        // Act
        var act = async () => await SendAsync(command);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Test]
    public async Task InitiateOrder_AsAuthenticatedUser_ShouldSucceed()
    {
        // Arrange
        SetUserId(Testing.TestData.DefaultCustomerId); // Set authenticated user
        var command = InitiateOrderTestHelper.BuildValidCommand();

        // Act
        var result = await SendAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.OrderId.Should().NotBe(Guid.Empty);
    }

    [Test]
    public async Task InitiateOrder_WithValidCustomerContext_ShouldCreateOrderWithCorrectCustomer()
    {
        // Arrange
        var customerId = Testing.TestData.DefaultCustomerId;
        SetUserId(customerId);
        var command = InitiateOrderTestHelper.BuildValidCommand(customerId: customerId);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        
        // Verify the order was created with the correct customer
        var order = await FindOrderAsync(result.Value.OrderId);
        order.Should().NotBeNull();
        order!.CustomerId.Value.Should().Be(customerId);
    }
}
