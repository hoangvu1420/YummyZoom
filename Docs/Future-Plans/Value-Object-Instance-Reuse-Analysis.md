
# Analysis of Value Object Instance Reuse

This document outlines the findings of a scan for potential value object instance reuse issues within the domain layer, which can cause problems with Entity Framework Core's change tracking.

## Summary

The original issue in the `OrderRepository` was caused by the same `Money` instance being used for both the `Order.TotalAmount` and the `PaymentTransaction.Amount`. This was resolved by creating a copy of the `Money` object for the `PaymentTransaction`.

This investigation expands on that issue to identify other potential areas of concern in the `Order`, `TeamCart`, `Coupon`, `CustomizationGroup`, `MenuItem`, `Restaurant`, `Review`, and `User` aggregates.

## `Order` Aggregate

The `Order` aggregate was reviewed, and the fix applied to the `Order.Create` method appears to have resolved the immediate issue. No other instances of value object reuse were found within the `Order` aggregate.

## `TeamCart` Aggregate

The `TeamCart` aggregate was analyzed, and the following potential issue was identified:

### `TeamCart.ApplyTip` Method

The `ApplyTip` method on the `TeamCart` aggregate directly assigns the provided `Money` object to the `TipAmount` property.

```csharp
public Result ApplyTip(UserId requestingUserId, Money tipAmount)
{
    // ... validation ...
    TipAmount = tipAmount; // <-- Potential issue
    return Result.Success();
}
```

If the client code that calls this method reuses the `tipAmount` object for another purpose, it could lead to the same EF Core tracking issue that was observed in the `OrderRepository`.

### Recommendation

To prevent this potential issue, the `ApplyTip` method should be updated to create a copy of the `Money` object before assigning it to the `TipAmount` property.

```csharp
public Result ApplyTip(UserId requestingUserId, Money tipAmount)
{
    // ... validation ...
    TipAmount = tipAmount.Copy(); // <-- FIX
    return Result.Success();
}
```

This ensures that the `TeamCart` aggregate always owns its own instance of the `Money` value object, preventing any potential conflicts with EF Core's change tracking.

## `Coupon` Aggregate

The `Coupon` aggregate has a `MinOrderAmount` property of type `Money`. This property is set in the `Create` and `SetMinimumOrderAmount` methods.

### `Coupon.Create` and `Coupon.SetMinimumOrderAmount` Methods

Both of these methods directly assign the `minOrderAmount` parameter to the `MinOrderAmount` property.

```csharp
// In Coupon.Create
var coupon = new Coupon(
    // ... other parameters
    minOrderAmount, // <-- Potential issue
    // ... other parameters
);

// In Coupon.SetMinimumOrderAmount
public Result SetMinimumOrderAmount(Money? minOrderAmount)
{
    // ... validation ...
    MinOrderAmount = minOrderAmount; // <-- Potential issue
    return Result.Success();
}
```

### Recommendation

Similar to the `TeamCart` aggregate, these methods should be updated to create a copy of the `Money` object.

```csharp
// In Coupon.Create
var coupon = new Coupon(
    // ... other parameters
    minOrderAmount?.Copy(), // <-- FIX
    // ... other parameters
);

// In Coupon.SetMinimumOrderAmount
public Result SetMinimumOrderAmount(Money? minOrderAmount)
{
    // ... validation ...
    MinOrderAmount = minOrderAmount?.Copy(); // <-- FIX
    return Result.Success();
}
```

## `CustomizationGroup` Aggregate

The `CustomizationGroup` aggregate contains a `CustomizationChoice` entity, which in turn has a `PriceAdjustment` property of type `Money`. This `PriceAdjustment` is set via the `CustomizationChoice.Create` method, which is called from the `CustomizationGroup.AddChoice` and `CustomizationGroup.UpdateChoice` methods.

### `CustomizationChoice.Create` Method

The `Create` method of the `CustomizationChoice` entity directly assigns the `priceAdjustment` parameter to the `PriceAdjustment` property.

```csharp
// In CustomizationChoice.Create
public static Result<CustomizationChoice> Create(
    string name,
    Money priceAdjustment,
    // ... other parameters
)
{
    // ... validation ...
    return Result.Success(new CustomizationChoice(
        ChoiceId.CreateUnique(),
        name.Trim(),
        priceAdjustment, // <-- Potential issue
        isDefault,
        displayOrder));
}
```

### Recommendation

To avoid potential issues, the `CustomizationChoice.Create` method should create a copy of the `Money` object.

```csharp
// In CustomizationChoice.Create
public static Result<CustomizationChoice> Create(
    string name,
    Money priceAdjustment,
    // ... other parameters
)
{
    // ... validation ...
    return Result.Success(new CustomizationChoice(
        ChoiceId.CreateUnique(),
        name.Trim(),
        priceAdjustment.Copy(), // <-- FIX
        isDefault,
        displayOrder));
}
```

This will ensure that each `CustomizationChoice` has its own `Money` instance.

## `MenuItem` Aggregate

The `MenuItem` aggregate has a `BasePrice` property of type `Money`. This property is set in the `Create` and `UpdatePrice` methods.

### `MenuItem.Create` and `MenuItem.UpdatePrice` Methods

Both of these methods directly assign the `Money` object to the `BasePrice` property.

```csharp
// In MenuItem.Create
var menuItem = new MenuItem(
    // ... other parameters
    basePrice, // <-- Potential issue
    // ... other parameters
);

// In MenuItem.UpdatePrice
public Result UpdatePrice(Money newPrice)
{
    // ... validation ...
    BasePrice = newPrice; // <-- Potential issue
    return Result.Success();
}
```

### Recommendation

These methods should be updated to create a copy of the `Money` object.

```csharp
// In MenuItem.Create
var menuItem = new MenuItem(
    // ... other parameters
    basePrice.Copy(), // <-- FIX
    // ... other parameters
);

// In MenuItem.UpdatePrice
public Result UpdatePrice(Money newPrice)
{
    // ... validation ...
    BasePrice = newPrice.Copy(); // <-- FIX
    return Result.Success();
}
```

## `Restaurant` Aggregate

The `Restaurant` aggregate uses several value objects (`Address`, `ContactInfo`, `BusinessHours`) that are assigned directly in the `Create` and update methods.

### `Restaurant.Create` and Update Methods

The `Create` method and various update methods (`ChangeLocation`, `UpdateContactInfo`, `UpdateBusinessHours`) directly assign the value object parameters to the aggregate's properties.

```csharp
// In Restaurant.Create
var restaurant = new Restaurant(
    // ...
    location, // <-- Potential issue
    contactInfo, // <-- Potential issue
    businessHours, // <-- Potential issue
    // ...
);

// In Restaurant.ChangeLocation
public Result ChangeLocation(Address location)
{
    // ...
    Location = location; // <-- Potential issue
    // ...
}
```

### Recommendation

To ensure the `Restaurant` aggregate owns its own instances of its value objects, `Copy` methods should be added to the `Address`, `ContactInfo`, and `BusinessHours` value objects, and these `Copy` methods should be called in the `Create` and update methods of the `Restaurant` aggregate.

```csharp
// In Address.cs
public Address Copy() => new Address(Street, City, State, ZipCode, Country);

// In Restaurant.ChangeLocation
public Result ChangeLocation(Address location)
{
    // ...
    Location = location.Copy(); // <-- FIX
    // ...
}
```

## `Review` Aggregate

The `Review` aggregate has a `Rating` value object that is assigned directly in the `Create` method.

### `Review.Create` Method

The `Create` method directly assigns the `rating` parameter to the `Rating` property.

```csharp
// In Review.Create
var review = new Review(
    // ...
    rating, // <-- Potential issue
    // ...
);
```

### Recommendation

To avoid potential issues, a `Copy` method should be added to the `Rating` value object and called in the `Create` method.

```csharp
// In Rating.cs
public Rating Copy() => new Rating(Value);

// In Review.Create
var review = new Review(
    // ...
    rating.Copy(), // <-- FIX
    // ...
);
```

## `User` Aggregate

The `User` aggregate has an `Address` entity that is added via the `AddAddress` method.

### `User.AddAddress` Method

The `AddAddress` method directly adds the `address` parameter to the list of addresses.

```csharp
public Result AddAddress(Address address)
{
    _addresses.Add(address); // <-- Potential issue
    // ...
}
```

### Recommendation

To avoid potential issues, a `Copy` method should be added to the `Address` entity and called in the `AddAddress` method.

```csharp
// In Address.cs (User aggregate)
public Address Copy() => new Address(Id, Street, City, State, ZipCode, Country, Label, DeliveryInstructions);

// In User.AddAddress
public Result AddAddress(Address address)
{
    _addresses.Add(address.Copy()); // <-- FIX
    // ...
}
```
