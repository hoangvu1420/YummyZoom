using FluentValidation;

namespace YummyZoom.Application.TeamCarts.Queries.GetTeamCartRealTimeViewModel;

public sealed class GetTeamCartRealTimeViewModelQueryValidator : AbstractValidator<GetTeamCartRealTimeViewModelQuery>
{
    public GetTeamCartRealTimeViewModelQueryValidator()
    {
        RuleFor(x => x.TeamCartIdGuid)
            .NotEmpty()
            .WithMessage("TeamCart ID is required.");
    }
}
