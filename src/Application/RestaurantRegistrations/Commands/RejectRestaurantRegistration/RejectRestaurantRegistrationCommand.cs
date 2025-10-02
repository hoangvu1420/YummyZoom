using FluentValidation;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.RestaurantRegistrationAggregate.Errors;
using YummyZoom.Domain.RestaurantRegistrationAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.RestaurantRegistrations.Commands.RejectRestaurantRegistration;

[Authorize(Roles = Roles.Administrator)]
public sealed record RejectRestaurantRegistrationCommand(
    Guid RegistrationId,
    string Reason
) : IRequest<Result>;

public sealed class RejectRestaurantRegistrationCommandValidator : AbstractValidator<RejectRestaurantRegistrationCommand>
{
    public RejectRestaurantRegistrationCommandValidator()
    {
        RuleFor(x => x.RegistrationId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public sealed class RejectRestaurantRegistrationCommandHandler : IRequestHandler<RejectRestaurantRegistrationCommand, Result>
{
    private readonly IRestaurantRegistrationRepository _registrations;
    private readonly IUnitOfWork _uow;
    private readonly IUser _user;

    public RejectRestaurantRegistrationCommandHandler(IRestaurantRegistrationRepository registrations, IUnitOfWork uow, IUser user)
    {
        _registrations = registrations;
        _uow = uow;
        _user = user;
    }

    public Task<Result> Handle(RejectRestaurantRegistrationCommand request, CancellationToken cancellationToken)
    {
        return _uow.ExecuteInTransactionAsync(async () =>
        {
            var id = RestaurantRegistrationId.Create(request.RegistrationId);
            var reg = await _registrations.GetByIdAsync(id, cancellationToken);
            if (reg is null)
            {
                return Result.Failure(RestaurantRegistrationErrors.NotFound(request.RegistrationId));
            }

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

            var result = reg.Reject(reviewer, request.Reason);
            if (result.IsFailure)
            {
                return result;
            }

            await _registrations.UpdateAsync(reg, cancellationToken);
            return Result.Success();
        }, cancellationToken);
    }
}
