using YummyZoom.Domain.TeamCartAggregate.Enums;

namespace YummyZoom.Application.TeamCarts.Queries.Common;

/// <summary>
/// DTO representing detailed TeamCart information for read operations.
/// Includes all members, items, payments and financial calculations.
/// </summary>
public sealed record TeamCartDetailsDto(
    Guid TeamCartId,
    Guid RestaurantId,
    Guid HostUserId,
    TeamCartStatus Status,
    string ShareTokenMasked,
    DateTime? Deadline,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    decimal TipAmount,
    string TipCurrency,
    Guid? AppliedCouponId,
    decimal DiscountAmount,
    string DiscountCurrency,
    decimal Subtotal,
    decimal DeliveryFee,
    decimal TaxAmount,
    decimal Total,
    string Currency,
    IReadOnlyList<TeamCartMemberDto> Members,
    IReadOnlyList<TeamCartItemDto> Items,
    IReadOnlyList<MemberPaymentDto> MemberPayments
);

/// <summary>
/// DTO representing a TeamCart member.
/// </summary>
public sealed record TeamCartMemberDto(
    Guid TeamCartMemberId,
    Guid UserId,
    string Name,
    string Role
);

/// <summary>
/// DTO representing a TeamCart item with its customizations.
/// </summary>
public sealed record TeamCartItemDto(
    Guid TeamCartItemId,
    Guid AddedByUserId,
    Guid MenuItemId,
    Guid MenuCategoryId,
    string ItemName,
    int Quantity,
    decimal BasePriceAmount,
    string BasePriceCurrency,
    IReadOnlyList<TeamCartItemCustomizationDto> Customizations
);

/// <summary>
/// DTO representing customizations applied to a TeamCart item.
/// </summary>
public sealed record TeamCartItemCustomizationDto(
    Guid CustomizationGroupId,
    string GroupDisplayTitle,
    Guid CustomizationOptionId,
    string OptionDisplayTitle,
    decimal PriceAdjustmentAmount,
    string PriceAdjustmentCurrency
);

/// <summary>
/// DTO representing a member's payment commitment.
/// </summary>
public sealed record MemberPaymentDto(
    Guid UserId,
    string PaymentMethod,
    string PaymentStatus,
    decimal Amount,
    string Currency,
    string? TransactionId,
    DateTime? ProcessedAt
);
