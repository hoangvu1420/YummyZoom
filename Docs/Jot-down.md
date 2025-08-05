The exception in the `OrderRepository.AddAsync`. 
Below is the error log and the traces added before calling `AddAsync`:

```
--- [Domain] Order.Create Factory ---
Creating Order with ID: 0c651f05-4c11-4e78-811b-d60bccc8deb1
  - Initial Status: AwaitingPayment
  - Payment Transactions Count: 1
    -> Transaction ID: b67beba0-fff8-416b-b149-7abd61f4a9fb | Gateway Ref: pi_3RsP6X30kTfMafYR1gl2Og7S
--- End of Order.Create Factory ---
 
 
--- [Repository] OrderRepository.AddAsync ---
Attempting to add Order with ID: 0c651f05-4c11-4e78-811b-d60bccc8deb1
  - Order Status: AwaitingPayment
  - Number of OrderItems: 2
    -> Item ID: b4d02aa3-622a-40f1-a0e1-c459ff0d857a | Name: Pizza Margherita | Quantity: 1
    -> Item ID: 0b061ceb-4022-4cc8-9147-a61acd654004 | Name: Spaghetti Carbonara | Quantity: 1
  - Number of PaymentTransactions: 1
    -> Transaction ID: b67beba0-fff8-416b-b149-7abd61f4a9fb | Status: Pending | RefID: pi_3RsP6X30kTfMafYR1gl2Og7S | Amount: 43.2084 USD
   - Total Amount: 43.2084 USD
--- Handing over to EF Core DbContext... ---
 
warn: Microsoft.EntityFrameworkCore.Update[10001]
      The same entity is being tracked as different entity types 'OrderItem.Snapshot_BasePriceAtOrder#Money' and 'MenuItem.BasePrice#Money' with defining navigations. If a property value changes, it will result in two store changes, which might not be the desired outcome.
warn: Microsoft.EntityFrameworkCore.Update[10001]
      The same entity is being tracked as different entity types 'OrderItem.Snapshot_BasePriceAtOrder#Money' and 'MenuItem.BasePrice#Money' with defining navigations. If a property value changes, it will result in two store changes, which might not be the desired outcome.
warn: Microsoft.EntityFrameworkCore.Update[10001]
      The same entity is being tracked as different entity types 'OrderItem.Snapshot_BasePriceAtOrder#Money' and 'MenuItem.BasePrice#Money' with defining navigations. If a property value changes, it will result in two store changes, which might not be the desired outcome.
warn: Microsoft.EntityFrameworkCore.Update[10001]
      The same entity is being tracked as different entity types 'OrderItem.Snapshot_BasePriceAtOrder#Money' and 'MenuItem.BasePrice#Money' with defining navigations. If a property value changes, it will result in two store changes, which might not be the desired outcome.
warn: Microsoft.EntityFrameworkCore.Update[10001]
      The same entity is being tracked as different entity types 'Order.TotalAmount#Money' and 'PaymentTransaction.Amount#Money' with defining navigations. If a property value changes, it will result in two store changes, which might not be the desired outcome.
warn: Microsoft.EntityFrameworkCore.Update[10001]
      The same entity is being tracked as different entity types 'Order.TotalAmount#Money' and 'PaymentTransaction.Amount#Money' with defining navigations. If a property value changes, it will result in two store changes, which might not be the desired outcome.
!!!!!! EF CORE EXCEPTION in AddAsync !!!!!!
System.InvalidOperationException: Unable to track an entity of type 'Order.TotalAmount#Money' because its primary key property 'Id' is null.
```

---

## Fix: Clone the `Money` value object

The simplest, most idiomatic DDD fix is to give each owned navigation its **own** `Money` instance. For example, if you make `Money` a record with a `Copy()`:

```csharp
public record Money(decimal Amount, string Currency)
{
    public Money Copy() => new Money(Amount, Currency);
}
```

then in the factory, instead of:

```csharp
// ❌ re-using the same instance
var onlinePaymentResult = PaymentTransaction.Create(
    paymentMethodType,
    PaymentTransactionType.Payment,
    totalAmount,         // <-- same instance as Order.TotalAmount
    currentTimestamp,
    paymentGatewayReferenceId);
```

do this:

```csharp
// ✅ give each owned nav its own instance
var txAmount = totalAmount.Copy();
var onlinePaymentResult = PaymentTransaction.Create(
    paymentMethodType,
    PaymentTransactionType.Payment,
    txAmount,            // <-- a fresh clone
    currentTimestamp,
    paymentGatewayReferenceId);
```

And similarly for the COD path:

```csharp
var codAmount = totalAmount.Copy();
var codTransactionResult = PaymentTransaction.Create(
    PaymentMethodType.CashOnDelivery,
    PaymentTransactionType.Payment,
    codAmount,
    currentTimestamp);
```

That way EF Core sees two **different** instances, each tracked under its own owned‐entity type, and the warnings and null‐PK exception go away.

---

With that, the exception when persisting the `Order` is resolved.
However, this is a potential issue in any aggregate that uses the same `Money` instance for multiple owned navigations or any type of Value Object that is used in multiple owned navigations.

We need to scan the Domain layer for any other places where the same instance of a Value Object is used in multiple owned navigations.

I need you to scan the Domain layer one part at a time.
First, look at:
- `TeamCart` aggregate and its related entities in `src/Domain/TeamCartAggregate/`

Make sure you scan for the right patterns: Look for owned navigations that use the same instance of a Value Object. Like `Money` in the example above, the same instance of a Value Object should not be used in both the `Order` and `PaymentTransaction` owned navigations (both has `Money` as a property and if we use the same instance like the `totalAmount` in the example, it will cause issues).
Any other places where a Value Object is directly assigned but it just used in one owned navigation is fine.

Perform a wide search in the Domain layer for similar issue places then give me a report for the current scanned aggregates. Write the report in a new file in Docs\Future-Plans
