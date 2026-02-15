# EShop - E-Commerce Order Management System

A microservice-based order management system built with .NET 8, implementing Clean Architecture, Domain-Driven Design (DDD), and CQRS patterns.

## Architecture

```
                         +-------------+
                         |   Client    |
                         |  (Postman)  |
                         +------+------+
                                |
                                v
                    +-----------------------+
                    |     API Gateway       |
                    |       (YARP)          |
                    |  :5000                |
                    |  - Rate Limiting      |
                    |  - Request Routing    |
                    |  - JWT Validation     |
                    +---+-------+-------+--+
                        |       |       |
              +---------+       |       +---------+
              |                 |                 |
              v                 v                 v
     +----------------+ +----------------+ +----------------+
     |   Identity     | |   Ordering     | |   Catalog      |
     |   Service      | |   Service      | |   Service      |
     |   :5213        | |   :5281        | |   :5056        |
     +-------+--------+ +--+----------+-+ +-------+--------+
             |              |          |           |
             v              v          v           v
     +-------------+ +-------------+ +----+ +-------------+
     |  IdentityDb | |  OrderingDb | | MQ | |  In-Memory  |
     |  (SQL)      | |  (SQL)      | +----+ |  (Products) |
     +-------------+ +-------------+        +-------------+
```

### Services

| Service | Port | Description |
|---------|------|-------------|
| API Gateway | 5000 | YARP reverse proxy with rate limiting, JWT validation, CORS |
| Identity API | 5213 | User registration and JWT token generation |
| Ordering API | 5281 | Order management (create, cancel, query) with CQRS |
| Catalog API | 5056 | Product catalog with in-memory data |

### Infrastructure

| Component | Port | Description |
|-----------|------|-------------|
| SQL Server | 1433 | Databases for Identity and Ordering services |
| RabbitMQ | 5672 / 15672 | Message broker for domain events |
| Seq | 5341 / 8081 | Centralized structured logging |

## Tech Stack

- **.NET 8** (LTS) with C# 12
- **ASP.NET Core 8** Web API
- **Entity Framework Core 8** + SQL Server 2022
- **MediatR** (CQRS pattern)
- **FluentValidation** (input validation)
- **MassTransit** + RabbitMQ (async messaging)
- **YARP** (API Gateway / reverse proxy)
- **Polly** (retry + circuit breaker resilience)
- **Serilog** + Seq (structured logging)
- **JWT Bearer** authentication
- **xUnit** + FluentAssertions + Moq (testing)
- **Docker** + Docker Compose (containerization)

## Getting Started

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (for local development)

### Run with Docker (recommended)

```bash
# Clone the repository
git clone https://github.com/PapagGeorge/EShop.git
cd EShop

# Start all services
docker-compose up -d

# Verify all containers are running
docker-compose ps
```

All services will be available:
- Gateway: http://localhost:5000
- Seq (logs): http://localhost:8081
- RabbitMQ: http://localhost:15672 (guest/guest)

### Run Locally

```bash
# Start infrastructure only
docker-compose up -d sqlserver rabbitmq seq

# Run each service (in separate terminals)
dotnet run --project src/Services/Identity/EShop.Identity.API
dotnet run --project src/Services/Ordering/EShop.Ordering.API
dotnet run --project src/Services/Catalog/EShop.Catalog.API
dotnet run --project src/ApiGateway/EShop.ApiGateway
```

### Run Tests

```bash
dotnet test
```

74 tests: 22 Identity unit tests + 52 Ordering unit tests (domain + application).

## API Endpoints

All requests go through the Gateway at `http://localhost:5000`.

### Authentication

#### Register
```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "MyPassword123",
  "fullName": "John Doe"
}
```

#### Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "MyPassword123"
}
```
Returns a JWT token to use in subsequent requests.

### Orders (requires JWT token)

#### Create Order
```http
POST /api/orders
Authorization: Bearer {token}
Content-Type: application/json

{
  "shippingAddress": {
    "street": "123 Main St",
    "city": "Athens",
    "zipCode": "10431",
    "country": "Greece"
  },
  "items": [
    { "productId": "a1b2c3d4-0001-0001-0001-000000000001", "quantity": 2 },
    { "productId": "a1b2c3d4-0002-0002-0002-000000000002", "quantity": 1 }
  ]
}
```

#### Get Order
```http
GET /api/orders/{id}
Authorization: Bearer {token}
```

#### Cancel Order
```http
PATCH /api/orders/{id}/cancel
Authorization: Bearer {token}
```

#### List User Orders
```http
GET /api/orders?status=Pending&page=1&pageSize=10
Authorization: Bearer {token}
```

### Products (no auth required)

#### List All Products
```http
GET /api/products
```

#### Get Product by ID
```http
GET /api/products/{id}
```

### Available Products

| ID | Name | Price | Category |
|----|------|-------|----------|
| a1b2c3d4-0001-0001-0001-000000000001 | Wireless Mouse | 29.99 | Peripherals |
| a1b2c3d4-0002-0002-0002-000000000002 | USB-C Cable | 12.50 | Cables |
| a1b2c3d4-0003-0003-0003-000000000003 | Mechanical Keyboard | 89.99 | Peripherals |
| a1b2c3d4-0004-0004-0004-000000000004 | HDMI Cable 2m | 15.99 | Cables |
| a1b2c3d4-0005-0005-0005-000000000005 | Webcam HD 1080p | 49.99 | Peripherals |

## Project Structure

```
EShop/
+-- src/
|   +-- ApiGateway/EShop.ApiGateway          # YARP reverse proxy
|   +-- BuildingBlocks/EShop.Shared          # Shared kernel (BaseEntity, exceptions)
|   +-- Services/
|       +-- Identity/
|       |   +-- EShop.Identity.API           # Controllers, middleware, DI
|       |   +-- EShop.Identity.Application   # Commands, handlers, validators
|       |   +-- EShop.Identity.Domain        # User entity
|       |   +-- EShop.Identity.Infrastructure# EF Core, repositories, services
|       +-- Ordering/
|       |   +-- EShop.Ordering.API           # Controllers, middleware, DI
|       |   +-- EShop.Ordering.Application   # CQRS commands/queries, behaviors
|       |   +-- EShop.Ordering.Domain        # Order aggregate, value objects, events
|       |   +-- EShop.Ordering.Infrastructure# EF Core, repositories, event bus
|       +-- Catalog/
|           +-- EShop.Catalog.API            # Minimal API with in-memory products
+-- tests/
|   +-- EShop.Identity.UnitTests             # 22 tests
|   +-- EShop.Ordering.UnitTests             # 52 tests
|   +-- EShop.Ordering.IntegrationTests      # Integration test project
+-- docker-compose.yml                       # Full stack deployment
+-- docker-compose.override.yml              # Development overrides
+-- EShop.Postman.json                       # Postman collection
```

## Clean Architecture

Each service follows the dependency rule:

```
Domain (zero dependencies)
  ^
  |
Application (depends on Domain)
  ^
  |
Infrastructure (depends on Application + Domain)
  ^
  |
API (depends on Application, references Infrastructure for DI)
```

## Key Design Decisions

- **Database per Service**: Identity and Ordering have separate SQL Server databases
- **CQRS without Event Sourcing**: Commands and Queries separated via MediatR, single database
- **Domain Events**: Published to RabbitMQ via MassTransit (OrderCreated, OrderCancelled)
- **API Gateway Pattern**: Single entry point via YARP with rate limiting (100 req/min)
- **JWT Authentication**: Symmetric key shared between Gateway and services
- **Service-to-Service**: Ordering calls Catalog via HTTP with Polly retry/circuit breaker

## Configuration

| Setting | Value |
|---------|-------|
| SQL Server Password | `YourStr0ng!Pass` |
| RabbitMQ Credentials | guest / guest |
| JWT Expiration | 1 hour |
| Rate Limit | 100 requests/minute |

## Git Strategy

GitFlow branching model:
- `main` - production releases (tagged: v1.0, v1.0.1)
- `develop` - integration branch
- `feature/*` - feature branches
- `release/*` - release preparation
- `hotfix/*` - production fixes
