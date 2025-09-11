using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.TeamCarts.Models;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;

namespace YummyZoom.Infrastructure.StateStores.TeamCartStore;

/// <summary>
/// Minimal viable Redis-backed TeamCart store implementation.
/// Notes / Future Enhancements:
/// - Replace read-modify-write with atomic Lua scripts (CAS using version) for high contention updates.
/// - Consider Redis Hash structure for top-level fields and smaller JSON blobs for arrays to enable partial updates.
/// - Re-compute financial totals server-side on each mutation; for MVP we keep shallow updates and rely on application recomputation when needed.
/// - Publish lightweight update events on a channel for fan-out; MVP publishes a generic update type.
/// - Add basic size guards (max items/members) and input validation.
/// </summary>
public sealed class RedisTeamCartStore : ITeamCartStore
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisTeamCartStore> _logger;
    private readonly TeamCartStoreOptions _options;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public RedisTeamCartStore(
        IConnectionMultiplexer redis,
        IOptions<TeamCartStoreOptions> options,
        ILogger<RedisTeamCartStore> logger)
    {
        _redis = redis;
        _logger = logger;
        _options = options.Value;
    }

    private IDatabase Db => _redis.GetDatabase();
    private TimeSpan Ttl => TimeSpan.FromMinutes(Math.Max(1, _options.TtlMinutes));
    private string Key(TeamCartId id) => $"{_options.KeyPrefix}:teamcart:vm:{id.Value}:v1";

    public async Task<TeamCartViewModel?> GetVmAsync(TeamCartId cartId, CancellationToken ct = default)
    {
        var key = Key(cartId);
        var val = await Db.StringGetAsync(key);
        if (val.IsNullOrEmpty) return null;
        try
        {
            var stored = JsonSerializer.Deserialize<RedisVm>(val!, _jsonOptions);
            if (stored is null) return null;
            // Sliding TTL refresh
            _ = Db.KeyExpireAsync(key, Ttl);
            return MapFrom(stored);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize TeamCart VM from Redis (Key={Key})", key);
            return null;
        }
    }

    public async Task CreateVmAsync(TeamCartViewModel vm, CancellationToken ct = default)
    {
        var key = Key(vm.CartId);
        var stored = MapTo(vm);
        stored.Version = Math.Max(1, stored.Version);
        var json = JsonSerializer.Serialize(stored, _jsonOptions);
        await Db.StringSetAsync(key, json, Ttl);
        await PublishUpdateAsync(vm.CartId, "created");
    }

    public async Task DeleteVmAsync(TeamCartId cartId, CancellationToken ct = default)
    {
        var key = Key(cartId);
        await Db.KeyDeleteAsync(key);
        await PublishUpdateAsync(cartId, "deleted");
    }

    public async Task AddMemberAsync(TeamCartId cartId, TeamCartViewModel.Member member, CancellationToken ct = default)
        => await MutateAsync(cartId, vm =>
        {
            var existing = vm.Members.FirstOrDefault(m => m.UserId == member.UserId);
            if (existing is not null)
            {
                // Replace immutable/init-only member record with the new one
                vm.Members.RemoveAll(m => m.UserId == member.UserId);
            }
            vm.Members.Add(member);
        }, "member_added");

    public async Task AddItemAsync(TeamCartId cartId, TeamCartViewModel.Item item, CancellationToken ct = default)
        => await MutateAsync(cartId, vm =>
        {
            vm.Items.Add(item);
            RecalculateTotals(vm);
        }, "item_added");

    public async Task UpdateItemQuantityAsync(TeamCartId cartId, Guid itemId, int newQuantity, CancellationToken ct = default)
        => await MutateAsync(cartId, vm =>
        {
            var it = vm.Items.FirstOrDefault(i => i.ItemId == itemId);
            if (it is null) return;
            it.Quantity = newQuantity;
            it.LineTotal = it.BasePrice * newQuantity;
            RecalculateTotals(vm);
        }, "item_quantity_updated");

    public async Task RemoveItemAsync(TeamCartId cartId, Guid itemId, CancellationToken ct = default)
        => await MutateAsync(cartId, vm =>
        {
            vm.Items.RemoveAll(i => i.ItemId == itemId);
            RecalculateTotals(vm);
        }, "item_removed");

    public async Task SetLockedAsync(TeamCartId cartId, CancellationToken ct = default)
        => await MutateAsync(cartId, vm => { vm.Status = TeamCartStatus.Locked; }, "locked");

    public async Task ApplyTipAsync(TeamCartId cartId, decimal amount, string currency, CancellationToken ct = default)
        => await MutateAsync(cartId, vm =>
        {
            vm.TipAmount = amount;
            vm.TipCurrency = currency;
            RecalculateTotals(vm);
        }, "tip_applied");

    public async Task ApplyCouponAsync(TeamCartId cartId, string couponCode, decimal discountAmount, string currency, CancellationToken ct = default)
        => await MutateAsync(cartId, vm =>
        {
            vm.CouponCode = couponCode;
            vm.DiscountAmount = discountAmount;
            vm.DiscountCurrency = currency;
            RecalculateTotals(vm);
        }, "coupon_applied");

    public async Task RemoveCouponAsync(TeamCartId cartId, CancellationToken ct = default)
        => await MutateAsync(cartId, vm =>
        {
            vm.CouponCode = null;
            vm.DiscountAmount = 0;
            RecalculateTotals(vm);
        }, "coupon_removed");

    public async Task CommitCodAsync(TeamCartId cartId, Guid userId, decimal amount, string currency, CancellationToken ct = default)
        => await MutateAsync(cartId, vm =>
        {
            var m = vm.Members.FirstOrDefault(x => x.UserId == userId);
            if (m is null) return;
            m.PaymentStatus = "CashOnDelivery";
            m.CommittedAmount = amount;
            vm.CashOnDeliveryPortion += amount;
        }, "payment_cod_committed");

    public async Task RecordOnlinePaymentAsync(TeamCartId cartId, Guid userId, decimal amount, string currency, string transactionId, CancellationToken ct = default)
        => await MutateAsync(cartId, vm =>
        {
            var m = vm.Members.FirstOrDefault(x => x.UserId == userId);
            if (m is null) return;
            m.PaymentStatus = "PaidOnline";
            m.CommittedAmount = amount;
            m.OnlineTransactionId = transactionId;
        }, "payment_online_succeeded");

    public async Task RecordOnlinePaymentFailureAsync(TeamCartId cartId, Guid userId, CancellationToken ct = default)
        => await MutateAsync(cartId, vm =>
        {
            var m = vm.Members.FirstOrDefault(x => x.UserId == userId);
            if (m is null) return;
            m.PaymentStatus = "Failed";
            m.OnlineTransactionId = null;
            // Keep committed amount at 0 by default; do not accumulate failures
            m.CommittedAmount = 0;
        }, "payment_online_failed");

    private async Task MutateAsync(TeamCartId cartId, Action<TeamCartViewModel> mutate, string updateType)
    {
        var key = Key(cartId);
        var db = Db;
        const int maxRetries = 5;
        var rand = new Random();

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            var val = await db.StringGetAsync(key);
            if (val.IsNullOrEmpty)
            {
                _logger.LogWarning("TeamCart VM not found in Redis for mutation (Key={Key}, Type={Type})", key, updateType);
                return;
            }

            TeamCartViewModel vm;
            try
            {
                var current = JsonSerializer.Deserialize<RedisVm>(val!, _jsonOptions);
                if (current is null) return;
                vm = MapFrom(current);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize VM for mutation (Key={Key}, Type={Type})", key, updateType);
                return;
            }

            mutate(vm);
            vm.Version++;
            var updated = MapTo(vm);
            var json = JsonSerializer.Serialize(updated, _jsonOptions);

            // Optimistic concurrency: conditional transaction that only succeeds if the stored value
            // is unchanged. This emulates WATCH/MULTI/EXEC using StackExchange.Redis conditions.
            var tran = db.CreateTransaction();
            tran.AddCondition(Condition.StringEqual(key, val));
            _ = tran.StringSetAsync(key, json, Ttl, When.Always, CommandFlags.None);

            var committed = await tran.ExecuteAsync();
            if (committed)
            {
                await PublishUpdateAsync(cartId, updateType);
                return;
            }

            // Conflict: someone else modified the key. Retry with jittered backoff.
            if (attempt < maxRetries)
            {
                await Task.Delay(rand.Next(5, 30));
            }
            else
            {
                _logger.LogWarning("Failed to apply mutation due to concurrent updates after {Attempts} attempts (Key={Key}, Type={Type})", attempt, key, updateType);
            }
        }
    }

    private async Task PublishUpdateAsync(TeamCartId cartId, string updateType)
    {
        try
        {
            var sub = _redis.GetSubscriber();
            var payload = JsonSerializer.Serialize(new { cartId = cartId.Value, type = updateType });
            await sub.PublishAsync(RedisChannel.Literal(_options.UpdatesChannel), payload);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to publish TeamCart update (CartId={CartId}, Type={Type})", cartId.Value, updateType);
        }
    }

    private static void RecalculateTotals(TeamCartViewModel viewModel)
    {
        // Calculate subtotal from all items
        viewModel.Subtotal = viewModel.Items.Sum(item => item.LineTotal);
        
        // Calculate total: subtotal + tip - discount
        viewModel.Total = viewModel.Subtotal + viewModel.TipAmount - viewModel.DiscountAmount;
        
        // Ensure total is not negative
        if (viewModel.Total < 0)
            viewModel.Total = 0;
    }

    #region Mapping
    private static TeamCartViewModel MapFrom(RedisVm s)
    {
        return new TeamCartViewModel
        {
            CartId = TeamCartId.Create(s.CartId),
            RestaurantId = s.RestaurantId,
            Status = s.Status,
            Deadline = s.Deadline,
            ExpiresAt = s.ExpiresAt,
            ShareTokenMasked = s.ShareTokenMasked,
            TipAmount = s.TipAmount,
            TipCurrency = s.TipCurrency,
            CouponCode = s.CouponCode,
            DiscountAmount = s.DiscountAmount,
            DiscountCurrency = s.DiscountCurrency,
            Subtotal = s.Subtotal,
            Currency = s.Currency,
            DeliveryFee = s.DeliveryFee,
            TaxAmount = s.TaxAmount,
            Total = s.Total,
            CashOnDeliveryPortion = s.CashOnDeliveryPortion,
            Version = s.Version,
            Members = s.Members.Select(m => new TeamCartViewModel.Member
            {
                UserId = m.UserId,
                Name = m.Name,
                Role = m.Role,
                PaymentStatus = m.PaymentStatus,
                CommittedAmount = m.CommittedAmount,
                OnlineTransactionId = m.OnlineTransactionId
            }).ToList(),
            Items = s.Items.Select(i => new TeamCartViewModel.Item
            {
                ItemId = i.ItemId,
                AddedByUserId = i.AddedByUserId,
                Name = i.Name,
                Quantity = i.Quantity,
                BasePrice = i.BasePrice,
                LineTotal = i.LineTotal,
                Customizations = i.Customizations.Select(c => new TeamCartViewModel.Customization
                {
                    GroupName = c.GroupName,
                    ChoiceName = c.ChoiceName,
                    PriceAdjustment = c.PriceAdjustment
                }).ToList()
            }).ToList()
        };
    }

    private static RedisVm MapTo(TeamCartViewModel v)
    {
        return new RedisVm
        {
            CartId = v.CartId.Value,
            RestaurantId = v.RestaurantId,
            Status = v.Status,
            Deadline = v.Deadline,
            ExpiresAt = v.ExpiresAt,
            ShareTokenMasked = v.ShareTokenMasked,
            TipAmount = v.TipAmount,
            TipCurrency = v.TipCurrency,
            CouponCode = v.CouponCode,
            DiscountAmount = v.DiscountAmount,
            DiscountCurrency = v.DiscountCurrency,
            Subtotal = v.Subtotal,
            Currency = v.Currency,
            DeliveryFee = v.DeliveryFee,
            TaxAmount = v.TaxAmount,
            Total = v.Total,
            CashOnDeliveryPortion = v.CashOnDeliveryPortion,
            Version = v.Version,
            Members = v.Members.Select(m => new RedisVm.Member
            {
                UserId = m.UserId,
                Name = m.Name,
                Role = m.Role,
                PaymentStatus = m.PaymentStatus,
                CommittedAmount = m.CommittedAmount,
                OnlineTransactionId = m.OnlineTransactionId
            }).ToList(),
            Items = v.Items.Select(i => new RedisVm.Item
            {
                ItemId = i.ItemId,
                AddedByUserId = i.AddedByUserId,
                Name = i.Name,
                Quantity = i.Quantity,
                BasePrice = i.BasePrice,
                LineTotal = i.LineTotal,
                Customizations = i.Customizations.Select(c => new RedisVm.Customization
                {
                    GroupName = c.GroupName,
                    ChoiceName = c.ChoiceName,
                    PriceAdjustment = c.PriceAdjustment
                }).ToList()
            }).ToList()
        };
    }

    private sealed class RedisVm
    {
        public Guid CartId { get; set; }
        public Guid RestaurantId { get; set; }
        public TeamCartStatus Status { get; set; }
        public DateTime? Deadline { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string? ShareTokenMasked { get; set; }
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
        public long Version { get; set; }
        public List<Member> Members { get; set; } = new();
        public List<Item> Items { get; set; } = new();

        public sealed class Member
        {
            public Guid UserId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public string PaymentStatus { get; set; } = "Pending";
            public decimal CommittedAmount { get; set; }
            public string? OnlineTransactionId { get; set; }
        }

        public sealed class Item
        {
            public Guid ItemId { get; set; }
            public Guid AddedByUserId { get; set; }
            public string Name { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public decimal BasePrice { get; set; }
            public decimal LineTotal { get; set; }
            public List<Customization> Customizations { get; set; } = new();
        }

        public sealed class Customization
        {
            public string GroupName { get; set; } = string.Empty;
            public string ChoiceName { get; set; } = string.Empty;
            public decimal PriceAdjustment { get; set; }
        }
    }
    #endregion
}
