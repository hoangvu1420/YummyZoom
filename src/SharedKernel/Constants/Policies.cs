namespace YummyZoom.SharedKernel.Constants;

public abstract class Policies
{
    public const string CanPurge = nameof(CanPurge);
    public const string MustBeRestaurantOwner = nameof(MustBeRestaurantOwner);
    public const string MustBeRestaurantStaff = nameof(MustBeRestaurantStaff);
    public const string MustBeUserOwner = nameof(MustBeUserOwner);

    public const string MustBeOrderOwner = nameof(MustBeOrderOwner);
    public const string MustBeOrderManager = nameof(MustBeOrderManager);
}
