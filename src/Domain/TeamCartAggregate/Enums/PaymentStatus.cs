namespace YummyZoom.Domain.TeamCartAggregate.Enums;

/// <summary>
/// Represents the status of a payment in a team cart.
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// No payment commitment has been made yet.
    /// </summary>
    Pending,

    /// <summary>
    /// The member has committed to pay cash on delivery.
    /// </summary>
    CommittedToCOD,

    /// <summary>
    /// The member has successfully paid online.
    /// </summary>
    PaidOnline,

    /// <summary>
    /// The online payment attempt failed.
    /// </summary>
    Failed
}