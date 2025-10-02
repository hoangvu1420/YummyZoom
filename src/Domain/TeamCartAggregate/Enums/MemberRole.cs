namespace YummyZoom.Domain.TeamCartAggregate.Enums;

/// <summary>
/// Represents the role of a member in a team cart.
/// </summary>
public enum MemberRole
{
    /// <summary>
    /// The creator and manager of the team cart.
    /// </summary>
    Host,

    /// <summary>
    /// An invited participant in the team cart.
    /// </summary>
    Guest
}
