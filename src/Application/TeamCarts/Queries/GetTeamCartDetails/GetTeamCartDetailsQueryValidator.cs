using FluentValidation;

namespace YummyZoom.Application.TeamCarts.Queries.GetTeamCartDetails;

public sealed class GetTeamCartDetailsQueryValidator : AbstractValidator<GetTeamCartDetailsQuery>
{
    public GetTeamCartDetailsQueryValidator()
    {
        RuleFor(x => x.TeamCartIdGuid)
            .NotEmpty()
            .WithMessage("TeamCart ID is required.");
    }
}
