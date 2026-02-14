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

### Phase 5: Ordering Infrastructure — DONE
- [x] OrderingDbContext with Orders + OrderItems DbSets
- [x] OrderConfiguration (owned Address, string status, backing field for Items)
- [x] OrderItemConfiguration (decimal precision, ignored computed TotalPrice)
- [x] OrderRepository (CRUD + paginated user orders with status filter)
- [x] CatalogServiceClient (HttpClient + Polly retry/circuit breaker at DI level)
- [x] EventBus (MassTransit IPublishEndpoint wrapper)
- [x] API minimal setup (DbContext, DI registrations, connection strings)
- [x] EF Migration: InitialOrderingMigration (Orders + OrderItems tables)
- [x] NuGet: EF Core SqlServer, MassTransit.RabbitMQ, Http.Polly
- [x] All 52 ordering + 22 identity tests passing (74 total)

### Phase 6: Ordering API — DONE
- [x] NuGet: JwtBearer, HealthChecks.EF, Serilog.AspNetCore, Serilog.Sinks.Seq, Swashbuckle 6.9.0
- [x] ExceptionHandlingMiddleware (409 InvalidStatusTransition, 400 Validation, 422 BusinessRule, 404 NotFound, 500 fallback)
- [x] OrdersController (POST create, GET by id, PATCH cancel, GET user orders) with [Authorize]
- [x] Program.cs: Serilog, Controllers, Swagger+JWT, Auth, MediatR, FluentValidation, Behaviors, HealthChecks
- [x] appsettings.json: Jwt section (shared token validation), Serilog config (Console + Seq)
- [x] All 74 tests passing (52 ordering + 22 identity)

### Phase 7: Catalog Service (Minimal) — DONE
- [x] NuGet: Serilog.AspNetCore 8.0.3, Serilog.Sinks.Seq 8.0.0, Swashbuckle 6.9.0
- [x] Product model (Id, Name, Price, Category)
- [x] ProductsController: GET all, GET by id, POST batch (no auth — internal service)
- [x] 5 hardcoded products (Wireless Mouse, USB-C Cable, Mechanical Keyboard, HDMI Cable, Webcam)
- [x] Program.cs: Serilog, Controllers, Swagger, Health checks
- [x] appsettings.json: Serilog config (Console + Seq)
- [x] Batch endpoint matches CatalogServiceClient contract (List<Guid> → List<Product>)
- [x] All 74 tests still passing

### Phase 8: API Gateway (YARP) — DONE
- [x] NuGet: Yarp.ReverseProxy 2.2.0, JwtBearer, Serilog.AspNetCore, Serilog.Sinks.Seq, Swashbuckle 6.9.0
- [x] YARP routes: /api/auth → Identity(:5213), /api/orders → Ordering(:5281), /api/products → Catalog(:5056)
- [x] JWT Authentication (validates tokens, same config as services)
- [x] Rate Limiting: fixed window (100 req/min), returns 429
- [x] CORS: AllowAll policy for development
- [x] Health checks: /health endpoint
- [x] Serilog + Seq logging
- [x] Gateway port: 5000 (per architecture docs)
- [x] Fixed Ordering appsettings Catalog URL: 5002 → 5056
- [x] All 74 tests still passing

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
