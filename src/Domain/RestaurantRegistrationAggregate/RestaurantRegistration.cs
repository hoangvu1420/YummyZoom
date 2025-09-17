using System.Text.RegularExpressions;
using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.RestaurantRegistrationAggregate.Enums;
using YummyZoom.Domain.RestaurantRegistrationAggregate.Errors;
using YummyZoom.Domain.RestaurantRegistrationAggregate.Events;
using YummyZoom.Domain.RestaurantRegistrationAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.RestaurantRegistrationAggregate;

public sealed class RestaurantRegistration : AggregateRoot<RestaurantRegistrationId, Guid>, ICreationAuditable
{
    // Core submission
    public UserId SubmitterUserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string CuisineType { get; private set; } = string.Empty;

    // Contact & address (kept primitive for MVP)
    public string Street { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string State { get; private set; } = string.Empty;
    public string ZipCode { get; private set; } = string.Empty;
    public string Country { get; private set; } = string.Empty;

    public string PhoneNumber { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string BusinessHours { get; private set; } = string.Empty;

    public string? LogoUrl { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }

    // Workflow
    public RestaurantRegistrationStatus Status { get; private set; }
    public DateTime SubmittedAtUtc { get; private set; }
    public DateTime? ReviewedAtUtc { get; private set; }
    public UserId? ReviewedByUserId { get; private set; }
    public string? ReviewNote { get; private set; }

    // Audit
    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }

    private RestaurantRegistration(
        RestaurantRegistrationId id,
        UserId submitterUserId,
        string name,
        string description,
        string cuisineType,
        string street,
        string city,
        string state,
        string zipCode,
        string country,
        string phoneNumber,
        string email,
        string businessHours,
        string? logoUrl,
        double? latitude,
        double? longitude) : base(id)
    {
        SubmitterUserId = submitterUserId;
        Name = name;
        Description = description;
        CuisineType = cuisineType;
        Street = street;
        City = city;
        State = state;
        ZipCode = zipCode;
        Country = country;
        PhoneNumber = phoneNumber;
        Email = email;
        BusinessHours = businessHours;
        LogoUrl = logoUrl;
        Latitude = latitude;
        Longitude = longitude;
        Status = RestaurantRegistrationStatus.Pending;
        SubmittedAtUtc = DateTime.UtcNow;
    }

    public static Result<RestaurantRegistration> Submit(
        UserId submitterUserId,
        string name,
        string description,
        string cuisineType,
        string street,
        string city,
        string state,
        string zipCode,
        string country,
        string phoneNumber,
        string email,
        string businessHours,
        string? logoUrl = null,
        double? latitude = null,
        double? longitude = null)
    {
        // Validate inputs (lightweight, align with Restaurant aggregate caps)
        var v = ValidateSubmission(name, description, cuisineType, street, city, state, zipCode, country, phoneNumber, email, businessHours, logoUrl, latitude, longitude);
        if (v.IsFailure)
            return Result.Failure<RestaurantRegistration>(v.Error);

        var reg = new RestaurantRegistration(
            RestaurantRegistrationId.CreateUnique(),
            submitterUserId,
            name.Trim(),
            description.Trim(),
            cuisineType.Trim(),
            street.Trim(), city.Trim(), state.Trim(), zipCode.Trim(), country.Trim(),
            phoneNumber.Trim(), email.Trim(), businessHours.Trim(),
            string.IsNullOrWhiteSpace(logoUrl) ? null : logoUrl.Trim(),
            latitude, longitude);

        reg.AddDomainEvent(new RegistrationSubmitted(reg.Id, submitterUserId, reg.Name, reg.City));

        return Result.Success(reg);
    }

    public Result Approve(UserId reviewerUserId, Guid restaurantId, string? note = null)
    {
        if (Status == RestaurantRegistrationStatus.Approved)
            return Result.Failure(RestaurantRegistrationErrors.AlreadyApproved);
        if (Status == RestaurantRegistrationStatus.Rejected)
            return Result.Failure(RestaurantRegistrationErrors.AlreadyRejected);
        if (Status != RestaurantRegistrationStatus.Pending)
            return Result.Failure(RestaurantRegistrationErrors.NotPending);

        Status = RestaurantRegistrationStatus.Approved;
        ReviewedAtUtc = DateTime.UtcNow;
        ReviewedByUserId = reviewerUserId;
        ReviewNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

        AddDomainEvent(new RegistrationApproved(Id, SubmitterUserId, reviewerUserId, restaurantId));
        return Result.Success();
    }

    public Result Reject(UserId reviewerUserId, string reason)
    {
        if (Status == RestaurantRegistrationStatus.Approved)
            return Result.Failure(RestaurantRegistrationErrors.AlreadyApproved);
        if (Status == RestaurantRegistrationStatus.Rejected)
            return Result.Failure(RestaurantRegistrationErrors.AlreadyRejected);
        if (Status != RestaurantRegistrationStatus.Pending)
            return Result.Failure(RestaurantRegistrationErrors.NotPending);
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(RestaurantRegistrationErrors.ReasonIsRequired);

        Status = RestaurantRegistrationStatus.Rejected;
        ReviewedAtUtc = DateTime.UtcNow;
        ReviewedByUserId = reviewerUserId;
        ReviewNote = reason.Trim();

        AddDomainEvent(new RegistrationRejected(Id, SubmitterUserId, reviewerUserId, ReviewNote));
        return Result.Success();
    }

    private static Result ValidateSubmission(
        string name,
        string description,
        string cuisineType,
        string street,
        string city,
        string state,
        string zip,
        string country,
        string phone,
        string email,
        string businessHours,
        string? logoUrl,
        double? lat,
        double? lng)
    {
        static Result Req(string v, string field, int max)
        {
            if (string.IsNullOrWhiteSpace(v))
                return Result.Failure(RestaurantRegistrationErrors.InvalidField(field, "required"));
            if (v.Length > max)
                return Result.Failure(RestaurantRegistrationErrors.InvalidField(field, $"too long (max {max})"));
            return Result.Success();
        }

        var r = Req(name, nameof(name), 100);
        if (r.IsFailure) return r;
        r = Req(description, nameof(description), 500);
        if (r.IsFailure) return r;
        r = Req(cuisineType, nameof(cuisineType), 50);
        if (r.IsFailure) return r;
        r = Req(street, nameof(street), 200);
        if (r.IsFailure) return r;
        r = Req(city, nameof(city), 100);
        if (r.IsFailure) return r;
        r = Req(state, nameof(state), 100);
        if (r.IsFailure) return r;
        r = Req(zip, nameof(zip), 20);
        if (r.IsFailure) return r;
        r = Req(country, nameof(country), 100);
        if (r.IsFailure) return r;
        r = Req(phone, nameof(phone), 30);
        if (r.IsFailure) return r;
        r = Req(email, nameof(email), 320);
        if (r.IsFailure) return r;
        r = Req(businessHours, nameof(businessHours), 200);
        if (r.IsFailure) return r;

        if (!string.IsNullOrWhiteSpace(logoUrl))
        {
            var urlPattern = @"^https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)$";
            if (!Regex.IsMatch(logoUrl, urlPattern))
                return Result.Failure(RestaurantRegistrationErrors.InvalidField(nameof(logoUrl), "invalid URL"));
        }

        if (lat.HasValue && (lat.Value < -90 || lat.Value > 90))
            return Result.Failure(RestaurantRegistrationErrors.InvalidField(nameof(lat), "out of range [-90,90]"));
        if (lng.HasValue && (lng.Value < -180 || lng.Value > 180))
            return Result.Failure(RestaurantRegistrationErrors.InvalidField(nameof(lng), "out of range [-180,180]"));

        // Basic email/phone format checks (very light; detailed validation sits in validators later)
        if (!email.Contains('@'))
            return Result.Failure(RestaurantRegistrationErrors.InvalidField(nameof(email), "must contain '@'"));

        return Result.Success();
    }

#pragma warning disable CS8618
    private RestaurantRegistration() { }
#pragma warning restore CS8618
}

