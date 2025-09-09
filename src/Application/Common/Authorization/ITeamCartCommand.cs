using YummyZoom.Domain.TeamCartAggregate.ValueObjects;

namespace YummyZoom.Application.Common.Authorization;

/// <summary>
/// Contextual interface for team cart-scoped commands. Enables fine-grained 
/// authorization policies (e.g., MustBeTeamCartHost, MustBeTeamCartMember) without 
/// modifying existing command records.
/// </summary>
public interface ITeamCartCommand : IContextualCommand
{
    /// <summary>
    /// Strongly-typed team cart identifier associated with the command.
    /// </summary>
    TeamCartId TeamCartId { get; }

    string IContextualCommand.ResourceType => "TeamCart";
    string IContextualCommand.ResourceId => TeamCartId.Value.ToString();
}
