using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YummyZoom.Application.Notifications.Commands.SendBroadcastNotification;
using YummyZoom.Application.Notifications.Commands.SendNotificationToUser;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Web.Endpoints;

[Authorize(Roles = Roles.Administrator)]
public class Notifications : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(this);

        // Admin endpoint for sending notification to specific user
        group.MapPost("/send-to-user", async ([FromBody] SendNotificationToUserCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : result.ToIResult();
        })
        .WithName("SendNotificationToUser")
        .WithSummary("Send notification to specific user")
        .WithDescription("Sends a push notification to all active devices of a specific user. Admin access required.")
        .WithStandardResults()
        .RequireAuthorization();

        // Admin endpoint for broadcasting notification to all users
        group.MapPost("/send-broadcast", async ([FromBody] SendBroadcastNotificationCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : result.ToIResult();
        })
        .WithName("SendBroadcastNotification")
        .WithSummary("Send broadcast notification")
        .WithDescription("Sends a push notification to all active users in the system. Admin access required.")
        .WithStandardResults()
        .RequireAuthorization();
    }
}
