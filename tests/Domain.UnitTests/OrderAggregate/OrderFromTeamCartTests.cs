namespace YummyZoom.Domain.UnitTests.OrderAggregate;

// [TestFixture]
// public class OrderFromTeamCartTests : OrderTestHelpers
// {
//     private TeamCartId _teamCartId = null!;
//     private List<PaymentTransaction> _paymentTransactions = null!;
//     private List<PaymentTransaction> _paymentTransactionsWithWrongTotal = null!;
//
//     [SetUp]
//     public void SetUp()
//     {
//         _teamCartId = TeamCartId.CreateUnique();
//         _paymentTransactions = CreatePaymentTransactionsWithPaidByUserId();
//         _paymentTransactionsWithWrongTotal = CreatePaymentTransactionsWithWrongTotal();
//     }
//
//     #region Enhanced Order.Create() Tests
//
//     [Test]
//     public void Create_WithTeamCartId_ShouldSetSourceTeamCartId()
//     {
//         // Arrange
//         // Calculate subtotal from order items
//         var subtotal = new Money(DefaultOrderItems.Sum(item => item.LineItemTotal.Amount), Currencies.Default);
//         
//         // Calculate total amount
//         var totalAmount = subtotal - DefaultDiscountAmount + DefaultDeliveryFee + DefaultTipAmount + DefaultTaxAmount;
//         
//         // Create payment transactions that match the total amount
//         var paymentTransactions = CreatePaymentTransactionsWithTotal(totalAmount);
//         
//         // Act
//         var result = Order.Create(
//             DefaultCustomerId,
//             DefaultRestaurantId,
//             DefaultDeliveryAddress,
//             DefaultOrderItems,
//             DefaultSpecialInstructions,
//             subtotal,
//             DefaultDiscountAmount,
//             DefaultDeliveryFee,
//             DefaultTipAmount,
//             DefaultTaxAmount,
//             totalAmount,
//             paymentTransactions,
//             null,
//             OrderStatus.Placed,
//             null,
//             _teamCartId);
//
//         // Assert
//         result.ShouldBeSuccessful();
//         var order = result.Value;
//         
//         order.SourceTeamCartId.Should().Be(_teamCartId);
//     }
//
//     [Test]
//     public void Create_WithPaymentTransactions_ShouldAddTransactions()
//     {
//         // Arrange
//         // Calculate subtotal from order items
//         var subtotal = new Money(DefaultOrderItems.Sum(item => item.LineItemTotal.Amount), Currencies.Default);
//         
//         // Calculate total amount
//         var totalAmount = subtotal - DefaultDiscountAmount + DefaultDeliveryFee + DefaultTipAmount + DefaultTaxAmount;
//
//         // Act
//         var result = Order.Create(
//             DefaultCustomerId,
//             DefaultRestaurantId,
//             DefaultDeliveryAddress,
//             DefaultOrderItems,
//             DefaultSpecialInstructions,
//             subtotal,
//             DefaultDiscountAmount,
//             DefaultDeliveryFee,
//             DefaultTipAmount,
//             DefaultTaxAmount,
//             totalAmount,
//             _paymentTransactions,
//             null);
//
//         // Assert
//         result.ShouldBeSuccessful();
//         var order = result.Value;
//         
//         order.PaymentTransactions.Should().HaveCount(_paymentTransactions.Count);
//         order.PaymentTransactions.Should().BeEquivalentTo(_paymentTransactions);
//     }
//
//     [Test]
//     public void Create_WithTeamCartIdAndPaymentTransactions_ShouldSetBothCorrectly()
//     {
//         // Arrange
//         // Calculate subtotal from order items
//         var subtotal = new Money(DefaultOrderItems.Sum(item => item.LineItemTotal.Amount), Currencies.Default);
//         
//         // Calculate total amount
//         var totalAmount = subtotal - DefaultDiscountAmount + DefaultDeliveryFee + DefaultTipAmount + DefaultTaxAmount;
//
//         // Act
//         var result = Order.Create(
//             DefaultCustomerId,
//             DefaultRestaurantId,
//             DefaultDeliveryAddress,
//             DefaultOrderItems,
//             DefaultSpecialInstructions,
//             subtotal,
//             DefaultDiscountAmount,
//             DefaultDeliveryFee,
//             DefaultTipAmount,
//             DefaultTaxAmount,
//             totalAmount,
//             _paymentTransactions,
//             null,
//             OrderStatus.Placed,
//             null,
//             _teamCartId);
//
//         // Assert
//         result.ShouldBeSuccessful();
//         var order = result.Value;
//         
//         order.SourceTeamCartId.Should().Be(_teamCartId);
//         order.PaymentTransactions.Should().HaveCount(_paymentTransactions.Count);
//         order.PaymentTransactions.Should().BeEquivalentTo(_paymentTransactions);
//     }
//
//     [Test]
//     public void Create_WithNullTeamCartId_ShouldLeaveSourceTeamCartIdNull()
//     {
//         // Arrange
//         // Calculate subtotal from order items
//         var subtotal = new Money(DefaultOrderItems.Sum(item => item.LineItemTotal.Amount), Currencies.Default);
//         
//         // Calculate total amount
//         var totalAmount = subtotal - DefaultDiscountAmount + DefaultDeliveryFee + DefaultTipAmount + DefaultTaxAmount;
//         
//         // Create payment transactions that match the total amount
//         var paymentTransactions = CreatePaymentTransactionsWithTotal(totalAmount);
//
//         // Act
//         var result = Order.Create(
//             DefaultCustomerId,
//             DefaultRestaurantId,
//             DefaultDeliveryAddress,
//             DefaultOrderItems,
//             DefaultSpecialInstructions,
//             subtotal,
//             DefaultDiscountAmount,
//             DefaultDeliveryFee,
//             DefaultTipAmount,
//             DefaultTaxAmount,
//             totalAmount,
//             paymentTransactions,
//             null,
//             OrderStatus.Placed,
//             null,
//             null); // Null TeamCartId
//
//         // Assert
//         result.ShouldBeSuccessful();
//         var order = result.Value;
//         
//         order.SourceTeamCartId.Should().BeNull();
//     }
//
//     [Test]
//     public void Create_WithEmptyPaymentTransactions_ShouldNotAddTransactions()
//     {
//         // Arrange
//         // Calculate subtotal from order items
//         var subtotal = new Money(DefaultOrderItems.Sum(item => item.LineItemTotal.Amount), Currencies.Default);
//         
//         // Calculate total amount
//         var totalAmount = subtotal - DefaultDiscountAmount + DefaultDeliveryFee + DefaultTipAmount + DefaultTaxAmount;
//
//         // Act
//         var result = Order.Create(
//             DefaultCustomerId,
//             DefaultRestaurantId,
//             DefaultDeliveryAddress,
//             DefaultOrderItems,
//             DefaultSpecialInstructions,
//             subtotal,
//             DefaultDiscountAmount,
//             DefaultDeliveryFee,
//             DefaultTipAmount,
//             DefaultTaxAmount,
//             totalAmount,
//             new List<PaymentTransaction>(), // Empty payment transactions
//             null,
//             OrderStatus.PendingPayment, // Use PendingPayment to avoid payment validation
//             DefaultPaymentIntentId);
//
//         // Assert
//         result.ShouldBeSuccessful();
//         var order = result.Value;
//         
//         order.PaymentTransactions.Should().BeEmpty();
//     }
//
//     [Test]
//     public void Create_WithNullPaymentTransactions_ShouldNotAddTransactions()
//     {
//         // Arrange
//         // Calculate subtotal from order items
//         var subtotal = new Money(DefaultOrderItems.Sum(item => item.LineItemTotal.Amount), Currencies.Default);
//         
//         // Calculate total amount
//         var totalAmount = subtotal - DefaultDiscountAmount + DefaultDeliveryFee + DefaultTipAmount + DefaultTaxAmount;
//
//         // Act
//         var result = Order.Create(
//             DefaultCustomerId,
//             DefaultRestaurantId,
//             DefaultDeliveryAddress,
//             DefaultOrderItems,
//             DefaultSpecialInstructions,
//             subtotal,
//             DefaultDiscountAmount,
//             DefaultDeliveryFee,
//             DefaultTipAmount,
//             DefaultTaxAmount,
//             totalAmount,
//             null, // Null payment transactions
//             null,
//             OrderStatus.PendingPayment, // Use PendingPayment to avoid payment validation
//             DefaultPaymentIntentId);
//
//         // Assert
//         result.ShouldBeSuccessful();
//         var order = result.Value;
//         
//         order.PaymentTransactions.Should().BeEmpty();
//     }
//
//     #endregion
//
//     #region Payment Transaction Validation Tests
//
//     [Test]
//     public void Create_WithMismatchedPaymentTotal_ShouldFailWithPaymentMismatchError()
//     {
//         // Arrange
//         // Calculate subtotal from order items
//         var subtotal = new Money(DefaultOrderItems.Sum(item => item.LineItemTotal.Amount), Currencies.Default);
//         
//         // Calculate total amount
//         var totalAmount = subtotal - DefaultDiscountAmount + DefaultDeliveryFee + DefaultTipAmount + DefaultTaxAmount;
//
//         // Act
//         var result = Order.Create(
//             DefaultCustomerId,
//             DefaultRestaurantId,
//             DefaultDeliveryAddress,
//             DefaultOrderItems,
//             DefaultSpecialInstructions,
//             subtotal,
//             DefaultDiscountAmount,
//             DefaultDeliveryFee,
//             DefaultTipAmount,
//             DefaultTaxAmount,
//             totalAmount,
//             _paymentTransactionsWithWrongTotal, // Mismatched payment total
//             null);
//
//         // Assert
//         result.ShouldBeFailure();
//         result.Error.Should().Be(OrderErrors.PaymentMismatch);
//     }
//
//     [Test]
//     public void Create_WithCorrectPaymentTotal_ShouldSucceed()
//     {
//         // Arrange
//         // Calculate subtotal from order items
//         var subtotal = new Money(DefaultOrderItems.Sum(item => item.LineItemTotal.Amount), Currencies.Default);
//         
//         // Calculate total amount
//         var totalAmount = subtotal - DefaultDiscountAmount + DefaultDeliveryFee + DefaultTipAmount + DefaultTaxAmount;
//
//         // Act
//         var result = Order.Create(
//             DefaultCustomerId,
//             DefaultRestaurantId,
//             DefaultDeliveryAddress,
//             DefaultOrderItems,
//             DefaultSpecialInstructions,
//             subtotal,
//             DefaultDiscountAmount,
//             DefaultDeliveryFee,
//             DefaultTipAmount,
//             DefaultTaxAmount,
//             totalAmount,
//             _paymentTransactions,
//             null);
//
//         // Assert
//         result.ShouldBeSuccessful();
//         var order = result.Value;
//         
//         var paymentTotal = order.PaymentTransactions.Sum(pt => pt.Amount.Amount);
//         paymentTotal.Should().Be(order.TotalAmount.Amount);
//     }
//
//     [Test]
//     public void Create_WithPartialPaymentTransactions_ShouldFailWithPaymentMismatchError()
//     {
//         // Arrange
//         // Calculate subtotal from order items
//         var subtotal = new Money(DefaultOrderItems.Sum(item => item.LineItemTotal.Amount), Currencies.Default);
//         
//         // Calculate total amount
//         var totalAmount = subtotal - DefaultDiscountAmount + DefaultDeliveryFee + DefaultTipAmount + DefaultTaxAmount;
//         
//         var partialPayments = new List<PaymentTransaction>
//         {
//             _paymentTransactions.First() // Only one payment, not covering full total
//         };
//
//         // Act
//         var result = Order.Create(
//             DefaultCustomerId,
//             DefaultRestaurantId,
//             DefaultDeliveryAddress,
//             DefaultOrderItems,
//             DefaultSpecialInstructions,
//             subtotal,
//             DefaultDiscountAmount,
//             DefaultDeliveryFee,
//             DefaultTipAmount,
//             DefaultTaxAmount,
//             totalAmount,
//             partialPayments,
//             null);
//
//         // Assert
//         result.ShouldBeFailure();
//         result.Error.Should().Be(OrderErrors.PaymentMismatch);
//     }
//
//     #endregion
//
//     #region Order Properties Tests
//
//     [Test]
//     public void Create_WithTeamCartData_ShouldMaintainAllExistingProperties()
//     {
//         // Arrange
//         // Calculate subtotal from order items
//         var subtotal = new Money(DefaultOrderItems.Sum(item => item.LineItemTotal.Amount), Currencies.Default);
//         
//         // Calculate total amount
//         var totalAmount = subtotal - DefaultDiscountAmount + DefaultDeliveryFee + DefaultTipAmount + DefaultTaxAmount;
//
//         // Act
//         var result = Order.Create(
//             DefaultCustomerId,
//             DefaultRestaurantId,
//             DefaultDeliveryAddress,
//             DefaultOrderItems,
//             DefaultSpecialInstructions,
//             subtotal,
//             DefaultDiscountAmount,
//             DefaultDeliveryFee,
//             DefaultTipAmount,
//             DefaultTaxAmount,
//             totalAmount,
//             _paymentTransactions,
//             null,
//             OrderStatus.Placed,
//             null,
//             _teamCartId);
//
//         // Assert
//         result.ShouldBeSuccessful();
//         var order = result.Value;
//         
//         order.CustomerId.Should().Be(DefaultCustomerId);
//         order.RestaurantId.Should().Be(DefaultRestaurantId);
//         order.DeliveryAddress.Should().Be(DefaultDeliveryAddress);
//         order.SpecialInstructions.Should().Be(DefaultSpecialInstructions);
//         order.DiscountAmount.Should().Be(DefaultDiscountAmount);
//         order.DeliveryFee.Should().Be(DefaultDeliveryFee);
//         order.TipAmount.Should().Be(DefaultTipAmount);
//         order.TaxAmount.Should().Be(DefaultTaxAmount);
//         order.OrderItems.Should().BeEquivalentTo(DefaultOrderItems);
//         order.SourceTeamCartId.Should().Be(_teamCartId);
//         order.PaymentTransactions.Should().BeEquivalentTo(_paymentTransactions);
//     }
//
//     [Test]
//     public void Create_WithTeamCartData_ShouldCalculateCorrectTotalAmount()
//     {
//         // Arrange
//         // Calculate subtotal from order items
//         var subtotal = new Money(DefaultOrderItems.Sum(item => item.LineItemTotal.Amount), Currencies.Default);
//         
//         // Calculate total amount
//         var totalAmount = subtotal - DefaultDiscountAmount + DefaultDeliveryFee + DefaultTipAmount + DefaultTaxAmount;
//
//         // Act
//         var result = Order.Create(
//             DefaultCustomerId,
//             DefaultRestaurantId,
//             DefaultDeliveryAddress,
//             DefaultOrderItems,
//             DefaultSpecialInstructions,
//             subtotal,
//             DefaultDiscountAmount,
//             DefaultDeliveryFee,
//             DefaultTipAmount,
//             DefaultTaxAmount,
//             totalAmount,
//             _paymentTransactions,
//             null,
//             OrderStatus.Placed,
//             null,
//             _teamCartId);
//
//         // Assert
//         result.ShouldBeSuccessful();
//         var order = result.Value;
//         
//         order.TotalAmount.Amount.Should().Be(totalAmount.Amount);
//     }
//
//     [Test]
//     public void Create_WithTeamCartData_ShouldSetCorrectOrderStatus()
//     {
//         // Arrange
//         // Calculate subtotal from order items
//         var subtotal = new Money(DefaultOrderItems.Sum(item => item.LineItemTotal.Amount), Currencies.Default);
//         
//         // Calculate total amount
//         var totalAmount = subtotal - DefaultDiscountAmount + DefaultDeliveryFee + DefaultTipAmount + DefaultTaxAmount;
//         
//         var initialStatus = OrderStatus.Placed;
//
//         // Act
//         var result = Order.Create(
//             DefaultCustomerId,
//             DefaultRestaurantId,
//             DefaultDeliveryAddress,
//             DefaultOrderItems,
//             DefaultSpecialInstructions,
//             subtotal,
//             DefaultDiscountAmount,
//             DefaultDeliveryFee,
//             DefaultTipAmount,
//             DefaultTaxAmount,
//             totalAmount,
//             _paymentTransactions,
//             null,
//             initialStatus,
//             null,
//             _teamCartId);
//
//         // Assert
//         result.ShouldBeSuccessful();
//         var order = result.Value;
//         
//         order.Status.Should().Be(initialStatus);
//     }
//
//     #endregion
//
//     #region PaymentTransaction Properties Tests
//
//     [Test]
//     public void Create_WithPaymentTransactions_ShouldPreservePaidByUserId()
//     {
//         // Arrange
//         // Calculate subtotal from order items
//         var subtotal = new Money(DefaultOrderItems.Sum(item => item.LineItemTotal.Amount), Currencies.Default);
//         
//         // Calculate total amount
//         var totalAmount = subtotal - DefaultDiscountAmount + DefaultDeliveryFee + DefaultTipAmount + DefaultTaxAmount;
//
//         // Act
//         var result = Order.Create(
//             DefaultCustomerId,
//             DefaultRestaurantId,
//             DefaultDeliveryAddress,
//             DefaultOrderItems,
//             DefaultSpecialInstructions,
//             subtotal,
//             DefaultDiscountAmount,
//             DefaultDeliveryFee,
//             DefaultTipAmount,
//             DefaultTaxAmount,
//             totalAmount,
//             _paymentTransactions,
//             null);
//
//         // Assert
//         result.ShouldBeSuccessful();
//         var order = result.Value;
//         
//         var firstTransaction = order.PaymentTransactions.First();
//         firstTransaction.PaidByUserId.Should().NotBeNull();
//         firstTransaction.PaidByUserId.Should().Be(_paymentTransactions.First().PaidByUserId);
//     }
//
//     [Test]
//     public void Create_WithPaymentTransactions_ShouldPreserveAllTransactionProperties()
//     {
//         // Arrange
//         // Calculate subtotal from order items
//         var subtotal = new Money(DefaultOrderItems.Sum(item => item.LineItemTotal.Amount), Currencies.Default);
//         
//         // Calculate total amount
//         var totalAmount = subtotal - DefaultDiscountAmount + DefaultDeliveryFee + DefaultTipAmount + DefaultTaxAmount;
//
//         // Act
//         var result = Order.Create(
//             DefaultCustomerId,
//             DefaultRestaurantId,
//             DefaultDeliveryAddress,
//             DefaultOrderItems,
//             DefaultSpecialInstructions,
//             subtotal,
//             DefaultDiscountAmount,
//             DefaultDeliveryFee,
//             DefaultTipAmount,
//             DefaultTaxAmount,
//             totalAmount,
//             _paymentTransactions,
//             null);
//
//         // Assert
//         result.ShouldBeSuccessful();
//         var order = result.Value;
//         
//         for (int i = 0; i < _paymentTransactions.Count; i++)
//         {
//             var expected = _paymentTransactions[i];
//             var actual = order.PaymentTransactions.ElementAt(i);
//             
//             actual.PaymentMethodType.Should().Be(expected.PaymentMethodType);
//             actual.Amount.Should().Be(expected.Amount);
//             actual.Type.Should().Be(expected.Type);
//             actual.PaidByUserId.Should().Be(expected.PaidByUserId);
//             actual.PaymentMethodDisplay.Should().Be(expected.PaymentMethodDisplay);
//             actual.PaymentGatewayReferenceId.Should().Be(expected.PaymentGatewayReferenceId);
//         }
//     }
//
//     #endregion
//
//     #region PendingPayment Tests
//
//     [Test]
//     public void Create_WithPendingPaymentStatus_ShouldNotValidatePaymentTransactions()
//     {
//         // Arrange
//         // Calculate subtotal from order items
//         var subtotal = new Money(DefaultOrderItems.Sum(item => item.LineItemTotal.Amount), Currencies.Default);
//         
//         // Calculate total amount
//         var totalAmount = subtotal - DefaultDiscountAmount + DefaultDeliveryFee + DefaultTipAmount + DefaultTaxAmount;
//         
//         // Create payment transactions that don't match the total amount
//         var mismatchedPayments = _paymentTransactionsWithWrongTotal;
//
//         // Act
//         var result = Order.Create(
//             DefaultCustomerId,
//             DefaultRestaurantId,
//             DefaultDeliveryAddress,
//             DefaultOrderItems,
//             DefaultSpecialInstructions,
//             subtotal,
//             DefaultDiscountAmount,
//             DefaultDeliveryFee,
//             DefaultTipAmount,
//             DefaultTaxAmount,
//             totalAmount,
//             mismatchedPayments,
//             null,
//             OrderStatus.PendingPayment, // PendingPayment status
//             DefaultPaymentIntentId);
//
//         // Assert
//         result.ShouldBeSuccessful();
//         var order = result.Value;
//         
//         order.Status.Should().Be(OrderStatus.PendingPayment);
//         // Payment transactions should be cleared for pending payment status
//         order.PaymentTransactions.Should().BeEmpty();
//     }
//
//     [Test]
//     public void Create_WithPendingPaymentStatusAndPaymentTransactions_ShouldClearTransactions()
//     {
//         // Arrange
//         // Calculate subtotal from order items
//         var subtotal = new Money(DefaultOrderItems.Sum(item => item.LineItemTotal.Amount), Currencies.Default);
//         
//         // Calculate total amount
//         var totalAmount = subtotal - DefaultDiscountAmount + DefaultDeliveryFee + DefaultTipAmount + DefaultTaxAmount;
//
//         // Act
//         var result = Order.Create(
//             DefaultCustomerId,
//             DefaultRestaurantId,
//             DefaultDeliveryAddress,
//             DefaultOrderItems,
//             DefaultSpecialInstructions,
//             subtotal,
//             DefaultDiscountAmount,
//             DefaultDeliveryFee,
//             DefaultTipAmount,
//             DefaultTaxAmount,
//             totalAmount,
//             _paymentTransactions, // Valid payment transactions
//             null,
//             OrderStatus.PendingPayment, // PendingPayment status
//             DefaultPaymentIntentId);
//
//         // Assert
//         result.ShouldBeSuccessful();
//         var order = result.Value;
//         
//         order.Status.Should().Be(OrderStatus.PendingPayment);
//         // Payment transactions should be cleared for pending payment status
//         order.PaymentTransactions.Should().BeEmpty();
//     }
//
//     #endregion
// }
