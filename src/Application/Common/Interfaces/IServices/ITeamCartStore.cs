using YummyZoom.Application.TeamCarts.Models;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;

namespace YummyZoom.Application.Common.Interfaces.IServices;

/// <summary>
/// Authoritative, Redis-backed TeamCart real-time view store.
/// Only the interface is defined here; implementation lives in Infrastructure.
/// </summary>
public interface ITeamCartStore
{
    Task<TeamCartViewModel?> GetVmAsync(TeamCartId cartId, CancellationToken ct = default);
    Task CreateVmAsync(TeamCartViewModel vm, CancellationToken ct = default);
    Task DeleteVmAsync(TeamCartId cartId, CancellationToken ct = default);

    // Mutation operations (atomic)
    Task AddMemberAsync(TeamCartId cartId, TeamCartViewModel.Member member, CancellationToken ct = default);
    Task AddItemAsync(TeamCartId cartId, TeamCartViewModel.Item item, CancellationToken ct = default);
    Task UpdateItemQuantityAsync(TeamCartId cartId, Guid itemId, int newQuantity, CancellationToken ct = default);
    Task RemoveItemAsync(TeamCartId cartId, Guid itemId, CancellationToken ct = default);
    Task SetLockedAsync(TeamCartId cartId, CancellationToken ct = default);
    Task ApplyTipAsync(TeamCartId cartId, decimal amount, string currency, CancellationToken ct = default);
    Task ApplyCouponAsync(TeamCartId cartId, string couponCode, decimal discountAmount, string currency, CancellationToken ct = default);
    Task RemoveCouponAsync(TeamCartId cartId, CancellationToken ct = default);
    Task CommitCodAsync(TeamCartId cartId, Guid userId, decimal amount, string currency, CancellationToken ct = default);
    Task RecordOnlinePaymentAsync(TeamCartId cartId, Guid userId, decimal amount, string currency, string transactionId, CancellationToken ct = default);
    Task RecordOnlinePaymentFailureAsync(TeamCartId cartId, Guid userId, CancellationToken ct = default);
}
