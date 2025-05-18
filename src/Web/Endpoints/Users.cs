using YummyZoom.Application.Users.Commands.RegisterUser;
using YummyZoom.Infrastructure.Identity;
using Microsoft.AspNetCore.Mvc;
using YummyZoom.Application.Users.Commands.AssignRoleToUser;
using YummyZoom.Application.Users.Commands.RemoveRoleFromUser;

namespace YummyZoom.Web.Endpoints;

public class Users : EndpointGroupBase
{
    public override void Map(WebApplication app)
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
    }
}
