using YummyZoom.Application.Users.Commands.RegisterUser;
using YummyZoom.Infrastructure.Identity;
using Microsoft.AspNetCore.Mvc;
using YummyZoom.Application.RoleAssignments.Commands.CreateRoleAssignment;
using YummyZoom.Application.RoleAssignments.Commands.DeleteRoleAssignment;
using YummyZoom.Application.Users.Commands.RegisterDevice;
using YummyZoom.Application.Users.Commands.UnregisterDevice;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using MediatR;
using YummyZoom.Application.Auth.Commands.RequestPhoneOtp;
using YummyZoom.Application.Auth.Commands.VerifyPhoneOtp;

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

        // Phone OTP authentication endpoints
        group.MapPost("/auth/otp/request", async ([FromBody] RequestPhoneOtpCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Accepted() : result.ToIResult();
        })
        .WithName("Auth_Otp_Request")
        .WithStandardResults()
        .AllowAnonymous();

        group.MapPost("/auth/otp/verify", async (
            [FromBody] VerifyPhoneOtpCommand command,
            ISender sender,
            UserManager<ApplicationUser> users,
            IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
            HttpContext http) =>
        {
            var result = await sender.Send(command);
            if (!result.IsSuccess) return result.ToIResult();

            var user = await users.FindByIdAsync(result.Value.IdentityUserId.ToString());
            if (user is null) return Results.Unauthorized();

            var principal = await claimsFactory.CreateAsync(user);
            await http.SignInAsync(IdentityConstants.BearerScheme, principal);
            return Results.Ok();
        })
        .WithName("Auth_Otp_Verify")
        .WithStandardResults()
        .AllowAnonymous();
    }
}
