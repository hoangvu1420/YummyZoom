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
using YummyZoom.Application.Auth.Commands.RequestPhoneOtp;
using YummyZoom.Application.Auth.Commands.VerifyPhoneOtp;
using YummyZoom.Application.Auth.Commands.CompleteSignup;
using YummyZoom.Application.Auth.Commands.SetPassword;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Web.Endpoints;

/// <summary>
/// User-focused endpoints for authentication, onboarding, self-service profile,
/// addresses, devices, and role assignments. Base route resolves to /api/v1/users
/// via versioned endpoint grouping.
/// </summary>
public class Users : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        // Authenticated user endpoints (default protected group)
        var protectedGroup = app
            .MapGroup(this)
            .RequireAuthorization();

        #region Self-service Profile Endpoints (Authenticated)

        // GET /api/v1/users/me
        protectedGroup.MapGet("/me", async (ISender sender) =>
        {
            var result = await sender.Send(new GetMyProfileQuery());
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToIResult();
        })
        .WithName("GetMyProfile")
        .WithSummary("Get my profile")
        .WithDescription("Returns the authenticated user's profile including name, email, phone, and primary address if available.")
        .WithStandardResults<GetMyProfileResponse>()
        .RequireAuthorization();

        // PUT /api/v1/users/me/profile
        protectedGroup.MapPut("/me/profile", async ([FromBody] CompleteProfileCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess
                ? Results.NoContent()
                : result.ToIResult();
        })
        .WithName("CompleteProfile")
        .WithSummary("Update my profile")
        .WithDescription("Updates the authenticated user's display name and optionally email. Requires that signup is completed (domain user exists).")
        .WithStandardResults()
        .RequireAuthorization();

        // PUT /api/v1/users/me/address
        protectedGroup.MapPut("/me/address", async ([FromBody] UpsertPrimaryAddressCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess
                ? Results.Ok(new { addressId = result.Value })
                : result.ToIResult();
        })
        .WithName("UpsertPrimaryAddress")
        .WithSummary("Create or update my primary address")
        .WithDescription("Creates or updates the authenticated user's primary address and returns its identifier.")
        .WithStandardResults()
        .RequireAuthorization();

        #endregion

        #region Device Management (Authenticated)

        // POST /api/v1/users/devices/register
        protectedGroup.MapPost("/devices/register", async ([FromBody] RegisterDeviceCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.NoContent() : result.ToIResult();
        })
        .WithName("RegisterDevice")
        .WithSummary("Register my device for notifications")
        .WithDescription("Registers a device token for push notifications for the authenticated user.")
        .WithStandardResults()
        .RequireAuthorization();

        // POST /api/v1/users/devices/unregister
        protectedGroup.MapPost("/devices/unregister", async ([FromBody] UnregisterDeviceCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.NoContent() : result.ToIResult();
        })
        .WithName("UnregisterDevice")
        .WithSummary("Unregister my device")
        .WithDescription("Removes a previously registered device token for the authenticated user.")
        .WithStandardResults()
        .RequireAuthorization();

        #endregion

        #region My Reviews (Authenticated)

        // GET /api/v1/users/me/reviews
        protectedGroup.MapGet("/me/reviews", async (int pageNumber, int pageSize, ISender sender) =>
        {
            var result = await sender.Send(new YummyZoom.Application.Reviews.Queries.GetMyReviews.GetMyReviewsQuery(pageNumber, pageSize));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("GetMyReviews")
        .WithSummary("List my reviews")
        .WithDescription("Returns the authenticated user's reviews, newest first.")
        .Produces<YummyZoom.Application.Common.Models.PaginatedList<YummyZoom.Application.Reviews.Queries.Common.ReviewDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization();

        #endregion

        #region Role Assignments (Management)

        // POST /api/v1/users/role-assignments
        var roleGroup = app.MapGroup(this); // Adjust authorization in future as needed
        roleGroup.MapPost("/role-assignments", async ([FromBody] CreateRoleAssignmentCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("CreateRoleAssignment")
        .WithSummary("Create a role assignment")
        .WithDescription("Assigns a role to a user. Typically used by administrative flows.")
        .WithStandardResults<CreateRoleAssignmentResponse>();

        // DELETE /api/v1/users/role-assignments/{roleAssignmentId}
        roleGroup.MapDelete("/role-assignments/{roleAssignmentId:guid}", async (Guid roleAssignmentId, ISender sender) =>
        {
            var command = new DeleteRoleAssignmentCommand(roleAssignmentId);
            var result = await sender.Send(command);

            return result.IsSuccess ? Results.NoContent() : result.ToIResult();
        })
        .WithName("DeleteRoleAssignment")
        .WithSummary("Delete a role assignment")
        .WithDescription("Removes a role assignment from a user. Typically used by administrative flows.")
        .WithStandardResults();

        #endregion

        #region Authentication – Phone OTP (Public)

        // Public endpoints (no authentication required)
        var publicGroup = app.MapGroup(this);

        // POST /api/v1/users/auth/otp/request
        publicGroup.MapPost("/auth/otp/request", async (
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
        .WithSummary("Request phone OTP code")
        .WithDescription("Requests a one-time code for the provided phone number. In development, the code is returned in the response; in production a 202 Accepted is returned and the code is sent via SMS.")
        .WithStandardResults()
        .AllowAnonymous();

        // POST /api/v1/users/auth/otp/verify
        publicGroup.MapPost("/auth/otp/verify", async (
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
            // Return a SignIn result so the BearerToken handler emits access/refresh tokens
            return Results.SignIn(principal, authenticationScheme: IdentityConstants.BearerScheme);
        })
        .WithName("Auth_Otp_Verify")
        .WithSummary("Verify phone OTP code")
        .WithDescription("Verifies the one-time code and signs in the user. Returns onboarding flags indicating whether the client should complete signup.")
        .WithStandardResults()
        .AllowAnonymous();

        #endregion

        #region Authentication – Complete Signup (Authenticated)

        // POST /api/v1/users/auth/complete-signup
        protectedGroup.MapPost("/auth/complete-signup", async ([FromBody] CompleteSignupCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok() : result.ToIResult();
        })
        .WithName("CompleteSignup")
        .WithSummary("Complete signup after OTP")
        .WithDescription("Creates the Domain User record for the authenticated Identity user after a successful OTP verification. Idempotent if already completed.")
        .WithStandardResults()
        .RequireAuthorization();

        #endregion

        #region Authentication – Set Password (Authenticated)

        // POST /api/v1/users/auth/set-password
        protectedGroup.MapPost("/auth/set-password", async ([FromBody] SetPasswordCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.NoContent() : result.ToIResult();
        })
        .WithName("Auth_SetPassword")
        .WithSummary("Set password for OTP-created accounts")
        .WithDescription("Sets an initial password for the authenticated user if none exists yet. Username remains the E.164 phone number.")
        .WithStandardResults()
        .RequireAuthorization();

        #endregion

        #region Authentication – Status (Authenticated)

        // GET /api/v1/users/auth/status
        protectedGroup.MapGet("/auth/status", async (
            IUser currentUser,
            IUserAggregateRepository usersRepo,
            CancellationToken ct) =>
        {
            if (currentUser.Id is null)
            {
                return Results.Unauthorized();
            }

            if (!Guid.TryParse(currentUser.Id, out var identityId))
            {
                return Results.BadRequest(new { code = "Auth.InvalidUserId", message = "Authenticated user id is invalid." });
            }

            var domainUserId = UserId.Create(identityId);
            var existing = await usersRepo.GetByIdAsync(domainUserId, ct);
            var isNew = existing is null;

            return Results.Ok(new { isNewUser = isNew, requiresOnboarding = isNew });
        })
        .WithName("Auth_Status")
        .WithSummary("Get authentication/onboarding status")
        .WithDescription("Returns flags indicating whether the authenticated user has completed signup (i.e., domain user exists).")
        .WithStandardResults()
        .RequireAuthorization();

        #endregion

        #region Legacy/Custom Registration (Public)

        // POST /api/v1/users/register-custom (legacy/custom entry)
        var customRegGroup = app.MapGroup(this);

        // Keep other identity API endpoints
        customRegGroup.MapIdentityApi<ApplicationUser>();

        customRegGroup.MapPost("/register-custom", async ([FromBody] RegisterUserCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess
                ? Results.Ok(new RegisterUserResponse { UserId = result.Value })
                : result.ToIResult();
        })
        .WithName("RegisterUserCustom")
        .WithSummary("Register user (custom)")
        .WithDescription("Legacy/custom registration endpoint retained for compatibility. Not used in the standard phone OTP flow.")
        .WithStandardResults<RegisterUserResponse>();

        #endregion
    }
}


