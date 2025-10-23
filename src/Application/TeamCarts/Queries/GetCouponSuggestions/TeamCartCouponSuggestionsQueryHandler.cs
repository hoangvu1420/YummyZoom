using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Coupons.Queries.Common;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TeamCarts.Queries.GetCouponSuggestions;

public sealed class TeamCartCouponSuggestionsQueryHandler : IRequestHandler<TeamCartCouponSuggestionsQuery, Result<CouponSuggestionsResponse>>
{
    private readonly ITeamCartRepository _teamCartRepository;
    private readonly IFastCouponCheckService _fastCouponCheckService;
    private readonly IUser _currentUser;
    private readonly ILogger<TeamCartCouponSuggestionsQueryHandler> _logger;

    public TeamCartCouponSuggestionsQueryHandler(
        ITeamCartRepository teamCartRepository,
        IFastCouponCheckService fastCouponCheckService,
        IUser currentUser,
        ILogger<TeamCartCouponSuggestionsQueryHandler> logger)
    {
        _teamCartRepository = teamCartRepository;
        _fastCouponCheckService = fastCouponCheckService;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<CouponSuggestionsResponse>> Handle(TeamCartCouponSuggestionsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Get TeamCart
            var teamCartId = Domain.TeamCartAggregate.ValueObjects.TeamCartId.Create(request.TeamCartId);
            var teamCart = await _teamCartRepository.GetByIdAsync(teamCartId, cancellationToken);
            if (teamCart is null)
            {
                return Result.Failure<CouponSuggestionsResponse>(Error.NotFound("TeamCart.NotFound", "TeamCart not found"));
            }

            // 2. Authorization check: user must be a member
            var currentUserId = _currentUser.DomainUserId!.Value;
            if (!teamCart.Members.Any(m => m.UserId.Value == currentUserId))
            {
                _logger.LogWarning("User {UserId} is not a member of TeamCart {TeamCartId}", currentUserId, request.TeamCartId);
                return Result.Failure<CouponSuggestionsResponse>(Error.Validation("TeamCart.NotMember", "You are not a member of this team cart"));
            }

            // 3. Convert TeamCart items to cart items for fast check
            var cartItems = teamCart.Items.Select(item => new CartItem(
                item.Snapshot_MenuItemId.Value,
                item.Snapshot_MenuCategoryId.Value,
                item.Quantity,
                item.Snapshot_BasePriceAtOrder.Amount,
                item.Snapshot_BasePriceAtOrder.Currency)).ToList();

            if (!cartItems.Any())
            {
                _logger.LogInformation("TeamCart {TeamCartId} has no items, returning empty coupon suggestions", request.TeamCartId);
                return Result.Success(CouponSuggestionsResponse.Empty());
            }

            // 4. Use existing fast check service
            var suggestions = await _fastCouponCheckService.GetSuggestionsAsync(
                teamCart.RestaurantId, cartItems, _currentUser.DomainUserId!, cancellationToken);

            _logger.LogInformation("TeamCart coupon suggestions completed for cart {TeamCartId}, restaurant {RestaurantId}. " +
                                 "Cart subtotal: {Subtotal}, suggestions: {SuggestionCount}, best savings: {BestSavings}",
                request.TeamCartId, teamCart.RestaurantId.Value, suggestions.CartSummary.Subtotal,
                suggestions.Suggestions.Count, suggestions.BestDeal?.Savings ?? 0);

            return Result.Success(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting coupon suggestions for TeamCart {TeamCartId}", request.TeamCartId);
            return Result.Failure<CouponSuggestionsResponse>(Error.Failure("TeamCartCouponSuggestions.ProcessingError", "An error occurred while getting coupon suggestions"));
        }
    }
}
