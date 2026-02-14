# 06 - Implementation Plan

## Σειρά Υλοποίησης

Η σειρά είναι σημαντική — κάθε βήμα χτίζει πάνω στο προηγούμενο.

---

## Phase 1: Foundation (Infrastructure Setup)

### Step 1.0 — Git Repository Setup
- `git init`, δημιουργία repo
- Δημιουργία `main` branch → αρχικό commit
- Δημιουργία `develop` branch από main
- **Git Practice:** Basic branch creation, initial commit

### Step 1.1 — Solution Structure
- Δημιουργία `feature/solution-setup` branch από develop
- Δημιουργία solution και projects σύμφωνα με το 03-TechStack.md
- Project references (dependency rule enforcement)
- .editorconfig για consistent coding style
- .gitignore
- Merge `feature/solution-setup` → develop
- **Git Practice (Σενάριο 1):** Basic feature branch flow

### Step 1.2 — Docker Compose
- docker-compose.yml με SQL Server, RabbitMQ, Seq
- docker-compose.override.yml για development settings
- Verification: docker-compose up τρέχει χωρίς errors

### Step 1.3 — Shared Building Blocks
- EShop.Shared project με:
  - Base Entity class (Id, CreatedAt)
  - IDomainEvent interface
  - Custom Exception types (NotFoundException, BusinessRuleException)
  - Result pattern (optional — για error handling χωρίς exceptions)

---

## Phase 2: Identity Service

### Step 2.1 — Domain & Application Layer
- User entity
- Register command + handler + validator
- Login command + handler + validator
- JWT token generation service (interface στο Application, implementation στο Infrastructure)

### Step 2.2 — Infrastructure Layer
- EF Core DbContext (IdentityDbContext)
- User entity configuration (Fluent API)
- Initial migration
- Password hashing service (BCrypt)

### Step 2.3 — API Layer
- AuthController (register, login)
- JWT configuration στο Program.cs
- Error handling middleware
- Serilog configuration

### Step 2.4 — Testing
- Unit tests: JWT generation, password hashing, validators
- Integration test: register → login → get token flow

- Merge `feature/identity-service` → develop
- **Git Practice (Σενάριο 3):** Soft Reset — θα κάνουμε μερικά commits
  και θα τα "μαζέψουμε" σε ένα καθαρό commit πριν merge

**Checkpoint:** Μπορούμε να κάνουμε register, login, και να πάρουμε JWT token.

---

## Phase 3: Ordering Service — Domain Layer
- Δημιουργία `feature/ordering-domain` branch από develop

### Step 3.1 — Domain Entities
- Order (Aggregate Root) με private setters, behaviors
- OrderItem (Entity)
- Address (Value Object)
- OrderStatus (Enum)

### Step 3.2 — Domain Events
- OrderCreatedDomainEvent
- OrderCancelledDomainEvent

### Step 3.3 — Domain Exceptions
- OrderDomainException
- InvalidOrderStatusTransitionException

### Step 3.4 — Unit Tests
- Order creation (happy path)
- Order creation (no items — should throw)
- Cancel order (Pending → Cancelled)
- Cancel order (Confirmed → should throw)
- Status transitions (all valid/invalid combinations)

- Merge `feature/ordering-domain` → develop
- **Git Practice (Σενάριο 7):** Stash — θα προσποιηθούμε urgent interrupt

**Checkpoint:** Domain layer πλήρες, tested, χωρίς dependencies.

---

## Phase 4: Ordering Service — Application Layer
- Δημιουργία `feature/ordering-application` branch από develop

### Step 4.1 — CQRS Commands
- CreateOrderCommand + CreateOrderCommandHandler
- CancelOrderCommand + CancelOrderCommandHandler

### Step 4.2 — CQRS Queries
- GetOrderByIdQuery + Handler
- GetUserOrdersQuery + Handler (με pagination)

### Step 4.3 — Validators (FluentValidation)
- CreateOrderCommandValidator
- GetUserOrdersQueryValidator

### Step 4.4 — MediatR Pipeline Behaviors
- ValidationBehavior (αυτόματο validation πριν κάθε handler)
- LoggingBehavior (log κάθε command/query)

### Step 4.5 — Interfaces
- IOrderRepository
- ICatalogService (για HTTP call στο Catalog)
- IEventBus (για publish στο RabbitMQ)

### Step 4.6 — DTOs & Mapping
- OrderDto, OrderSummaryDto, OrderItemDto
- AutoMapper profiles

- Merge `feature/ordering-application` → develop
- **Git Practice (Σενάριο 2):** Merge Conflict — θα δημιουργήσουμε σκόπιμα
  conflict στο Shared project και θα το λύσουμε

**Checkpoint:** Application layer πλήρες. Handlers δουλεύουν με mocked dependencies.

---

## Phase 5: Ordering Service — Infrastructure Layer
- Δημιουργία `feature/ordering-infrastructure` branch από develop

### Step 5.1 — Database
- OrderingDbContext
- Entity configurations (Fluent API)
  - Order configuration (table, columns, relationships)
  - OrderItem configuration
  - Address owned type configuration
- Initial migration

### Step 5.2 — Repository
- OrderRepository implementation (EF Core)

### Step 5.3 — External Service Clients
- CatalogServiceClient (HttpClient → Catalog API)
  - Polly retry policy
  - Polly circuit breaker

### Step 5.4 — Message Bus
- MassTransit configuration
- EventBus implementation (publish to RabbitMQ)
- Domain event dispatcher (μετά το SaveChanges, publish events)

- Merge `feature/ordering-infrastructure` → develop
- **Git Practice (Σενάριο 5):** Revert — θα κάνουμε push ένα commit
  με "bug" και θα το revert-αρουμε safely

**Checkpoint:** Infrastructure πλήρες. Database δημιουργείται, messages στέλνονται.

---

## Phase 6: Ordering Service — API Layer
- Δημιουργία `feature/ordering-api` branch από develop

### Step 6.1 — Controllers
- OrdersController
  - POST /api/orders → Send CreateOrderCommand
  - GET /api/orders/{id} → Send GetOrderByIdQuery
  - PATCH /api/orders/{id}/cancel → Send CancelOrderCommand
  - GET /api/orders → Send GetUserOrdersQuery

### Step 6.2 — Middleware
- Global exception handler middleware
- Correlation ID middleware (X-Correlation-Id header)

### Step 6.3 — Configuration
- JWT validation (ίδιο signing key με Identity Service)
- Swagger/OpenAPI με JWT support
- Serilog + Seq
- Health checks (DB, RabbitMQ)

### Step 6.4 — Integration Tests
- Create order (full flow: API → Handler → DB → Event)
- Get order
- Cancel order (valid & invalid)
- Unauthorized access (no token, wrong user)

- **Git Practice (Σενάριο 4):** Hard Reset — σε ξεχωριστό πειραματικό
  branch, θα δούμε πώς λειτουργεί (safely)

**Checkpoint:** Ordering Service πλήρες και tested.

---

## Phase 7: Catalog Service (Minimal)
- Δημιουργία `feature/catalog-service` branch από develop

### Step 7.1
- Minimal API ή controller
- In-memory product list (hardcoded)
- GET /api/products, GET /api/products/{id}, POST /api/products/batch
- Health check

- **Git Practice (Σενάριο 6):** Cherry-Pick — θα κάνουμε cherry-pick
  ένα fix από feature branch στο develop

**Checkpoint:** Catalog Service τρέχει, Ordering μπορεί να κάνει HTTP call σε αυτό.

---

## Phase 8: API Gateway
- Δημιουργία `feature/api-gateway` branch από develop

### Step 8.1 — YARP Configuration
- Route configuration (Identity, Ordering, Catalog)
- Rate limiting middleware (ASP.NET 8 built-in)
- CORS configuration
- Swagger aggregation (optional)

### Step 8.2 — Health Check Aggregation
- Gateway health check that checks all downstream services

**Checkpoint:** Ένα single entry point για όλα τα services.

---

## Phase 9: Docker Compose — Full Stack & Release

### Step 9.1
- Dockerfile για κάθε service
- docker-compose.yml ενημερωμένο με όλα τα services
- Environment variables configuration
- Network configuration (services communicate internally)
- Volume configuration (SQL Server data persistence)

### Step 9.2 — End-to-End Testing
- docker-compose up
- Register user → Login → Create order → Get order → Cancel order
- Verify logs στο Seq
- Verify events στο RabbitMQ Management UI

---

### Step 9.3 — Release & Hotfix Practice
- **Git Practice (Σενάριο 8):** Release branch
  - Κόβουμε `release/v1.0` από develop
  - Merge → main, Tag `v1.0`, merge → develop
- **Git Practice (Σενάριο 9):** Hotfix
  - "Βρίσκουμε" critical bug στο main
  - `hotfix/critical-fix` → fix → merge σε main + develop
  - Tag `v1.0.1`

---

## Phase 10: Polish & Documentation

### Step 10.1
- README.md με setup instructions
- Postman collection για testing
- Architecture Decision Records (optional)

---

## Estimated Complexity per Phase

| Phase | Complexity | Files |
|-------|-----------|-------|
| 1. Foundation | Low | ~10 |
| 2. Identity Service | Medium | ~15 |
| 3. Ordering Domain | Medium | ~10 |
| 4. Ordering Application | High | ~20 |
| 5. Ordering Infrastructure | High | ~15 |
| 6. Ordering API | Medium | ~10 |
| 7. Catalog Service | Low | ~5 |
| 8. API Gateway | Low | ~5 |
| 9. Docker Full Stack | Medium | ~5 |
| 10. Polish | Low | ~3 |

---

## Σε κάθε session μπορούμε:
- Να ολοκληρώσουμε 1-2 phases
- Να κάνουμε review τι φτιάξαμε
- Να συζητήσουμε τα patterns που χρησιμοποιήσαμε
- Να κάνουμε adjustments αν χρειαστεί
