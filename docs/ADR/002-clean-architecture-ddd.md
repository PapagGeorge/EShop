# ADR 002: Clean Architecture & Domain-Driven Design

**Date:** 2026-06-27  
**Status:** Accepted  
**Context:** Need clear separation of concerns and scalable domain logic  

## Decision

Adopt **Clean Architecture** (dependency rule) + **Domain-Driven Design (DDD)** principles within each microservice.

## Rationale

### Clean Architecture Benefits
1. **Testability:** Core domain logic has zero external dependencies
2. **Maintainability:** Clear layers, easy to understand and modify
3. **Flexibility:** Can swap implementations (DB, external services) without changing domain logic
4. **Future-Proof:** Doesn't depend on frameworks (easy to upgrade .NET versions)

### DDD Benefits
1. **Ubiquitous Language:** Domain language = code language = business language
2. **Rich Domain Objects:** Business logic lives in entities, not anemic DTOs
3. **Aggregate Roots:** Clear boundaries and transaction scopes
4. **Domain Events:** Capture what happened in the domain

## Architecture Layers (per Service)

```
┌─────────────────────────────────┐
│   API Layer (Controllers)        │  ← HTTP endpoints, routing
│   Depends on: Application        │
└─────────────────────────────────┘
            ↑
            │ depends on (DI)
            ↓
┌─────────────────────────────────┐
│   Application Layer             │  ← Commands, Queries, Handlers
│   (MediatR Commands/Queries)    │  ← Use cases / workflows
│   Depends on: Domain, Shared    │
└─────────────────────────────────┘
            ↑
            │ depends on (DI)
            ↓
┌─────────────────────────────────┐
│   Domain Layer (Entities)        │  ← Business logic
│   (Zero external dependencies!)  │  ← Aggregates, Value Objects
│   Depends on: Shared only        │  ← Domain Events
└─────────────────────────────────┘
            ↑
            │ depends on
            ↓
┌─────────────────────────────────┐
│   Infrastructure Layer          │  ← Repositories, DB, HTTP clients
│   (Implementation details)       │  ← RabbitMQ, external services
│   Depends on: Application        │  ← Logging, caching
└─────────────────────────────────┘
```

## Dependency Rule

**THE DEPENDENCY RULE:** Code that deals with high-level policy must not depend on code that deals with low-level details.

In code:
```
Domain  (no dependencies except Shared)
   ↑
   │
Application (depends on Domain + Shared)
   ↑
   │
Infrastructure (depends on Application)
   ↑
   │
API (depends on Application + Infrastructure)
```

**What this means:**
- ✅ Repository interface lives in Application, implementation in Infrastructure
- ✅ Domain entities never reference Database libraries
- ✅ Commands/Handlers are in Application layer
- ❌ Domain entities should NOT reference HttpClient
- ❌ Infrastructure should NOT directly reference API layer

## Domain-Driven Design Practices

### Entities
```csharp
// Domain entity with private setters, factory method
public class Order : BaseEntity
{
    public Guid UserId { get; private set; }
    public Address ShippingAddress { get; private set; }
    public List<OrderItem> Items { get; private set; }
    
    // Factory method (business rule: create with validation)
    public static Order Create(Guid userId, Address address, List<OrderItem> items)
    {
        var order = new Order { UserId = userId, ... };
        order.RaiseDomainEvent(new OrderCreatedDomainEvent(order.Id, userId));
        return order;
    }
}
```

### Value Objects
```csharp
// Immutable, equality by value
public record Address(string Street, string City, string ZipCode, string Country);

public record OrderItem(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity)
{
    public decimal TotalPrice => UnitPrice * Quantity;
}
```

### Domain Events
```csharp
// Capture what happened
public class OrderCreatedDomainEvent : IDomainEvent
{
    public Guid OrderId { get; }
    public Guid UserId { get; }
    public DateTime OccurredAt { get; }
    
    public OrderCreatedDomainEvent(Guid orderId, Guid userId)
    {
        OrderId = orderId;
        UserId = userId;
        OccurredAt = DateTime.UtcNow;
    }
}
```

### Aggregate Roots
- **Order** is an aggregate root (contains OrderItems)
- **User** is an aggregate root (contains authentication details)
- Boundaries: Save/load entire aggregate together

## Related ADRs

- ADR-001: Microservices Architecture
- ADR-005: CQRS for Read/Write Separation
