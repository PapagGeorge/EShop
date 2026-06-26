# DDD Domain Layer in .NET — Technical Implementation Guide

## Overview

This document explains how we implement the **Domain Layer** in a Domain-Driven Design (DDD) architecture using .NET 8 and C#. The examples are taken directly from the EShop codebase.

---

## 1. Encapsulation — Protecting Entity State

The core principle of a DDD entity is that it **owns and controls its own state**. No external code should be able to modify an entity's properties directly.

### Private Setters

All properties use `private set` so they can only be modified from within the entity itself:

```csharp
public Guid UserId { get; private set; }
public OrderStatus Status { get; private set; }
public decimal TotalAmount { get; private set; }
public Address ShippingAddress { get; private set; } = default!;
public DateTime? UpdatedAt { get; private set; }
```

External code can **read** these properties but cannot write to them:

```csharp
var id = order.UserId;       // ✅ allowed — reading
order.UserId = someGuid;     // ❌ compile error — private set
```

---

## 2. Collections — Two Layers of Protection

Collections require extra care because a `public List<T>` exposes mutation methods (`Add`, `Remove`, `Clear`) even if the property itself has a private setter.

We solve this with two separate members:

```csharp
private readonly List<OrderItem> _items = new();
public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
```

### The `private readonly` field

The `readonly` keyword prevents the **reference** from being reassigned after initialization:

```csharp
_items = new List<OrderItem>(); // ❌ compile error — readonly
_items = someOtherList;         // ❌ compile error — readonly
```

The entity itself can still modify the **contents** of the list:

```csharp
_items.Add(item);    // ✅ allowed — used internally by AddItem()
_items.Remove(item); // ✅ allowed — used internally if needed
```

### The `IReadOnlyCollection<T>` property

`AsReadOnly()` wraps the list in a read-only view. External code receives this wrapper and cannot call mutation methods on it:

```csharp
order.Items.Add(fakeItem); // ❌ compile error — IReadOnlyCollection has no Add
order.Items.Clear();        // ❌ compile error
foreach (var i in order.Items) { } // ✅ iteration allowed
```

### Why both?

| Mechanism | Protects against | Scope |
|---|---|---|
| `readonly` field | Reassigning the list reference | Internal domain code |
| `IReadOnlyCollection` property | Calling Add/Remove on the list | External code (handlers, queries) |

Together they ensure the **only way to add an item** is through the entity's own method.

---

## 3. Factory Method — Controlled Creation

Instead of exposing a public constructor, we use a `static` factory method named `Create`. This guarantees the entity is always born in a **valid state**:

```csharp
private Order() { } // private — EF Core only

public static Order Create(
    Guid userId,
    Address address,
    List<(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity)> items)
{
    if (userId == Guid.Empty)
        throw new OrderDomainException("UserId cannot be empty.");

    if (items is null || items.Count == 0)
        throw new OrderDomainException("Order must contain at least one item.");

    var order = new Order
    {
        UserId = userId,
        ShippingAddress = address ?? throw new OrderDomainException("Shipping address is required."),
        Status = OrderStatus.Pending
    };

    foreach (var item in items)
        order.AddItem(item.ProductId, item.ProductName, item.UnitPrice, item.Quantity);

    order.AddDomainEvent(new OrderCreatedDomainEvent(...));

    return order;
}
```

Benefits:
- **Validation** runs before the object exists — an invalid `Order` can never be created
- **Domain events** are raised at creation time
- **Initial state** (e.g. `Status = Pending`) is always set correctly
- The private constructor prevents anyone from using `new Order()` directly

External code creates an order like this:

```csharp
var order = Order.Create(userId, address, items);
```

---

## 4. Behaviour Methods — Business Logic Inside the Entity

State changes happen through **named methods** that encode business rules, not through direct property assignment:

```csharp
public void Confirm()
{
    if (Status != OrderStatus.Pending)
        throw new InvalidOrderStatusTransitionException(Status, OrderStatus.Confirmed);

    Status = OrderStatus.Confirmed;
    UpdatedAt = DateTime.UtcNow;
}
```

The method enforces the rule that only a `Pending` order can be confirmed. External code cannot bypass this:

```csharp
order.Status = OrderStatus.Confirmed; // ❌ compile error — private set
order.Confirm();                       // ✅ goes through business rule validation
```

### `AddItem` — a public behaviour method

`AddItem` is `public` because adding an item to an existing order is a valid operation from outside the entity. However it still routes through the entity's own logic:

```csharp
public void AddItem(Guid productId, string productName, decimal unitPrice, int quantity)
{
    var item = OrderItem.Create(productId, productName, unitPrice, quantity);
    _items.Add(item);
    RecalculateTotalAmount(); // always kept consistent
    UpdatedAt = DateTime.UtcNow;
}
```

If `_items` were a `public List`, a handler could call `order.Items.Add(item)` directly — skipping `RecalculateTotalAmount()` and leaving `TotalAmount` wrong. The public method is the **only door in**.

---

## 5. Private Helper Methods — Internal Consistency

Derived state that must always stay in sync is computed in a private method:

```csharp
private void RecalculateTotalAmount()
{
    TotalAmount = _items.Sum(i => i.TotalPrice);
}
```

It is called by `AddItem` every time the collection changes. External code never calls it directly — it is an internal consistency mechanism.

---

## 6. EF Core Constructor

EF Core needs to materialise entity objects when loading from the database. Because we made the real constructor private, we provide a **separate private parameterless constructor** for EF Core:

```csharp
private Order() { } // EF Core
```

This constructor is intentionally empty and never called by domain code. EF Core uses reflection to invoke it and then populates the properties directly.

---

## 7. Summary — The Full Pattern

```
External code
    │
    ├─ Order.Create(...)          ← factory: validates, sets initial state, raises events
    ├─ order.Confirm()            ← behaviour: enforces business rules, mutates state
    ├─ order.AddItem(...)         ← behaviour: mutates collection through the safe door
    └─ order.Items                ← read-only view: can iterate, cannot mutate
    
    ❌ order.Status = ...         compile error — private set
    ❌ order.Items.Add(...)       compile error — IReadOnlyCollection
    ❌ new Order()                compile error — private constructor
```

The result is an entity that is **impossible to corrupt from outside**. All paths that change state go through methods that enforce business rules, keep derived data consistent, and raise the appropriate domain events.
