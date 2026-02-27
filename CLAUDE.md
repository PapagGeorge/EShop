# EShop — E-Commerce Order Management System

## Project Overview
Microservice-based order management system. Clean Architecture, DDD, CQRS.
**Status:** v1.0.1 released | 74 tests passing | All 10 phases complete.
Planning docs: `C:\Users\Semilac01\Desktop\PlanningPhase\` | Progress archive: `docs/PROGRESS.md`

## Tech Stack
- .NET 8 LTS (C# 12), ASP.NET Core 8, EF Core 8 + SQL Server 2022
- MediatR (CQRS), FluentValidation, AutoMapper
- MassTransit + RabbitMQ, YARP (Gateway), Polly (resilience)
- Serilog + Seq, BCrypt, JWT Bearer auth
- xUnit, FluentAssertions, Moq

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
- JWT Secret: per-service `appsettings.json` → `Jwt` section
- SQL Server SA: `YourStr0ng!Pass` | Connection: `localhost,1433`
- Seq UI: http://localhost:8081 | RabbitMQ: http://localhost:15672 (guest/guest)
