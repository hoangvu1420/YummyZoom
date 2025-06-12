using YummyZoom.Application.Users.Commands.RegisterUser;
using YummyZoom.Infrastructure.Identity;
using Microsoft.AspNetCore.Mvc;
using YummyZoom.Application.Users.Commands.AssignRoleToUser;
using YummyZoom.Application.Users.Commands.RemoveRoleFromUser;
using YummyZoom.Application.Users.Commands.RegisterDevice;
using YummyZoom.Application.Users.Commands.UnregisterDevice;

namespace YummyZoom.Web.Endpoints;

public class Users : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(this);

        // Keep other identity API endpoints
        group.MapIdentityApi<ApplicationUser>();

        // Add custom registration endpoint
        group.MapPost("/register-custom", async ([FromBody] RegisterUserCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            
            return result.IsSuccess
                ? Results.Ok(new RegisterUserResponse { UserId = result.Value })
                : result.ToIResult();
        })
        .WithName("RegisterUserCustom")
        .WithStandardResults<RegisterUserResponse>();

        // Add endpoint for assigning roles
        group.MapPost("/assign-role", async ([FromBody] AssignRoleToUserCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Ok()
                : result.ToIResult();
        })
        .WithName("AssignRoleToUser");

        // Add endpoint for removing roles
        group.MapPost("/remove-role", async ([FromBody] RemoveRoleFromUserCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Ok()
                : result.ToIResult();
        })
        .WithName("RemoveRoleFromUser");

        // Add endpoint for registering devices
        group.MapPost("/devices/register", async ([FromBody] RegisterDeviceCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Ok()
                : result.ToIResult();
        })
        .WithName("RegisterDevice")
        .RequireAuthorization();

        // Add endpoint for unregistering devices
        group.MapPost("/devices/unregister", async ([FromBody] UnregisterDeviceCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Ok()
                : result.ToIResult();
        })
        .WithName("UnregisterDevice")
        .RequireAuthorization();
    }
}
