# EShop — E-Commerce Order Management System

## Project Overview
Microservice-based order management system. Clean Architecture, DDD, CQRS.
**Status:** v1.0.1 released | 74 tests passing | All 10 phases complete.
**Language:** Greek (Ελληνικά) preferred in communication.
Planning docs: `C:\Users\Semilac01\Desktop\PlanningPhase\` | Progress archive: `docs/PROGRESS.md`

## Quick Context for AI Sessions
- **User Preference:** Greek language, Visual Studio (not VS Code), mix of CLI + UI
- **Commits:** User reviews and pushes from VS - **DO NOT push unless explicitly asked**
- **Architecture:** Decentralized auth (each service validates JWT), no auth at gateway
- **Rate Limiting:** Extension method pattern in ApiGateway (configurable via appsettings)
- **Logging:** Lightweight setup - Serilog + Seq (no ELK for dev to keep PC light)

## Tech Stack
- **.NET 8 LTS** (C# 12), ASP.NET Core 8, EF Core 8 + SQL Server 2022
- **CQRS:** MediatR, FluentValidation, AutoMapper
- **Integration:** MassTransit + RabbitMQ, YARP (Gateway), Polly (resilience)
- **Observability:** Serilog + Seq (lightweight logging)
- **Security:** BCrypt, JWT Bearer auth (decentralized per service)
- **Testing:** xUnit, FluentAssertions, Moq (74 tests passing)

## Git Strategy
- GitFlow: `main` → `develop` → `feature/*`
- Conventional Commits: `feat(scope):`, `fix(scope):`, `chore:`, `docs:`, `test:`
- Remote: https://github.com/PapagGeorge/EShop

## Solution Structure
```
EShop.sln
├── src/
│   ├── ApiGateway/EShop.ApiGateway         (YARP, port 5000)
│   ├── BuildingBlocks/EShop.Shared         (BaseEntity, IDomainEvent, Exceptions)
│   └── Services/
│       ├── Identity/  (API:5213, Application, Domain, Infrastructure)
│       ├── Ordering/  (API:5281, Application, Domain, Infrastructure)
│       └── Catalog/   (API:5056 — minimal, hardcoded products)
├── tests/
│   ├── EShop.Identity.UnitTests       (22 tests)
│   ├── EShop.Ordering.UnitTests       (52 tests)
│   └── EShop.Ordering.IntegrationTests
├── docker-compose.yml
└── docker-compose.override.yml
```

## Architecture Rules
```
Domain (zero deps) ← Application ← Infrastructure
                     Application ← API → Infrastructure (DI only)
Shared → all Domain projects
```

## Common Commands
```bash
# Build & Test
dotnet build EShop.sln
dotnet test EShop.sln                              # all 74 tests
dotnet test tests/EShop.Identity.UnitTests          # identity only
dotnet test tests/EShop.Ordering.UnitTests          # ordering only

# Run services (local)
dotnet run --project src/Services/Identity/EShop.Identity.API
dotnet run --project src/Services/Ordering/EShop.Ordering.API
dotnet run --project src/Services/Catalog/EShop.Catalog.API
dotnet run --project src/ApiGateway/EShop.ApiGateway

# Docker
docker compose up -d                               # full stack
docker compose down                                # stop all
docker compose up -d --build <service>              # rebuild one

# EF Migrations (from solution root)
dotnet ef migrations add <Name> --project src/Services/Ordering/EShop.Ordering.Infrastructure --startup-project src/Services/Ordering/EShop.Ordering.API
dotnet ef database update --project src/Services/Ordering/EShop.Ordering.Infrastructure --startup-project src/Services/Ordering/EShop.Ordering.API
```

## Coding Conventions
- **Domain entities:** private setters, factory methods (`Create(...)`), raise domain events
- **Commands/Queries:** one file per command/query, handler, and validator (MediatR + FluentValidation)
- **Middleware:** `ExceptionHandlingMiddleware` per service (RFC 7807 ProblemDetails)
- **Repository pattern:** interface in Application, implementation in Infrastructure
- **Config:** `appsettings.json` (local dev), `appsettings.Docker.json` (container)
- **Naming:** PascalCase (C#), `I` prefix for interfaces, `*Dto` suffix for DTOs
- **Tests:** Arrange/Act/Assert, descriptive method names (`Method_Scenario_ExpectedResult`)

## Adding a New Service
1. Create projects: `EShop.{Name}.API`, `.Application`, `.Domain`, `.Infrastructure`
2. Add `EShop.Shared` reference to Domain
3. Follow dependency rule (Domain ← Application ← Infrastructure, API → both)
4. Add YARP route in `ApiGateway/appsettings.json` (ReverseProxy section)
5. Add Dockerfile + `appsettings.Docker.json`
6. Add service to `docker-compose.yml` and `docker-compose.override.yml`
7. Add test project in `tests/`

## Key Configuration
- **JWT Secret:** per-service `appsettings.json` → `Jwt` section (same secret in all services)
- **SQL Server:** SA password `YourStr0ng!Pass` | Connection: `localhost,1433`
- **Seq UI:** http://localhost:8081 | Logging endpoint: http://localhost:5341
- **RabbitMQ:** http://localhost:15672 (guest/guest)
- **Rate Limiting:** Configured via `RateLimiting` section in appsettings (per-IP, per-user)

## Architecture Decisions (see `/docs/ADR/`)
1. **Decentralized Authentication:** Each service validates JWT tokens locally (not at gateway)
   - Reason: Microservice independence, scalability
   - Trade-off: Token validation overhead per request (but negligible)

2. **Rate Limiting as Extension Method:** Configuration-driven, not inline in Program.cs
   - Reason: Maintainability, reusability, easy to switch backends (Redis)
   - Pattern: `AddCustomRateLimiting(builder.Configuration)`

3. **Lightweight Logging:** Serilog + Seq (not ELK)
   - Reason: Development speed, low PC resource usage
   - Scaling: Swap Seq sink for Elasticsearch in appsettings.json (no code changes)

4. **YARP Gateway:** Routing only (no auth, no business logic)
   - Reason: Stateless, simple, separates concerns
   - Observability: Request logging via Serilog middleware

## Service Communication Patterns
- **Internal (Service-to-Service):** HttpClient with Polly resilience policies
  - Example: Ordering → Catalog (GetProductsByIdsAsync)
- **Asynchronous (Service-to-Service):** MassTransit + RabbitMQ
  - Example: Order created → event published → other services subscribe
- **Authentication:** JWT token in Authorization header (validated in each service)

## Common Code Patterns
- **Domain Entity:** `private setters, factory Create() method, raise DomainEvents`
- **Command Handler:** Inject dependencies (repo, logger, services), handle in Handle()
- **Query Handler:** Read-only operations, no side effects
- **Middleware Exception Handling:** RFC 7807 ProblemDetails format
- **Repository Pattern:** Interface in Application, implementation in Infrastructure

## Testing Strategy
- **Unit Tests:** Domain logic, handlers, services (74 tests)
- **Integration Tests:** End-to-end flows with real DB (skeleton in progress)
- **Naming:** `Method_Scenario_ExpectedResult` (e.g., `CreateOrder_WithValidItems_ReturnsOrderDto`)

## Known Gotchas
- **Identity Service:** Uses `EnsureCreated()` (no migrations), Ordering uses `Migrate()`
- **Catalog Service:** Minimal, hardcoded products, no database, no auth required
- **Docker Setup:** Services use `appsettings.Docker.json` with service names (not localhost)
- **SQL Server Healthcheck:** Has retry logic; wait 30s for readiness
- **EF Migrations:** Always use `--project` and `--startup-project` flags from solution root

## Development Workflow
See **`docs/DEVELOPMENT.md`** for:
- Local setup (prerequisites, first-time setup)
- Running services locally vs. Docker
- Adding migrations
- Debugging common issues
- Running tests and checks
