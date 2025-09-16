using YummyZoom.Application.Users.Commands.RegisterUser;
using YummyZoom.Application.Users.Commands.CompleteProfile;
using YummyZoom.Application.Users.Commands.UpsertPrimaryAddress;
using YummyZoom.Application.Users.Queries.GetMyProfile;
using YummyZoom.Infrastructure.Identity;
using Microsoft.AspNetCore.Mvc;
using YummyZoom.Application.RoleAssignments.Commands.CreateRoleAssignment;
using YummyZoom.Application.RoleAssignments.Commands.DeleteRoleAssignment;
using YummyZoom.Application.Users.Commands.RegisterDevice;
using YummyZoom.Application.Users.Commands.UnregisterDevice;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication;
using MediatR;
using YummyZoom.Application.Auth.Commands.RequestPhoneOtp;
using YummyZoom.Application.Auth.Commands.VerifyPhoneOtp;
using YummyZoom.Application.Auth.Commands.CompleteSignup;

namespace YummyZoom.Web.Endpoints;

public class Users : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(this);

        // Self-service profile endpoints
        group.MapGet("/me", async (ISender sender) =>
        {
            var result = await sender.Send(new GetMyProfileQuery());
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToIResult();
        })
        .WithName("GetMyProfile")
        .WithStandardResults<GetMyProfileResponse>()
        .RequireAuthorization();

        group.MapPut("/me/profile", async ([FromBody] CompleteProfileCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess
                ? Results.NoContent()
                : result.ToIResult();
        })
        .WithName("CompleteProfile")
        .WithStandardResults()
        .RequireAuthorization();

        group.MapPut("/me/address", async ([FromBody] UpsertPrimaryAddressCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess
                ? Results.Ok(new { addressId = result.Value })
                : result.ToIResult();
        })
        .WithName("UpsertPrimaryAddress")
        .WithStandardResults()
        .RequireAuthorization();

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
        group.MapPost("/auth/otp/request", async (
            [FromBody] RequestPhoneOtpCommand command,
            ISender sender,
            IHostEnvironment environment) =>
        {
            var result = await sender.Send(command);
            if (!result.IsSuccess) return result.ToIResult();

            if (environment.IsDevelopment())
            {
                return Results.Ok(new { code = result.Value.Code });
            }

            return Results.Accepted();
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
            return Results.Ok(new {
                isNewUser = result.Value.IsNewUser,
                requiresOnboarding = result.Value.RequiresOnboarding
            });
        })
        .WithName("Auth_Otp_Verify")
        .WithStandardResults()
        .AllowAnonymous();

        // Complete signup after OTP for newly created identity users
        group.MapPost("/auth/complete-signup", async ([FromBody] CompleteSignupCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok() : result.ToIResult();
        })
        .WithName("CompleteSignup")
        .WithStandardResults()
        .RequireAuthorization();
    }
}





