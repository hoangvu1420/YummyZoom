namespace YummyZoom.Infrastructure.Payments.Mock;

public class MockPayoutProviderOptions
{
    public const string SectionName = "MockPayoutProvider";

    public bool Enabled { get; init; } = true;
    public int ProcessingDelaySeconds { get; init; } = 5;
    public bool ForceFailure { get; init; } = false;
    public string FailureReason { get; init; } = "Mock payout failure.";
}
