using YummyZoom.Domain.TeamCartAggregate.ValueObjects;

namespace YummyZoom.Application.Common.Authorization;

/// <summary>
/// Contextual interface for team cart-scoped queries. Enables future fine-grained 
/// authorization policies (e.g., MustBeTeamCartMember, MustBeTeamCartHost) without 
/// modifying existing query records.
/// </summary>
public interface ITeamCartQuery : IContextualCommand
{
    /// <summary>
    /// Strongly-typed team cart identifier associated with the query.
    /// </summary>
    TeamCartId TeamCartId { get; }

    string IContextualCommand.ResourceType => "TeamCart";
    string IContextualCommand.ResourceId => TeamCartId.Value.ToString();
}
