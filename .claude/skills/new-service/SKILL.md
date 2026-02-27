# /new-service — Scaffold a New Microservice

Create the full Clean Architecture project structure for a new microservice.

## Usage
- `/new-service <ServiceName>` — e.g., `/new-service Payment`

## Instructions

Given a service name (e.g., `Payment`), create the following:

### 1. Projects (under `src/Services/<ServiceName>/`)
```
EShop.<ServiceName>.Domain          → class library, ref EShop.Shared
EShop.<ServiceName>.Application     → class library, ref Domain
EShop.<ServiceName>.Infrastructure  → class library, ref Application
EShop.<ServiceName>.API             → web api, ref Application + Infrastructure
```

Use `dotnet new classlib` / `dotnet new webapi` and `dotnet sln add` for each.

### 2. Domain Project
- Create `Entities/` folder with a placeholder entity inheriting `BaseEntity`
- Create `Exceptions/` folder

### 3. Application Project
- Add NuGet: MediatR, FluentValidation.DependencyInjectionExtensions
- Create folders: `Commands/`, `Queries/`, `DTOs/`, `Interfaces/`, `Behaviors/`
- Copy `ValidationBehavior.cs` pattern from Ordering.Application

### 4. Infrastructure Project
- Add NuGet: EF Core SqlServer, MassTransit.RabbitMQ
- Create `Data/<ServiceName>DbContext.cs`
- Create `Repositories/` folder

### 5. API Project
- Add NuGet: JwtBearer, Serilog.AspNetCore, Serilog.Sinks.Seq, Swashbuckle
- Copy `ExceptionHandlingMiddleware.cs` pattern from existing service
- Setup `Program.cs` with Serilog, Swagger+JWT, Auth, MediatR, HealthChecks
- Create `appsettings.json` and `appsettings.Docker.json`

### 6. Test Project (under `tests/`)
- Create `EShop.<ServiceName>.UnitTests` with xUnit, FluentAssertions, Moq

### 7. Integration
- Add YARP route in `src/ApiGateway/EShop.ApiGateway/appsettings.json`
- Create Dockerfile (multi-stage, same pattern as other services)
- Add to `docker-compose.yml` and `docker-compose.override.yml`

### 8. Verify
- Run `dotnet build EShop.sln` to ensure everything compiles
- Run `dotnet test EShop.sln` to ensure existing tests still pass

Ask the user for the port number before starting, or suggest the next available one.
