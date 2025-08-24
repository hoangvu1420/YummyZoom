using System.Threading.Channels;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Features.Orders.Commands.InitiateOrder;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Web.SignalR;

[TestFixture]
public class SignalRHubEndToEndTests : BaseTestFixture
{
    // [Test]
    // [Category("SignalR"), Category("E2E"), Ignore("Optional heavy test. Enable when SignalR.Client is referenced and auth flow is configured.")]
    // public async Task Receive_OrderPlaced_After_Subscribe()
    // {
    //     var factory = TestInfrastructure.GetFactory();

    //     // Ensure user has owner permission for the default restaurant
    //     var userId = await RunAsDefaultUserAsync();
    //     var restaurantId = Testing.TestData.DefaultRestaurantId;
    //     CustomWebApplicationFactory.GetTestUserService().AddPermissionClaim(Domain.RoleAssignmentAggregate.Enums.Roles.RestaurantOwner, restaurantId.ToString());

    //     var received = Channel.CreateUnbounded<object>();

    //     var connection = new HubConnectionBuilder()
    //         .WithUrl(new Uri(factory.Server.BaseAddress, "/hubs/restaurant-orders"), options =>
    //         {
    //             options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
    //         })
    //         .WithAutomaticReconnect()
    //         .Build();

    //     connection.On<object>("ReceiveOrderPlaced", dto => received.Writer.TryWrite(dto));

    //     await connection.StartAsync();
    //     await connection.InvokeAsync("SubscribeToRestaurant", restaurantId);

    //     // Trigger an OrderPlaced by initiating a COD order
    //     var cmd = InitiateOrderTestHelper.BuildValidCommand(paymentMethod: InitiateOrderTestHelper.PaymentMethods.CashOnDelivery);
    //     var initResponse = await SendAndUnwrapAsync(cmd);
    //     initResponse.Should().NotBeNull();

    //     await DrainOutboxAsync(TimeSpan.FromSeconds(10));

    //     using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    //     var read = await received.Reader.WaitToReadAsync(cts.Token);
    //     read.Should().BeTrue();

    //     await connection.DisposeAsync();
    // }
}
