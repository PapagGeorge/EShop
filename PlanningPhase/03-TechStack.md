# 03 - Tech Stack & Libraries

## Runtime & Framework

| Component | Technology | Version | Σκοπός |
|-----------|-----------|---------|--------|
| Runtime | .NET | 8 (LTS) | Long-term support, enterprise standard |
| Web Framework | ASP.NET Core | 8 | Web API |
| Language | C# | 12 | Primary language |

---

## Core Libraries

### API Layer
| Library | NuGet Package | Σκοπός |
|---------|--------------|--------|
| YARP | Yarp.ReverseProxy | API Gateway / Reverse Proxy |
| Swashbuckle | Swashbuckle.AspNetCore | Swagger/OpenAPI documentation |
| Serilog | Serilog.AspNetCore | Structured logging |
| Serilog Seq Sink | Serilog.Sinks.Seq | Αποστολή logs στο Seq |

### Application Layer
| Library | NuGet Package | Σκοπός |
|---------|--------------|--------|
| MediatR | MediatR | CQRS — Command/Query dispatch |
| FluentValidation | FluentValidation.DependencyInjectionExtensions | Request validation |
| AutoMapper | AutoMapper | Entity ↔ DTO mapping |

### Domain Layer
Κανένα external package — pure C# classes μόνο.
Αυτό είναι σκόπιμο: το Domain δεν πρέπει να εξαρτάται από τίποτα.

### Infrastructure Layer
| Library | NuGet Package | Σκοπός |
|---------|--------------|--------|
| EF Core | Microsoft.EntityFrameworkCore.SqlServer | ORM / Database access |
| EF Core Tools | Microsoft.EntityFrameworkCore.Tools | Migrations |
| MassTransit | MassTransit.RabbitMQ | Message bus abstraction πάνω από RabbitMQ |
| Polly | Microsoft.Extensions.Http.Polly | Retry/Circuit breaker για HTTP calls |

### Authentication
| Library | NuGet Package | Σκοπός |
|---------|--------------|--------|
| JWT Bearer | Microsoft.AspNetCore.Authentication.JwtBearer | JWT token validation |
| BCrypt | BCrypt.Net-Next | Password hashing |

### Testing
| Library | NuGet Package | Σκοπός |
|---------|--------------|--------|
| xUnit | xunit | Test framework |
| FluentAssertions | FluentAssertions | Readable test assertions |
| Moq | Moq | Mocking framework |
| Testcontainers | Testcontainers.MsSql | Integration tests με real SQL Server σε Docker |
| WebApplicationFactory | Microsoft.AspNetCore.Mvc.Testing | API integration tests |

---

## Infrastructure (Docker)

| Component | Docker Image | Port | Σκοπός |
|-----------|-------------|------|--------|
| SQL Server | mcr.microsoft.com/mssql/server:2022-latest | 1433 | Database |
| RabbitMQ | rabbitmq:3-management | 5672 / 15672 | Message broker + Management UI |
| Seq | datalust/seq:latest | 5341 / 80 | Centralized log viewer |

---

## Project Structure (Solution)

```
EShop/
├── src/
│   ├── ApiGateway/
│   │   └── EShop.ApiGateway/                    (.NET 8 Web App)
│   │
│   ├── Services/
│   │   ├── Identity/
│   │   │   ├── EShop.Identity.API/              (.NET 8 Web API)
│   │   │   ├── EShop.Identity.Application/      (Class Library)
│   │   │   ├── EShop.Identity.Domain/           (Class Library)
│   │   │   └── EShop.Identity.Infrastructure/   (Class Library)
│   │   │
│   │   ├── Ordering/
│   │   │   ├── EShop.Ordering.API/              (.NET 8 Web API)
│   │   │   ├── EShop.Ordering.Application/      (Class Library)
│   │   │   ├── EShop.Ordering.Domain/           (Class Library)
│   │   │   └── EShop.Ordering.Infrastructure/   (Class Library)
│   │   │
│   │   └── Catalog/
│   │       └── EShop.Catalog.API/               (.NET 8 Web API — minimal)
│   │
│   └── BuildingBlocks/
│       └── EShop.Shared/                        (Class Library)
│           ├── Exceptions/
│           ├── Messaging/
│           └── Extensions/
│
├── tests/
│   ├── EShop.Ordering.UnitTests/
│   ├── EShop.Ordering.IntegrationTests/
│   └── EShop.Identity.UnitTests/
│
├── docker-compose.yml
├── docker-compose.override.yml
├── EShop.sln
└── README.md
```

---

## Γιατί αυτές οι επιλογές

### MediatR αντί απευθείας service calls
- Decouples τον controller από τη business logic
- Κάθε use case = 1 handler = εύκολο testing
- Pipeline behaviors (logging, validation) χωρίς code duplication

### MassTransit αντί raw RabbitMQ client
- Abstraction: αν αλλάξεις σε Azure Service Bus ή Kafka, αλλάζεις μόνο config
- Built-in retry, error handling, dead letter queues
- Strongly typed messages

### FluentValidation αντί Data Annotations
- Validation logic ξεχωριστή από models
- Complex rules (conditional validation, cross-field)
- Εύκολο testing

### EF Core αντί Dapper
- Migrations: version control του schema
- Change tracking: ιδανικό για DDD aggregates
- Relationships, lazy/eager loading
- Σημείωση: σε read-heavy queries μπορεί να χρησιμοποιήσεις Dapper,
  αλλά για αρχή EF Core αρκεί

### YARP αντί Ocelot
- Developed by Microsoft (official)
- Πολύ καλύτερο performance
- Actively maintained
- Πιο flexible configuration
