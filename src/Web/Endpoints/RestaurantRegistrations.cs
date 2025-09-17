using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YummyZoom.Application.RestaurantRegistrations.Commands.SubmitRestaurantRegistration;
using YummyZoom.Application.RestaurantRegistrations.Commands.ApproveRestaurantRegistration;
using YummyZoom.Application.RestaurantRegistrations.Commands.RejectRestaurantRegistration;
using YummyZoom.Application.RestaurantRegistrations.Queries.GetMyRestaurantRegistrations;
using YummyZoom.Application.RestaurantRegistrations.Queries.GetPendingRestaurantRegistrations;
using YummyZoom.Application.RestaurantRegistrations.Queries.Common;
using YummyZoom.Web.Infrastructure;
using YummyZoom.Web.Infrastructure.Http;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Web.Endpoints;

public sealed class RestaurantRegistrations : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(this);

        // Submit a registration (Authenticated)
        group.MapPost("/", async ([FromBody] SubmitRequest body, ISender sender, IUser user) =>
        {
            var uid = user.DomainUserId ?? (user.Id is string sid && Guid.TryParse(sid, out var gid) ? UserId.Create(gid) : throw new UnauthorizedAccessException());
            var cmd = new SubmitRestaurantRegistrationCommand(
                Name: body.Name,
                Description: body.Description,
                CuisineType: body.CuisineType,
                Street: body.Street,
                City: body.City,
                State: body.State,
                ZipCode: body.ZipCode,
                Country: body.Country,
                PhoneNumber: body.PhoneNumber,
                Email: body.Email,
                BusinessHours: body.BusinessHours,
                LogoUrl: body.LogoUrl,
                Latitude: body.Latitude,
                Longitude: body.Longitude)
            { UserId = uid };

            var result = await sender.Send(cmd);
            return result.ToIResult();
        })
        .WithName("SubmitRestaurantRegistration")
        .WithSummary("Submit restaurant registration")
        .WithDescription("Authenticated users submit a restaurant registration for admin approval.")
        .WithStandardCreationResults<SubmitRestaurantRegistrationResponse>();

        // List my registrations
        group.MapGet("/mine", async (ISender sender) =>
        {
            var result = await sender.Send(new GetMyRestaurantRegistrationsQuery());
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("GetMyRestaurantRegistrations")
        .WithSummary("List my restaurant registrations")
        .WithDescription("Returns the authenticated user's submitted restaurant registrations (most recent first).")
        .Produces<IReadOnlyList<RegistrationSummaryDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // Admin endpoints
        var admin = group.MapGroup("/admin").RequireAuthorization(new AuthorizeAttribute { Roles = "Administrator" });

        admin.MapGet("/pending", async (int pageNumber, int pageSize, ISender sender) =>
        {
            var result = await sender.Send(new GetPendingRestaurantRegistrationsQuery(pageNumber, pageSize));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("GetPendingRestaurantRegistrations")
        .WithSummary("List pending registrations (admin)")
        .WithDescription("Admin-only: returns pending restaurant registrations.")
        .Produces<object>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        admin.MapPost("/{registrationId:guid}/approve", async (Guid registrationId, [FromBody] ApproveRequest body, ISender sender) =>
        {
            var result = await sender.Send(new ApproveRestaurantRegistrationCommand(registrationId, body.Note));
            return result.ToIResult();
        })
        .WithName("ApproveRestaurantRegistration")
        .WithSummary("Approve a restaurant registration (admin)")
        .WithDescription("Admin-only: approves the specified registration, creates a verified restaurant, and assigns the submitter as owner.")
        .WithStandardResults<ApproveRestaurantRegistrationResponse>();

        admin.MapPost("/{registrationId:guid}/reject", async (Guid registrationId, [FromBody] RejectRequest body, ISender sender) =>
        {
            var result = await sender.Send(new RejectRestaurantRegistrationCommand(registrationId, body.Reason));
            return result.ToIResult();
        })
        .WithName("RejectRestaurantRegistration")
        .WithSummary("Reject a restaurant registration (admin)")
        .WithDescription("Admin-only: rejects the specified registration with a reason.")
        .WithStandardResults();
    }

    public sealed record SubmitRequest(
        string Name,
        string Description,
        string CuisineType,
        string Street,
        string City,
        string State,
        string ZipCode,
        string Country,
        string PhoneNumber,
        string Email,
        string BusinessHours,
        string? LogoUrl,
        double? Latitude,
        double? Longitude);

    public sealed record ApproveRequest(string? Note);
    public sealed record RejectRequest(string Reason);
}

