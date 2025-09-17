using FluentValidation;
using YummyZoom.Application.Common.Security;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.RestaurantAggregate;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantRegistrationAggregate.Errors;
using YummyZoom.Domain.RestaurantRegistrationAggregate.ValueObjects;
using YummyZoom.Domain.RoleAssignmentAggregate;
using YummyZoom.Domain.RoleAssignmentAggregate.Enums;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.RestaurantRegistrations.Commands.ApproveRestaurantRegistration;

[Authorize(Roles = Roles.Administrator)]
public sealed record ApproveRestaurantRegistrationCommand(
    Guid RegistrationId,
    string? Note
) : IRequest<Result<ApproveRestaurantRegistrationResponse>>;

public sealed record ApproveRestaurantRegistrationResponse(Guid RestaurantId);

public sealed class ApproveRestaurantRegistrationCommandValidator : AbstractValidator<ApproveRestaurantRegistrationCommand>
{
    public ApproveRestaurantRegistrationCommandValidator()
    {
        RuleFor(x => x.RegistrationId).NotEmpty();
        RuleFor(x => x.Note).MaximumLength(500).When(x => x.Note is not null);
    }
}

public sealed class ApproveRestaurantRegistrationCommandHandler : IRequestHandler<ApproveRestaurantRegistrationCommand, Result<ApproveRestaurantRegistrationResponse>>
{
    private readonly IRestaurantRegistrationRepository _registrations;
    private readonly IRestaurantProvisioningService _provisioning;
    private readonly IRoleAssignmentRepository _roles;
    private readonly IUnitOfWork _uow;
    private readonly IUser _user;

    public ApproveRestaurantRegistrationCommandHandler(
        IRestaurantRegistrationRepository registrations,
        IRestaurantProvisioningService provisioning,
        IRoleAssignmentRepository roles,
        IUnitOfWork uow,
        IUser user)
    {
        _registrations = registrations;
        _provisioning = provisioning;
        _roles = roles;
        _uow = uow;
        _user = user;
    }

    public Task<Result<ApproveRestaurantRegistrationResponse>> Handle(ApproveRestaurantRegistrationCommand request, CancellationToken cancellationToken)
    {
        return _uow.ExecuteInTransactionAsync(async () =>
        {
            var id = RestaurantRegistrationId.Create(request.RegistrationId);
            var reg = await _registrations.GetByIdAsync(id, cancellationToken);
            if (reg is null)
            {
                return Result.Failure<ApproveRestaurantRegistrationResponse>(RestaurantRegistrationErrors.NotFound(request.RegistrationId));
            }

            // Provision restaurant via service (Infra will persist + call Verify())
            var provision = await _provisioning.CreateAndVerifyAsync(
                new RestaurantProvisioningRequest(
                    reg.Name,
                    reg.LogoUrl,
                    null,
                    reg.Description,
                    reg.CuisineType,
                    reg.Street,
                    reg.City,
                    reg.State,
                    reg.ZipCode,
                    reg.Country,
                    reg.PhoneNumber,
                    reg.Email,
                    reg.BusinessHours,
                    reg.Latitude,
                    reg.Longitude),
                cancellationToken);

            if (provision.IsFailure)
            {
                return Result.Failure<ApproveRestaurantRegistrationResponse>(provision.Error);
            }

            var restaurantId = RestaurantId.Create(provision.Value);

            // Assign owner role to submitter
            var role = RoleAssignment.Create(
                reg.SubmitterUserId,
                restaurantId,
                RestaurantRole.Owner);
            if (role.IsFailure)
            {
                return Result.Failure<ApproveRestaurantRegistrationResponse>(role.Error);
            }
            await _roles.AddAsync(role.Value, cancellationToken);

            // Mark approved on the registration (emits event)
            UserId reviewer;
            if (_user.DomainUserId is { } duid)
            {
                reviewer = duid;
            }
            else if (_user.Id is string sid && Guid.TryParse(sid, out var gid))
            {
                reviewer = UserId.Create(gid);
            }
            else
            {
                reviewer = reg.SubmitterUserId;
            }

            var approveResult = reg.Approve(reviewer, restaurantId.Value, request.Note);
            if (approveResult.IsFailure)
            {
                return Result.Failure<ApproveRestaurantRegistrationResponse>(approveResult.Error);
            }
            await _registrations.UpdateAsync(reg, cancellationToken);

            return Result.Success(new ApproveRestaurantRegistrationResponse(restaurantId.Value));
        }, cancellationToken);
    }
}
