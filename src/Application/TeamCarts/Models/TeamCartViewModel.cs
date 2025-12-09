using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;

namespace YummyZoom.Application.TeamCarts.Models;

public sealed class TeamCartViewModel
{
    public required TeamCartId CartId { get; init; }
    public required Guid RestaurantId { get; init; }
    public required TeamCartStatus Status { get; set; }
    public DateTime? Deadline { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? ShareTokenMasked { get; set; }
    public string? ShareToken { get; set; }
    public decimal TipAmount { get; set; }
    public string TipCurrency { get; set; } = "USD";
    public string? CouponCode { get; set; }
    public decimal DiscountAmount { get; set; }
    public string DiscountCurrency { get; set; } = "USD";
    public decimal Subtotal { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal DeliveryFee { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public decimal CashOnDeliveryPortion { get; set; }
    public long QuoteVersion { get; set; }
    public long Version { get; set; }

    public List<Member> Members { get; init; } = new();
    public List<Item> Items { get; init; } = new();

    public sealed class Member
    {
        public required Guid UserId { get; init; }
        public required string Name { get; init; }
        public required string Role { get; init; }
        public string PaymentStatus { get; set; } = "Pending";
        public decimal CommittedAmount { get; set; }
        public string? OnlineTransactionId { get; set; }
        public decimal QuotedAmount { get; set; }
        public bool IsReady { get; set; }
    }

    public sealed class Item
    {
        public required Guid ItemId { get; init; }
        public required Guid AddedByUserId { get; init; }
        public required string Name { get; init; }
        public string? ImageUrl { get; init; }
        public required Guid MenuItemId { get; init; }
        public required int Quantity { get; set; }
        public required decimal BasePrice { get; init; }
        public required decimal LineTotal { get; set; }
        public List<Customization> Customizations { get; init; } = new();
    }

    public sealed class Customization
    {
        public required string GroupName { get; init; }
        public required string ChoiceName { get; init; }
        public required decimal PriceAdjustment { get; init; }
    }
}
