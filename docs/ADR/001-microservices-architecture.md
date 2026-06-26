# ADR 001: Microservices Architecture

**Date:** 2026-06-27  
**Status:** Accepted  
**Context:** Building an e-commerce order management system  

## Decision

Adopt a **microservices architecture** with three core services (Identity, Ordering, Catalog) plus an API Gateway.

## Rationale

1. **Scalability:** Each service can scale independently based on demand
2. **Technology Freedom:** Different services can use different tech stacks (though we chose .NET for all)
3. **Team Independence:** Different teams can own different services
4. **Deployment Flexibility:** Services can be deployed independently
5. **Fault Isolation:** Service failures don't cascade (with proper resilience patterns)

## Alternatives Considered

- **Monolith:** Simpler initially, but tight coupling and harder to scale
- **Serverless:** Good for event-driven flows, but overkill for this project

## Consequences

- **Positive:**
  - ✅ Independent scaling per service
  - ✅ Better fault isolation
  - ✅ Clear separation of concerns
  - ✅ Technology choice per service

- **Negative:**
  - ❌ Network latency between services
  - ❌ Distributed transaction complexity
  - ❌ Operational overhead (more services to monitor)
  - ❌ Data consistency challenges

## Implementation Details

```
┌─────────────┐
│   Client    │
└──────┬──────┘
       │
       ↓
┌─────────────────────────┐
│   API Gateway (5000)    │
│   - Routing             │
│   - Rate Limiting       │
│   - Request Logging     │
└─────────────────────────┘
       │
   ┌───┼───┬─────────────┐
   ↓   ↓   ↓             ↓
┌──────┐ ┌────────┐ ┌────────┐
│Identity│Ordering│ │Catalog │
│(5213) │ (5281) │ │(5056)  │
└──────┘ └────────┘ └────────┘
   │       │ ↔ ↕     │
   │       │ HTTP    │
   │       ↓ (Polly) │
   └────────┼────────┘
            │
       ┌────┴──────┐
       ↓           ↓
    ┌─────────┐ ┌─────┐
    │   SQL   │ │ RMQ │
    │ Server  │ │(msgs)
    └─────────┘ └─────┘
```

## Related ADRs

- ADR-003: Decentralized Authentication (each service validates JWT)
- ADR-004: Rate Limiting Strategy
