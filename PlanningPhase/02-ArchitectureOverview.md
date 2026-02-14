# 02 - Architecture Overview

## High-Level Architecture

```
                         ┌─────────────┐
                         │   Client    │
                         │  (Postman)  │
                         └──────┬──────┘
                                │
                                │ HTTPS
                                v
                    ┌───────────────────────┐
                    │     API Gateway       │
                    │       (YARP)          │
                    │                       │
                    │  - Rate Limiting      │
                    │  - Request Routing    │
                    │  - Auth Validation    │
                    │  - CORS               │
                    └───┬───────┬───────┬───┘
                        │       │       │
              ┌─────────┘       │       └─────────┐
              │                 │                 │
              v                 v                 v
     ┌────────────────┐ ┌────────────────┐ ┌────────────────┐
     │   Identity     │ │   Ordering     │ │   Catalog      │
     │   Service      │ │   Service      │ │   Service      │
     │                │ │                │ │                │
     │  POST /auth/   │ │  POST /orders  │ │  GET /products │
     │    register    │ │  GET /orders   │ │  GET /products/│
     │  POST /auth/   │ │  PATCH cancel  │ │     {id}       │
     │    login       │ │                │ │                │
     └───────┬────────┘ └──┬─────────┬───┘ └───────┬────────┘
             │             │         │              │
             │             │         │              │
             v             v         │              v
     ┌──────────────┐ ┌──────────────┐│     ┌──────────────┐
     │  SQL Server   │ │  SQL Server   ││     │  In-Memory   │
     │  (Identity    │ │  (Ordering    ││     │  (dummy data)│
     │   Database)   │ │   Database)   ││     └──────────────┘
     └──────────────┘ └──────────────┘│
                                       │
                                       v
                              ┌────────────────┐
                              │   RabbitMQ     │
                              │                │
                              │  OrderCreated  │
                              │  OrderCancelled│
                              └────────────────┘
                                       │
                                       v
                              ┌────────────────┐
                              │   Seq          │
                              │  (Centralized  │
                              │   Logging)     │
                              └────────────────┘
```

---

## Communication Patterns

### Synchronous (HTTP)
| From | To | Σκοπός |
|------|----|--------|
| Client → Gateway | Όλα τα services | Entry point |
| Gateway → Identity | Auth endpoints | Routing |
| Gateway → Ordering | Order endpoints | Routing |
| Gateway → Catalog | Product endpoints | Routing |
| Ordering → Catalog | Validate products & τιμές | Service-to-service HTTP call |

### Asynchronous (RabbitMQ)
| Event | Publisher | Potential Consumers |
|-------|-----------|-------------------|
| OrderCreated | Ordering Service | (future) Notification, Inventory |
| OrderCancelled | Ordering Service | (future) Notification, Inventory |

**Σημείωση:** Σε αυτό το project δεν θα φτιάξουμε consumers.
Τα events θα γίνονται publish στο RabbitMQ και θα μπορούμε να τα δούμε
στο RabbitMQ Management UI (http://localhost:15672).
Ο στόχος είναι να δούμε πώς γίνεται publish, όχι consume.

---

## Architecture per Service: Clean Architecture

Κάθε service ακολουθεί **Clean Architecture** (Onion Architecture):

```
┌──────────────────────────────────────────┐
│              API Layer                    │
│  (Controllers, Middleware, DI Setup)     │
│                                          │
│  ┌────────────────────────────────────┐  │
│  │       Application Layer            │  │
│  │  (Commands, Queries, Handlers,     │  │
│  │   DTOs, Validators, Interfaces)    │  │
│  │                                    │  │
│  │  ┌──────────────────────────────┐  │  │
│  │  │       Domain Layer           │  │  │
│  │  │  (Entities, Value Objects,   │  │  │
│  │  │   Domain Events, Enums,      │  │  │
│  │  │   Domain Exceptions)         │  │  │
│  │  │                              │  │  │
│  │  │   *** ΚΑΜΙΑ DEPENDENCY ***   │  │  │
│  │  └──────────────────────────────┘  │  │
│  └────────────────────────────────────┘  │
│                                          │
│  ┌────────────────────────────────────┐  │
│  │     Infrastructure Layer           │  │
│  │  (EF DbContext, Repositories,      │  │
│  │   External service clients,        │  │
│  │   Message bus implementation)      │  │
│  └────────────────────────────────────┘  │
└──────────────────────────────────────────┘
```

**Dependency Rule:** Τα βέλη δείχνουν ΜΟΝΟ προς τα μέσα.
- Domain δεν εξαρτάται από τίποτα
- Application εξαρτάται μόνο από Domain
- Infrastructure εξαρτάται από Application & Domain
- API εξαρτάται από Application (και Infrastructure για DI registration)

---

## Database Strategy

### Database per Service
Κάθε service έχει τη δική του βάση δεδομένων. Αυτό είναι θεμελιώδης αρχή
των microservices — τα services ΔΕΝ μοιράζονται database.

| Service | Database | Τεχνολογία |
|---------|----------|------------|
| Identity Service | IdentityDb | SQL Server + EF Core |
| Ordering Service | OrderingDb | SQL Server + EF Core |
| Catalog Service | — | In-memory (dummy data) |

**Γιατί database-per-service:**
- Loose coupling: αλλαγή schema σε ένα service δεν σπάει τα άλλα
- Independent deployment: κάθε service κάνει deploy ανεξάρτητα
- Technology freedom: κάθε service μπορεί να χρησιμοποιήσει διαφορετική DB

### SQL Server σε Docker
Ένα SQL Server container, πολλά databases μέσα σε αυτό.
Σε production θα ήταν ξεχωριστά instances, αλλά για development
ένα container αρκεί.

---

## Authentication Flow

```
1. Client → POST /api/auth/register (δημιουργία account)
2. Client → POST /api/auth/login (λαμβάνει JWT token)
3. Client → GET /api/orders (στέλνει token στο Authorization header)

   Authorization: Bearer eyJhbGciOiJIUzI1NiIs...

4. Gateway λαμβάνει request
5. Gateway validate JWT signature & expiration
6. Gateway forward request στο Ordering Service (με το JWT)
7. Ordering Service διαβάζει claims (userId, role) από το token
8. Ordering Service εκτελεί business logic
```

**Σημαντικό:** Το Ordering Service ΔΕΝ καλεί το Identity Service
για να validate το token. Αυτό είναι το κλειδί του JWT —
το token είναι self-contained, αρκεί να γνωρίζεις το signing key.

---

## Error Handling Strategy

Unified error response format σε όλα τα services:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "errors": {
    "items": ["At least one item is required."],
    "items[0].quantity": ["Quantity must be greater than 0."]
  },
  "traceId": "00-abc123-def456-01"
}
```

Αυτό ακολουθεί το **RFC 7807 Problem Details** standard.
