using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CouponAggregate.Errors;
using YummyZoom.Domain.CouponAggregate.Events;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.Common.Models;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.CouponAggregate;

public sealed class Coupon : AggregateRoot<CouponId, Guid>, IAuditableEntity, ISoftDeletableEntity
{
    private const int MaxCodeLength = 50;

    public RestaurantId RestaurantId { get; private set; }
    public string Code { get; private set; }
    public string Description { get; private set; }
    public CouponValue Value { get; private set; }
    public AppliesTo AppliesTo { get; private set; }
    public Money? MinOrderAmount { get; private set; }
    public DateTime ValidityStartDate { get; private set; }
    public DateTime ValidityEndDate { get; private set; }
    public int? TotalUsageLimit { get; private set; }
    public int CurrentTotalUsageCount { get; private set; }
    public bool IsEnabled { get; private set; }
    public int? UsageLimitPerUser { get; private set; }

    // Properties from IAuditableEntity
    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public string? LastModifiedBy { get; set; }

    // Properties from ISoftDeletableEntity
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedOn { get; set; }
    public string? DeletedBy { get; set; }

    private Coupon(
        CouponId id,
        RestaurantId restaurantId,
        string code,
        string description,
        CouponValue value,
        AppliesTo appliesTo,
        Money? minOrderAmount,
        DateTime validityStartDate,
        DateTime validityEndDate,
        int? totalUsageLimit,
        int currentTotalUsageCount,
        bool isEnabled,
        int? usageLimitPerUser)
        : base(id)
    {
        RestaurantId = restaurantId;
        Code = code;
        Description = description;
        Value = value;
        AppliesTo = appliesTo;
        MinOrderAmount = minOrderAmount;
        ValidityStartDate = validityStartDate;
        ValidityEndDate = validityEndDate;
        TotalUsageLimit = totalUsageLimit;
        CurrentTotalUsageCount = currentTotalUsageCount;
        IsEnabled = isEnabled;
        UsageLimitPerUser = usageLimitPerUser;
    }

    /// <summary>
    /// Creates a new coupon
    /// </summary>
    public static Result<Coupon> Create(
        RestaurantId restaurantId,
        string code,
        string description,
        CouponValue value,
        AppliesTo appliesTo,
        DateTime validityStartDate,
        DateTime validityEndDate,
        Money? minOrderAmount = null,
        int? totalUsageLimit = null,
        int? usageLimitPerUser = null,
        bool isEnabled = true)
    {
        // Validate code
        if (string.IsNullOrWhiteSpace(code))
        {
            return Result.Failure<Coupon>(CouponErrors.CouponCodeEmpty);
        }

        if (code.Length > MaxCodeLength)
        {
            return Result.Failure<Coupon>(CouponErrors.CouponCodeTooLong(MaxCodeLength));
        }

        // Validate description
        if (string.IsNullOrWhiteSpace(description))
        {
            return Result.Failure<Coupon>(CouponErrors.CouponDescriptionEmpty);
        }

        // Validate validity period
        if (validityEndDate <= validityStartDate)
        {
            return Result.Failure<Coupon>(CouponErrors.InvalidValidityPeriod);
        }

        // Validate usage limits
        if (totalUsageLimit <= 0)
        {
            return Result.Failure<Coupon>(CouponErrors.InvalidUsageLimit);
        }

        if (usageLimitPerUser <= 0)
        {
            return Result.Failure<Coupon>(CouponErrors.InvalidPerUserLimit);
        }

        // Validate minimum order amount
        if (minOrderAmount?.Amount <= 0)
        {
            return Result.Failure<Coupon>(CouponErrors.InvalidMinOrderAmount);
        }

        var coupon = new Coupon(
            CouponId.CreateUnique(),
            restaurantId,
            code.Trim().ToUpperInvariant(), // Normalize code
            description.Trim(),
            value,
            appliesTo,
            minOrderAmount,
            validityStartDate,
            validityEndDate,
            totalUsageLimit,
            currentTotalUsageCount: 0,
            isEnabled,
            usageLimitPerUser);

        // Raise domain event
        coupon.AddDomainEvent(new CouponCreated(
            coupon.Id,
            coupon.RestaurantId,
            coupon.Code,
            coupon.Value.Type,
            coupon.ValidityStartDate,
            coupon.ValidityEndDate));

        return Result.Success(coupon);
    }

    /// <summary>
    /// Factory method for recreating coupon from persistence
    /// </summary>
    public static Result<Coupon> Create(
        CouponId id,
        RestaurantId restaurantId,
        string code,
        string description,
        CouponValue value,
        AppliesTo appliesTo,
        DateTime validityStartDate,
        DateTime validityEndDate,
        int currentTotalUsageCount,
        Money? minOrderAmount = null,
        int? totalUsageLimit = null,
        int? usageLimitPerUser = null,
        bool isEnabled = true)
    {
        // Validate usage count doesn't exceed limit
        if (totalUsageLimit.HasValue && currentTotalUsageCount > totalUsageLimit.Value)
        {
            return Result.Failure<Coupon>(CouponErrors.UsageCountCannotExceedLimit(
                currentTotalUsageCount, totalUsageLimit));
        }

        return Result.Success(new Coupon(
            id,
            restaurantId,
            code,
            description,
            value,
            appliesTo,
            minOrderAmount,
            validityStartDate,
            validityEndDate,
            totalUsageLimit,
            currentTotalUsageCount,
            isEnabled,
            usageLimitPerUser));
    }

    /// <summary>
    /// Increments the usage count when coupon is used.
    /// 
    /// NOTE: In production scenarios with potential concurrent access, total usage limit 
    /// enforcement is handled atomically at the repository level (TryIncrementUsageCountAsync) 
    /// to prevent race conditions. This method is primarily used for:
    /// - Unit testing business rules
    /// - Non-concurrent scenarios  
    /// - Domain logic validation
    /// - Single-threaded operations
    /// 
    /// For concurrent scenarios, use the atomic repository operation instead.
    /// </summary>
    /// <param name="usageTime">The time when the coupon is being used. If not provided, uses current UTC time.</param>
    public Result Use(DateTime? usageTime = null)
    {
        // Check if coupon is enabled
        if (!IsEnabled)
        {
            return Result.Failure(CouponErrors.CouponDisabled);
        }

        var now = usageTime ?? DateTime.UtcNow;

        // Check validity period
        if (now < ValidityStartDate)
        {
            return Result.Failure(CouponErrors.CouponNotYetValid);
        }

        if (now > ValidityEndDate)
        {
            return Result.Failure(CouponErrors.CouponExpired);
        }

        // Check usage limit
        if (TotalUsageLimit.HasValue && CurrentTotalUsageCount >= TotalUsageLimit.Value)
        {
            return Result.Failure(CouponErrors.UsageLimitExceeded);
        }

        var previousCount = CurrentTotalUsageCount;
        CurrentTotalUsageCount++;

        // Raise domain event
        AddDomainEvent(new CouponUsed(
            Id,
            previousCount,
            CurrentTotalUsageCount,
            now));

        return Result.Success();
    }

    /// <summary>
    /// Enables the coupon
    /// </summary>
    /// <param name="enabledTime">The time when the coupon is being enabled. If not provided, uses current UTC time.</param>
    public Result Enable(DateTime? enabledTime = null)
    {
        if (IsEnabled)
        {
            return Result.Success(); // Already enabled, no-op
        }

        IsEnabled = true;

        AddDomainEvent(new CouponEnabled(Id, enabledTime ?? DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Disables the coupon
    /// </summary>
    /// <param name="disabledTime">The time when the coupon is being disabled. If not provided, uses current UTC time.</param>
    public Result Disable(DateTime? disabledTime = null)
    {
        if (!IsEnabled)
        {
            return Result.Success(); // Already disabled, no-op
        }

        IsEnabled = false;

        AddDomainEvent(new CouponDisabled(Id, disabledTime ?? DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Updates the coupon description
    /// </summary>
    public Result UpdateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return Result.Failure(CouponErrors.CouponDescriptionEmpty);
        }

        Description = description.Trim();
        return Result.Success();
    }

    /// <summary>
    /// Sets or updates the minimum order amount required for the coupon
    /// </summary>
    public Result SetMinimumOrderAmount(Money? minOrderAmount)
    {
        if (minOrderAmount?.Amount <= 0)
        {
            return Result.Failure(CouponErrors.InvalidMinOrderAmount);
        }

        MinOrderAmount = minOrderAmount;
        return Result.Success();
    }

    /// <summary>
    /// Removes the minimum order amount requirement
    /// </summary>
    public Result RemoveMinimumOrderAmount()
    {
        MinOrderAmount = null;
        return Result.Success();
    }

    /// <summary>
    /// Sets or updates the total usage limit for the coupon
    /// </summary>
    public Result SetTotalUsageLimit(int? totalUsageLimit)
    {
        if (totalUsageLimit is <= 0)
        {
            return Result.Failure(CouponErrors.InvalidUsageLimit);
        }

        // Check that new total usage limit doesn't violate current usage
        if (CurrentTotalUsageCount > totalUsageLimit)
        {
            return Result.Failure(CouponErrors.UsageCountCannotExceedLimit(
                CurrentTotalUsageCount, totalUsageLimit));
        }

        TotalUsageLimit = totalUsageLimit;
        return Result.Success();
    }

    /// <summary>
    /// Removes the total usage limit (makes it unlimited)
    /// </summary>
    public Result RemoveTotalUsageLimit()
    {
        TotalUsageLimit = null;
        return Result.Success();
    }

    /// <summary>
    /// Sets or updates the per-user usage limit for the coupon
    /// </summary>
    public Result SetPerUserUsageLimit(int? usageLimitPerUser)
    {
        if (usageLimitPerUser <= 0)
        {
            return Result.Failure(CouponErrors.InvalidPerUserLimit);
        }

        UsageLimitPerUser = usageLimitPerUser;
        return Result.Success();
    }

    /// <summary>
    /// Removes the per-user usage limit (makes it unlimited per user)
    /// </summary>
    public Result RemovePerUserUsageLimit()
    {
        UsageLimitPerUser = null;
        return Result.Success();
    }

    /// <summary>
    /// Checks if the coupon is currently valid for use
    /// </summary>
    public bool IsValidForUse(DateTime? checkTime = null)
    {
        var now = checkTime ?? DateTime.UtcNow;

        return IsEnabled &&
               now >= ValidityStartDate &&
               now <= ValidityEndDate &&
               (!TotalUsageLimit.HasValue || CurrentTotalUsageCount < TotalUsageLimit.Value);
    }

    /// <summary>
    /// Checks if the coupon applies to a specific menu item
    /// </summary>
    public bool AppliesToItem(MenuItemId menuItemId, MenuCategoryId categoryId)
    {
        return AppliesTo.AppliesToItem(menuItemId, categoryId);
    }

    /// <summary>
    /// Marks this coupon as deleted. This is the single, authoritative way to delete this aggregate.
    /// </summary>
    /// <param name="deletedOn">The timestamp when the entity was deleted</param>
    /// <param name="deletedBy">Who deleted the entity</param>
    /// <returns>A Result indicating success</returns>
    public Result MarkAsDeleted(DateTimeOffset deletedOn, string? deletedBy = null)
    {
        if (IsDeleted)
        {
            return Result.Success();
        }

        IsDeleted = true;
        DeletedOn = deletedOn;
        DeletedBy = deletedBy;

        AddDomainEvent(new CouponDeleted(Id));

        return Result.Success();
    }

#pragma warning disable CS8618
    // For EF Core
    private Coupon()
    {
    }
#pragma warning restore CS8618
}
