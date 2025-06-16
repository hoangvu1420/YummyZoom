using YummyZoom.Application.Users.Commands.RegisterUser;
using YummyZoom.Infrastructure.Identity;
using Microsoft.AspNetCore.Mvc;
using YummyZoom.Application.RoleAssignments.Commands.CreateRoleAssignment;
using YummyZoom.Application.RoleAssignments.Commands.DeleteRoleAssignment;
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

        // Add endpoint for creating role assignments
        group.MapPost("/role-assignments", async ([FromBody] CreateRoleAssignmentCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToIResult();
        })
        .WithName("CreateRoleAssignment")
        .WithStandardResults<CreateRoleAssignmentResponse>();

        // Add endpoint for removing role assignments
        group.MapDelete("/role-assignments/{roleAssignmentId:guid}", async (Guid roleAssignmentId, ISender sender) =>
        {
            var command = new DeleteRoleAssignmentCommand(roleAssignmentId);
            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : result.ToIResult();
        })
        .WithName("DeleteRoleAssignment")
        .WithStandardResults();

        // Add endpoint for registering devices
        group.MapPost("/devices/register", async ([FromBody] RegisterDeviceCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : result.ToIResult();
        })
        .WithName("RegisterDevice")
        .WithStandardResults()
        .RequireAuthorization();

        // Add endpoint for unregistering devices
        group.MapPost("/devices/unregister", async ([FromBody] UnregisterDeviceCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : result.ToIResult();
        })
        .WithName("UnregisterDevice")
        .WithStandardResults()
        .RequireAuthorization();
    }
}
