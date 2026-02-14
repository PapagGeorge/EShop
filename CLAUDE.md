# EShop — E-Commerce Order Management System

## Project Overview
Microservice-based order management system. Enterprise-grade implementation with Clean Architecture, DDD, CQRS.
Planning documents: `C:\Users\Semilac01\Desktop\PlanningPhase\` (01-07 markdown files).

## Tech Stack
- .NET 8 LTS (pinned via global.json)
- C# 12, ASP.NET Core 8
- EF Core 8 + SQL Server 2022 (Docker)
- MediatR (CQRS), FluentValidation, AutoMapper
- MassTransit + RabbitMQ (async messaging)
- YARP (API Gateway), Polly (resilience)
- Serilog + Seq (logging), BCrypt (password hashing)
- JWT Bearer authentication
- xUnit, FluentAssertions, Moq (testing)

## Git Strategy
- GitFlow: `main` → `develop` → `feature/*`
- Conventional Commits: `feat(scope):`, `fix(scope):`, `chore:`, etc.
- Remote: https://github.com/PapagGeorge/EShop

## Solution Structure
```
EShop.sln
├── src/
│   ├── ApiGateway/EShop.ApiGateway
│   ├── BuildingBlocks/EShop.Shared (BaseEntity, IDomainEvent, Exceptions)
│   └── Services/
│       ├── Identity/   (API, Application, Domain, Infrastructure)
│       ├── Ordering/   (API, Application, Domain, Infrastructure)
│       └── Catalog/    (API only — minimal)
├── tests/
│   ├── EShop.Identity.UnitTests
│   ├── EShop.Ordering.UnitTests
│   └── EShop.Ordering.IntegrationTests
├── docker-compose.yml (SQL Server, RabbitMQ, Seq)
└── docker-compose.override.yml
```

## Clean Architecture Dependency Rule
```
Domain (zero deps) ← Application ← Infrastructure
                     Application ← API → Infrastructure (DI only)
Shared → all Domain projects
```

## Implementation Progress

### Phase 1: Foundation — DONE
- [x] Git repo setup (main + develop)
- [x] Solution structure (14 projects, references)
- [x] Docker Compose (SQL Server :1433, RabbitMQ :5672/15672, Seq :5341/8081)
- [x] EShop.Shared (BaseEntity, IDomainEvent, NotFoundException, BusinessRuleException)

### Phase 2: Identity Service — DONE
- [x] Domain: User entity (factory method, private setters)
- [x] Application: RegisterCommand/Handler/Validator, LoginCommand/Handler/Validator
- [x] Application: ValidationBehavior (MediatR pipeline), DTOs, Interfaces
- [x] Infrastructure: IdentityDbContext, UserConfiguration (Fluent API), UserRepository
- [x] Infrastructure: BcryptPasswordHasher, JwtTokenService
- [x] API: AuthController (POST register, POST login)
- [x] API: ExceptionHandlingMiddleware (RFC 7807), Serilog+Seq, Swagger+JWT, health checks
- [x] Tests: 22 unit tests (handlers, validators, services) — ALL PASSING
- [x] Merged to develop, pushed

### Phase 3: Ordering Domain — DONE
- [x] Order (Aggregate Root), OrderItem (Entity), Address (Value Object), OrderStatus (Enum)
- [x] Domain Events: OrderCreatedDomainEvent, OrderCancelledDomainEvent
- [x] Domain Exceptions: OrderDomainException, InvalidOrderStatusTransitionException
- [x] Unit Tests: 26 tests (creation, invariants, state transitions, value equality) — ALL PASSING
- [x] Merged to develop

### Phase 4: Ordering Application — DONE
- [x] DTOs: AddressDto, OrderItemDto, OrderDto, OrderSummaryDto, PaginatedResult<T>
- [x] Interfaces: IOrderRepository, ICatalogService (+ ProductDto), IEventBus
- [x] Behaviors: ValidationBehavior, LoggingBehavior (MediatR pipeline)
- [x] Commands: CreateOrder (command+handler+validator), CancelOrder (command+handler)
- [x] Queries: GetOrderById (query+handler), GetUserOrders (query+handler+validator)
- [x] Tests: 26 application tests (handlers, validators) — ALL PASSING (52 total)

### Phase 5: Ordering Infrastructure — TODO
- [ ] OrderingDbContext, EF configs, migrations
- [ ] OrderRepository, CatalogServiceClient (Polly), MassTransit EventBus

### Phase 6: Ordering API — TODO
- [ ] OrdersController, middleware, config, integration tests

### Phase 7: Catalog Service (Minimal) — TODO
- [ ] In-memory products, 3 endpoints

### Phase 8: API Gateway (YARP) — TODO
- [ ] Route config, rate limiting, CORS, health checks

### Phase 9: Docker Full Stack & Release — TODO
- [ ] Dockerfiles, E2E testing, release/hotfix git practice

### Phase 10: Polish — TODO
- [ ] README, Postman collection

## Key Configuration
- JWT Secret: configured in appsettings.json per service
- SQL Server SA password: `YourStr0ng!Pass`
- Connection strings use `localhost,1433` for local dev
- Seq UI: http://localhost:8081
- RabbitMQ Management: http://localhost:15672 (guest/guest)

## User Preferences
- Language: Greek (Ελληνικά) for communication
- IDE: Visual Studio
- Git operations: mix of CLI and Visual Studio UI
- Prefers to review and push from VS when possible
