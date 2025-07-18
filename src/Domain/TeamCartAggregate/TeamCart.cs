using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate.Entities;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using YummyZoom.Domain.TeamCartAggregate.Events;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.TeamCartAggregate;

/// <summary>
/// Represents a collaborative shopping cart where multiple users can add items
/// before converting to a final Order. This is an Aggregate Root managing the
/// entire lifecycle of team-based ordering.
/// </summary>
public sealed class TeamCart : AggregateRoot<TeamCartId, Guid>, ICreationAuditable
{
    #region Fields

    private readonly List<TeamCartMember> _members = [];
    private readonly List<TeamCartItem> _items = [];
    private readonly List<MemberPayment> _memberPayments = [];

    #endregion

    #region Properties

    // Properties from ICreationAuditable
    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Gets the unique identifier of the team cart.
    /// </summary>
    public new TeamCartId Id { get; private set; }

    /// <summary>
    /// Gets the ID of the restaurant for this team cart.
    /// </summary>
    public RestaurantId RestaurantId { get; private set; }

    /// <summary>
    /// Gets the ID of the user who created and hosts this team cart.
    /// </summary>
    public UserId HostUserId { get; private set; }

    /// <summary>
    /// Gets the current status of the team cart.
    /// </summary>
    public TeamCartStatus Status { get; private set; }

    /// <summary>
    /// Gets the shareable token used for joining this team cart.
    /// </summary>
    public ShareableLinkToken ShareToken { get; private set; }

    /// <summary>
    /// Gets the optional deadline set by the host for ordering.
    /// </summary>
    public DateTime? Deadline { get; private set; }

    /// <summary>
    /// Gets the timestamp when the team cart was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the timestamp when the team cart will automatically expire.
    /// </summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>
    /// Gets a read-only list of members in this team cart.
    /// </summary>
    public IReadOnlyList<TeamCartMember> Members => _members.AsReadOnly();

    /// <summary>
    /// Gets a read-only list of items in this team cart.
    /// </summary>
    public IReadOnlyList<TeamCartItem> Items => _items.AsReadOnly();

    /// <summary>
    /// Gets a read-only list of member payments in this team cart.
    /// </summary>
    public IReadOnlyList<MemberPayment> MemberPayments => _memberPayments.AsReadOnly();
    
    /// <summary>
    /// Gets the tip amount for the order, set by the Host.
    /// </summary>
    public Money TipAmount { get; private set; }

    /// <summary>
    /// Gets the ID of the coupon applied to the team cart.
    /// </summary>
    public CouponId? AppliedCouponId { get; private set; }

    /// <summary>
    /// Gets the discount amount calculated from the applied coupon.
    /// </summary>
    public Money DiscountAmount { get; private set; }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="TeamCart"/> class.
    /// Private constructor enforced by DDD for controlled creation via static factory method.
    /// </summary>
    private TeamCart(
        TeamCartId teamCartId,
        RestaurantId restaurantId,
        UserId hostUserId,
        ShareableLinkToken shareToken,
        DateTime? deadline,
        DateTime createdAt,
        DateTime expiresAt)
        : base(teamCartId)
    {
        Id = teamCartId;
        RestaurantId = restaurantId;
        HostUserId = hostUserId;
        ShareToken = shareToken;
        Deadline = deadline;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        Status = TeamCartStatus.Open;
        
        // Initialize financial properties
        var defaultCurrency = Currencies.Default;
        TipAmount = Money.Zero(defaultCurrency);
        DiscountAmount = Money.Zero(defaultCurrency);
        AppliedCouponId = null;
    }

    /// <summary>
    /// Required for ORM (e.g., Entity Framework Core) and deserialization.
    /// </summary>
#pragma warning disable CS8618
    private TeamCart() { }
#pragma warning restore CS8618

    #endregion

    #region Static Factory Methods

    /// <summary>
    /// Creates a new team cart instance.
    /// </summary>
    /// <param name="hostUserId">The ID of the user creating the team cart.</param>
    /// <param name="restaurantId">The ID of the restaurant for the team cart.</param>
    /// <param name="hostName">The name of the host user for display purposes.</param>
    /// <param name="deadline">Optional deadline for the team cart.</param>
    /// <returns>A <see cref="Result{TeamCart}"/> indicating success or failure.</returns>
    public static Result<TeamCart> Create(
        UserId hostUserId,
        RestaurantId restaurantId,
        string hostName,
        DateTime? deadline = null)
    {
        if (string.IsNullOrWhiteSpace(hostName))
        {
            return Result.Failure<TeamCart>(TeamCartErrors.HostNameRequired);
        }

        var now = DateTime.UtcNow;
        
        // Set default deadline to 24 hours from now if not provided
        var actualDeadline = deadline ?? now.AddHours(24);
        
        // Validate deadline
        if (actualDeadline <= now)
        {
            return Result.Failure<TeamCart>(TeamCartErrors.DeadlineInPast);
        }

        // Create shareable token valid for 24 hours by default
        var shareToken = ShareableLinkToken.CreateUnique(TimeSpan.FromHours(24));
        
        // Team cart expires at the deadline
        var expiresAt = actualDeadline;

        var teamCart = new TeamCart(
            TeamCartId.CreateUnique(),
            restaurantId,
            hostUserId,
            shareToken,
            actualDeadline,
            now,
            expiresAt);

        // Add the host as the first member
        var hostMemberResult = teamCart.AddMember(hostUserId, hostName, MemberRole.Host);
        if (hostMemberResult.IsFailure)
        {
            return Result.Failure<TeamCart>(hostMemberResult.Error);
        }

        // Raise domain event
        teamCart.AddDomainEvent(new TeamCartCreated(teamCart.Id, hostUserId, restaurantId));

        return Result.Success(teamCart);
    }

    #endregion

    #region Public Methods - Member Management

    /// <summary>
    /// Adds a new member to the team cart.
    /// </summary>
    /// <param name="userId">The ID of the user to add.</param>
    /// <param name="name">The display name of the user.</param>
    /// <param name="role">The role of the member (Host or Guest).</param>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    public Result AddMember(UserId userId, string name, MemberRole role = MemberRole.Guest)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(TeamCartErrors.MemberNameRequired);
        }

        if (Status != TeamCartStatus.Open)
        {
            return Result.Failure(TeamCartErrors.CannotAddMembersToClosedCart);
        }

        // Check if member already exists
        if (_members.Any(m => m.UserId == userId))
        {
            return Result.Failure(TeamCartErrors.MemberAlreadyExists);
        }

        // Create new member
        var memberResult = TeamCartMember.Create(userId, name, role);
        if (memberResult.IsFailure)
        {
            return Result.Failure(memberResult.Error);
        }

        _members.Add(memberResult.Value);

        // Raise domain event (only for guests, host addition is part of creation)
        if (role == MemberRole.Guest)
        {
            AddDomainEvent(new MemberJoined(Id, userId, name));
        }

        return Result.Success();
    }

    /// <summary>
    /// Sets or updates the deadline for the team cart.
    /// Only the host can set the deadline.
    /// </summary>
    /// <param name="requestingUserId">The ID of the user making the request.</param>
    /// <param name="deadline">The new deadline.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    public Result SetDeadline(UserId requestingUserId, DateTime deadline)
    {
        if (requestingUserId != HostUserId)
        {
            return Result.Failure(TeamCartErrors.OnlyHostCanSetDeadline);
        }

        if (Status != TeamCartStatus.Open)
        {
            return Result.Failure(TeamCartErrors.CannotModifyClosedCart);
        }

        if (deadline <= DateTime.UtcNow)
        {
            return Result.Failure(TeamCartErrors.DeadlineInPast);
        }

        Deadline = deadline;
        return Result.Success();
    }

    /// <summary>
    /// Checks if the team cart has expired based on its expiration time or deadline.
    /// </summary>
    /// <returns>True if the team cart has expired, false otherwise.</returns>
    public bool IsExpired()
    {
        var now = DateTime.UtcNow;
        return now > ExpiresAt || (Deadline.HasValue && now > Deadline.Value) || ShareToken.IsExpired;
    }

    #endregion

    #region Public Methods - Item Management

    /// <summary>
    /// Adds an item to the team cart with optional customizations.
    /// </summary>
    /// <param name="userId">The ID of the user adding the item.</param>
    /// <param name="menuItemId">The ID of the menu item being added.</param>
    /// <param name="menuCategoryId">The ID of the menu category.</param>
    /// <param name="itemName">The name of the menu item.</param>
    /// <param name="basePrice">The base price of the menu item.</param>
    /// <param name="quantity">The quantity of the item to add.</param>
    /// <param name="customizations">Optional list of customizations for the item.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    public Result AddItem(
        UserId userId,
        MenuItemId menuItemId,
        MenuCategoryId menuCategoryId,
        string itemName,
        Money basePrice,
        int quantity,
        List<TeamCartItemCustomization>? customizations = null)
    {
        // Validate cart status
        if (Status != TeamCartStatus.Open)
        {
            return Result.Failure(TeamCartErrors.CannotAddItemsToClosedCart);
        }

        // Validate user is a member
        if (!_members.Any(m => m.UserId == userId))
        {
            return Result.Failure(TeamCartErrors.UserNotMember);
        }

        // Create the team cart item
        var itemResult = TeamCartItem.Create(
            userId,
            menuItemId,
            menuCategoryId,
            itemName,
            basePrice,
            quantity,
            customizations);

        if (itemResult.IsFailure)
        {
            return Result.Failure(itemResult.Error);
        }

        var item = itemResult.Value;
        _items.Add(item);

        // Raise domain event
        AddDomainEvent(new ItemAddedToTeamCart(
            Id,
            item.Id,
            userId,
            menuItemId,
            quantity));

        return Result.Success();
    }

    #endregion

    #region Public Methods - Status Management

    /// <summary>
    /// Marks the team cart as expired.
    /// </summary>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    public Result MarkAsExpired()
    {
        if (Status == TeamCartStatus.Expired || Status == TeamCartStatus.Converted)
        {
            return Result.Success(); // Already in final state
        }

        Status = TeamCartStatus.Expired;
        AddDomainEvent(new TeamCartExpired(Id));
        return Result.Success();
    }

    #endregion

    #region Public Methods - Validation

    /// <summary>
    /// Validates if a user can join the team cart using the provided token.
    /// </summary>
    /// <param name="token">The token provided for joining.</param>
    /// <returns>A <see cref="Result"/> indicating if the token is valid.</returns>
    public Result ValidateJoinToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Result.Failure(TeamCartErrors.InvalidTokenForJoining);
        }

        if (ShareToken.Value != token)
        {
            return Result.Failure(TeamCartErrors.InvalidTokenForJoining);
        }

        if (ShareToken.IsExpired)
        {
            return Result.Failure(TeamCartErrors.TokenExpired);
        }

        if (Status != TeamCartStatus.Open)
        {
            return Result.Failure(TeamCartErrors.CannotJoinClosedCart);
        }

        if (IsExpired())
        {
            return Result.Failure(TeamCartErrors.TeamCartExpired);
        }

        return Result.Success();
    }

    #endregion

    #region Public Methods - Payment Workflow

    /// <summary>
    /// Initiates the checkout process, transitioning the cart to AwaitingPayments status.
    /// Only the host can initiate checkout.
    /// </summary>
    /// <param name="requestingUserId">The ID of the user requesting to initiate checkout.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    public Result InitiateCheckout(UserId requestingUserId)
    {
        if (requestingUserId != HostUserId)
        {
            return Result.Failure(TeamCartErrors.OnlyHostCanInitiateCheckout);
        }

        if (Status != TeamCartStatus.Open)
        {
            return Result.Failure(TeamCartErrors.CannotModifyClosedCart);
        }

        if (!_items.Any())
        {
            return Result.Failure(TeamCartErrors.CannotInitiateCheckoutWithoutItems);
        }

        if (_members.Count <= 1) // Only host
        {
            return Result.Failure(TeamCartErrors.CannotInitiateCheckoutWithoutMembers);
        }

        Status = TeamCartStatus.AwaitingPayments;
        return Result.Success();
    }

    /// <summary>
    /// Records a member's firm commitment to pay with Cash on Delivery.
    /// This action is reversible if the user decides to pay online instead.
    /// </summary>
    /// <param name="userId">The ID of the user making the payment commitment.</param>
    /// <param name="amount">The amount the member is committing to pay.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    public Result CommitToCashOnDelivery(UserId userId, Money amount)
    {
        // Validate status
        if (Status != TeamCartStatus.AwaitingPayments)
        {
            return Result.Failure(TeamCartErrors.CannotCommitPaymentInCurrentStatus);
        }

        // Validate user is a member
        if (!_members.Any(m => m.UserId == userId))
        {
            return Result.Failure(TeamCartErrors.UserNotMember);
        }

        // Calculate member's total (items they added)
        var memberTotal = CalculateMemberTotal(userId);
        if (amount.Amount != memberTotal.Amount)
        {
            return Result.Failure(TeamCartErrors.InvalidPaymentAmount);
        }

        // Find and remove any previous payment commitment for this user.
        // This allows a user who failed an online payment to switch to COD,
        // or a user who chose COD to switch to an online payment later.
        var existingPayment = _memberPayments.FirstOrDefault(p => p.UserId == userId);
        if (existingPayment is not null)
        {
            _memberPayments.Remove(existingPayment);
        }

        // Create the COD payment commitment
        var paymentResult = MemberPayment.Create(userId, amount, PaymentMethod.CashOnDelivery);
        if (paymentResult.IsFailure)
        {
            return Result.Failure(paymentResult.Error);
        }
        
        _memberPayments.Add(paymentResult.Value);
        
        AddDomainEvent(new MemberCommittedToPayment(Id, userId, PaymentMethod.CashOnDelivery, amount));
        
        CheckAndTransitionToReadyToConfirm(); // This might complete the cart if all others have paid
        
        return Result.Success();
    }

    /// <summary>
    /// Records a successful online payment after it has been confirmed by the payment gateway.
    /// This action replaces any prior commitment (e.g., a COD choice).
    /// </summary>
    /// <param name="userId">The ID of the user who made the payment.</param>
    /// <param name="amount">The amount that was paid.</param>
    /// <param name="transactionId">The transaction ID from the payment processor.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    public Result RecordSuccessfulOnlinePayment(UserId userId, Money amount, string transactionId)
    {
        // Validate status
        if (Status != TeamCartStatus.AwaitingPayments)
        {
            return Result.Failure(TeamCartErrors.CannotCommitPaymentInCurrentStatus);
        }

        // Validate user is a member
        if (!_members.Any(m => m.UserId == userId))
        {
            return Result.Failure(TeamCartErrors.UserNotMember);
        }

        if (string.IsNullOrWhiteSpace(transactionId))
        {
            return Result.Failure(TeamCartErrors.InvalidTransactionId);
        }

        // Calculate member's total (items they added)
        var memberTotal = CalculateMemberTotal(userId);
        if (amount.Amount != memberTotal.Amount)
        {
            return Result.Failure(TeamCartErrors.InvalidPaymentAmount);
        }

        // Find and remove any previous payment commitment for this user
        var existingPayment = _memberPayments.FirstOrDefault(p => p.UserId == userId);
        if (existingPayment is not null)
        {
            _memberPayments.Remove(existingPayment);
        }

        // Create the Online payment record
        var paymentResult = MemberPayment.Create(userId, amount, PaymentMethod.Online);
        if (paymentResult.IsFailure)
        {
            return Result.Failure(paymentResult.Error);
        }
        
        var payment = paymentResult.Value;
        payment.MarkAsPaidOnline(transactionId); // Mark as paid immediately
        _memberPayments.Add(payment);
        
        AddDomainEvent(new OnlinePaymentSucceeded(Id, userId, transactionId, amount));
        
        CheckAndTransitionToReadyToConfirm();
        
        return Result.Success();
    }

    #endregion

    #region Public Methods - Financials

    /// <summary>
    /// Adds or updates the tip amount for the team cart.
    /// Only the host can perform this action.
    /// </summary>
    /// <param name="requestingUserId">The ID of the user attempting to add the tip.</param>
    /// <param name="tipAmount">The amount of the tip to add.</param>
    /// <returns>A Result indicating success or failure.</returns>
    public Result ApplyTip(UserId requestingUserId, Money tipAmount)
    {
        if (requestingUserId != HostUserId)
        {
            return Result.Failure(TeamCartErrors.OnlyHostCanModifyFinancials);
        }

        if (Status is not (TeamCartStatus.AwaitingPayments or TeamCartStatus.ReadyToConfirm))
        {
            return Result.Failure(TeamCartErrors.CannotModifyFinancialsInCurrentStatus);
        }

        if (tipAmount.Amount < 0)
        {
            return Result.Failure(TeamCartErrors.InvalidTip);
        }

        TipAmount = tipAmount;
        return Result.Success();
    }

    /// <summary>
    /// Applies a coupon to the team cart, calculating the discount.
    /// This logic mirrors the Order.ApplyCoupon method.
    /// </summary>
    /// <param name="requestingUserId">The ID of the user applying the coupon (must be the host).</param>
    /// <param name="couponId">The ID of the coupon.</param>
    /// <param name="couponValue">The value and type of the coupon.</param>
    /// <param name="appliesTo">The application scope of the coupon.</param>
    /// <param name="minOrderAmount">The minimum subtotal required.</param>
    /// <returns>A Result indicating success or failure.</returns>
    public Result ApplyCoupon(
        UserId requestingUserId,
        CouponId couponId,
        CouponValue couponValue,
        AppliesTo appliesTo,
        Money? minOrderAmount)
    {
        if (requestingUserId != HostUserId)
        {
            return Result.Failure(TeamCartErrors.OnlyHostCanModifyFinancials);
        }

        if (Status is not (TeamCartStatus.AwaitingPayments or TeamCartStatus.ReadyToConfirm))
        {
            return Result.Failure(TeamCartErrors.CannotModifyFinancialsInCurrentStatus);
        }

        if (AppliedCouponId is not null)
        {
            return Result.Failure(TeamCartErrors.CouponAlreadyApplied);
        }

        var subtotal = CalculateSubtotal();
        if (minOrderAmount is not null && subtotal.Amount < minOrderAmount.Amount)
        {
            return Result.Failure(TeamCartErrors.CouponNotApplicable);
        }

        var discountResult = CalculateDiscount(subtotal, couponValue, appliesTo);
        if (discountResult.IsFailure)
        {
            return Result.Failure(discountResult.Error);
        }

        DiscountAmount = discountResult.Value;
        AppliedCouponId = couponId;
        return Result.Success();
    }

    /// <summary>
    /// Removes the currently applied coupon.
    /// </summary>
    /// <param name="requestingUserId">The ID of the user requesting to remove the coupon (must be the host).</param>
    /// <returns>A Result indicating success or failure.</returns>
    public Result RemoveCoupon(UserId requestingUserId)
    {
        if (requestingUserId != HostUserId)
        {
            return Result.Failure(TeamCartErrors.OnlyHostCanModifyFinancials);
        }

        if (AppliedCouponId is null)
        {
            return Result.Success();
        }

        DiscountAmount = Money.Zero(DiscountAmount.Currency);
        AppliedCouponId = null;
        return Result.Success();
    }

#endregion

#region Private Helper Methods

    /// <summary>
    /// Calculates the total amount for items added by a specific member.
    /// </summary>
    /// <param name="userId">The ID of the member.</param>
    /// <returns>The total amount for the member's items.</returns>
    private Money CalculateMemberTotal(UserId userId)
    {
        var memberItems = _items.Where(item => item.AddedByUserId == userId);
        var total = memberItems.Sum(item => item.LineItemTotal.Amount);
        return new Money(total, Currencies.Default);
    }

    /// <summary>
    /// Checks if all members have committed to payment and transitions to ReadyToConfirm if so.
    /// </summary>
    private void CheckAndTransitionToReadyToConfirm()
    {
        if (Status != TeamCartStatus.AwaitingPayments)
        {
            return;
        }

        // Check if all members have payment commitments
        var allMembersCommitted = _members.All(member => 
            _memberPayments.Any(payment => payment.UserId == member.UserId));

        if (!allMembersCommitted)
        {
            return;
        }

        // Check if all online payments are complete
        var allOnlinePaymentsComplete = _memberPayments
            .Where(p => p.Method == PaymentMethod.Online)
            .All(p => p.Status == TeamCartAggregate.Enums.PaymentStatus.PaidOnline);

        if (!allOnlinePaymentsComplete)
        {
            return;
        }

        // All conditions met, transition to ReadyToConfirm
        Status = TeamCartStatus.ReadyToConfirm;

        // Calculate totals
        var totalAmount = new Money(_memberPayments.Sum(p => p.Amount.Amount), Currencies.Default);
        var cashAmount = new Money(_memberPayments
            .Where(p => p.Method == PaymentMethod.CashOnDelivery)
            .Sum(p => p.Amount.Amount), Currencies.Default);

        // Raise domain event
        AddDomainEvent(new TeamCartReadyForConfirmation(Id, totalAmount, cashAmount));
    }

    /// <summary>
    /// Marks the TeamCart as converted.
    /// This should be called by the Domain Service after successfully creating the Order.
    /// </summary>
    public Result MarkAsConverted()
    {
        if (Status != TeamCartStatus.ReadyToConfirm)
        {
            return Result.Failure(TeamCartErrors.InvalidStatusForConversion);
        }

        Status = TeamCartStatus.Converted;
        return Result.Success();
    }
    
    /// <summary>
    /// Calculates the subtotal of all items in the team cart.
    /// </summary>
    /// <returns>The subtotal as a Money value.</returns>
    private Money CalculateSubtotal()
    {
        if (!_items.Any())
        {
            return Money.Zero(Currencies.Default);
        }
        
        var currency = _items.First().Snapshot_BasePriceAtOrder.Currency;
        var totalAmount = _items.Sum(item => item.LineItemTotal.Amount);
        return new Money(totalAmount, currency);
    }
    
    /// <summary>
    /// Calculates the discount amount based on the coupon value and application scope.
    /// </summary>
    /// <param name="subtotal">The subtotal to base the discount on.</param>
    /// <param name="couponValue">The value and type of the coupon.</param>
    /// <param name="appliesTo">The scope and criteria for coupon application.</param>
    /// <returns>A Result containing the calculated discount amount.</returns>
    private Result<Money> CalculateDiscount(Money subtotal, CouponValue couponValue, AppliesTo appliesTo)
    {
        // Get the base amount for discount calculation based on the coupon's scope
        var discountBaseAmount = GetDiscountBaseAmount(couponValue, appliesTo);
        if (discountBaseAmount <= 0)
        {
            return Result.Failure<Money>(TeamCartErrors.CouponNotApplicable);
        }

        var discountBaseMoney = new Money(discountBaseAmount, subtotal.Currency);
        Money discount;
        
        switch (couponValue.Type)
        {
            case CouponType.Percentage:
                discount = discountBaseMoney * (couponValue.PercentageValue!.Value / 100m);
                break;
                
            case CouponType.FixedAmount:
                // Discount cannot be more than the value of the items it applies to
                var fixedAmount = couponValue.FixedAmountValue!;
                discount = new Money(Math.Min(discountBaseMoney.Amount, fixedAmount.Amount), subtotal.Currency);
                break;
                
            case CouponType.FreeItem:
                // For free item coupons, the discount is the price of one unit of the cheapest matching item
                discount = discountBaseMoney;
                break;
                
            default:
                return Result.Failure<Money>(TeamCartErrors.CouponNotApplicable);
        }

        // Ensure discount doesn't exceed subtotal
        if (discount.Amount > subtotal.Amount)
        {
            discount = subtotal;
        }

        return Result.Success(discount);
    }
    
    /// <summary>
    /// Calculates the base amount on which the coupon discount will be applied.
    /// </summary>
    /// <param name="couponValue">The value and type of the coupon.</param>
    /// <param name="appliesTo">The scope and criteria for coupon application.</param>
    /// <returns>The decimal amount to base the discount calculation on.</returns>
    private decimal GetDiscountBaseAmount(CouponValue couponValue, AppliesTo appliesTo)
    {
        // For FreeItem coupons, calculate based on the specific free item
        if (couponValue is { Type: CouponType.FreeItem, FreeItemValue: not null })
        {
            return GetFreeItemDiscountAmount(couponValue.FreeItemValue);
        }

        switch (appliesTo.Scope)
        {
            case CouponScope.WholeOrder:
                return _items.Sum(item => item.LineItemTotal.Amount);

            case CouponScope.SpecificItems:
                return _items
                    .Where(item => appliesTo.ItemIds.Contains(item.Snapshot_MenuItemId))
                    .Sum(item => item.LineItemTotal.Amount);

            case CouponScope.SpecificCategories:
                return _items
                    .Where(item => appliesTo.CategoryIds.Contains(item.Snapshot_MenuCategoryId))
                    .Sum(item => item.LineItemTotal.Amount);

            default:
                return 0;
        }
    }
    
    /// <summary>
    /// Calculates the discount amount for a free item coupon.
    /// </summary>
    /// <param name="freeItemId">The ID of the menu item that is free.</param>
    /// <returns>The decimal value representing the discount for the free item.</returns>
    private decimal GetFreeItemDiscountAmount(MenuItemId freeItemId)
    {
        // Find all items that match the free item
        var matchingItems = _items
            .Where(item => item.Snapshot_MenuItemId == freeItemId)
            .ToList();

        if (!matchingItems.Any())
        {
            return 0; // No matching items found
        }

        // For free item coupons, typically apply to the cheapest occurrence
        // Calculate per-unit price
        var cheapestItem = matchingItems
            .OrderBy(item => item.LineItemTotal.Amount / item.Quantity)
            .First();

        // Return the price for one unit of the cheapest matching item
        return cheapestItem.LineItemTotal.Amount / cheapestItem.Quantity;
    }

    #endregion

}
