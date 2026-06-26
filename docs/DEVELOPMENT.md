# EShop Development Guide

## Prerequisites
- **.NET 8 SDK** (or later) - https://dotnet.microsoft.com/
- **Visual Studio 2022** (Community or higher)
- **Docker Desktop** (for running full stack)
- **SQL Server 2022** (or use docker-compose)
- **RabbitMQ** (or use docker-compose)

---

## Local Development Setup

### Option 1: Full Stack with Docker (Recommended)

**Start everything:**
```bash
docker compose up -d
```

**Services will be available at:**
- 🌐 API Gateway: http://localhost:5000
- 🔐 Identity API: http://localhost:5213
- 📦 Ordering API: http://localhost:5281
- 🏷️ Catalog API: http://localhost:5056
- 📝 Seq (logs): http://localhost:8081
- 💬 RabbitMQ: http://localhost:15672 (guest/guest)
- 🗄️ SQL Server: localhost:1433 (sa/YourStr0ng!Pass)

**Stop everything:**
```bash
docker compose down
```

---

### Option 2: Local Services + Docker Infrastructure

**Start only infrastructure:**
```bash
docker compose up -d seq sqlserver rabbitmq
```

**Then run services locally in VS:**
- Right-click solution → "Set Startup Projects" → Multiple startup projects
- Select all 4 API projects: Identity, Ordering, Catalog, ApiGateway
- Press F5 to debug

---

## Common Development Tasks

### Build & Test
```bash
# Build entire solution
dotnet build EShop.sln

# Run all tests (74 tests)
dotnet test EShop.sln

# Run specific test project
dotnet test tests/EShop.Ordering.UnitTests
dotnet test tests/EShop.Identity.UnitTests

# Run tests with coverage
dotnet test EShop.sln /p:CollectCoverage=true
```

### Running Individual Services
```bash
# From solution root, run any service
dotnet run --project src/Services/Identity/EShop.Identity.API
dotnet run --project src/Services/Ordering/EShop.Ordering.API
dotnet run --project src/Services/Catalog/EShop.Catalog.API
dotnet run --project src/ApiGateway/EShop.ApiGateway
```

### Database Migrations (Ordering Service only)
```bash
# Add new migration
dotnet ef migrations add <MigrationName> \
  --project src/Services/Ordering/EShop.Ordering.Infrastructure \
  --startup-project src/Services/Ordering/EShop.Ordering.API

# Apply migrations
dotnet ef database update \
  --project src/Services/Ordering/EShop.Ordering.Infrastructure \
  --startup-project src/Services/Ordering/EShop.Ordering.API
```

**Note:** Identity uses `EnsureCreated()`, not migrations. Catalog has no database.

---

## Git Workflow

### Creating a Feature Branch
```bash
# Always branch from develop
git checkout develop
git pull origin develop

# Create feature branch (GitFlow naming)
git checkout -b feature/your-feature-name
```

### Committing Changes
```bash
# Stage changes
git add src/...
git add tests/...

# Commit with conventional format
git commit -m "feat(scope): add new feature

Detailed description here.

Co-Authored-By: Claude Haiku 4.5 <noreply@anthropic.com>"
```

**Conventional Commit Format:**
- `feat(scope):` - new feature
- `fix(scope):` - bug fix
- `refactor(scope):` - code refactor (no behavior change)
- `test:` - test additions/changes
- `docs:` - documentation
- `chore:` - build, deps, config

### Pushing & Review
```bash
# Push feature branch
git push -u origin feature/your-feature-name

# Create PR from GitHub (or VS)
# → Base: develop
# → Compare: feature/your-feature-name

# After approval, merge to develop locally
git checkout develop
git pull origin develop
git merge feature/your-feature-name --no-ff
git push origin develop
```

---

## Debugging & Troubleshooting

### Services won't start?
```bash
# Check docker containers are running
docker compose ps

# View logs
docker compose logs -f <service-name>

# Restart a service
docker compose restart <service-name>
```

### Tests failing?
```bash
# Run tests with verbose output
dotnet test EShop.sln -v n

# Run single test
dotnet test EShop.sln --filter "TestClassName.TestMethodName"
```

### Database connection issues?
```bash
# Check SQL Server is running
docker compose logs sqlserver

# Verify connection string
# Check appsettings.json / appsettings.Docker.json
```

### Can't see logs?
```bash
# Verify Seq is running
docker compose ps seq

# Check Seq UI: http://localhost:8081

# Verify Serilog configuration in appsettings.json
# Should point to http://localhost:5341
```

---

## Code Structure Deep Dive

### Adding a New Command/Query

**1. Domain Layer** (no external dependencies)
```csharp
// src/Services/Ordering/EShop.Ordering.Domain/Entities/Order.cs
public static Order Create(Guid userId, Address address, List<(Guid ProductId, string ProductName, decimal Price, int Quantity)> items)
{
    var order = new Order { UserId = userId, ShippingAddress = address, Items = items };
    order.RaiseDomainEvent(new OrderCreatedDomainEvent(...));
    return order;
}
```

**2. Application Layer** (business logic)
```csharp
// Commands
// src/Services/Ordering/EShop.Ordering.Application/Commands/CreateOrder/CreateOrderCommand.cs
public record CreateOrderCommand(Guid UserId, AddressDto ShippingAddress, List<OrderItemRequest> Items)
    : IRequest<OrderDto>;

// Handler
// src/Services/Ordering/EShop.Ordering.Application/Commands/CreateOrder/CreateOrderCommandHandler.cs
public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // Fetch products from catalog
        var products = await _catalogService.GetProductsByIdsAsync(...);
        // Create order
        var order = Order.Create(request.UserId, address, items);
        // Save and publish events
        await _orderRepository.AddAsync(order, cancellationToken);
        // ... publish events
    }
}

// Validator
// src/Services/Ordering/EShop.Ordering.Application/Commands/CreateOrder/CreateOrderCommandValidator.cs
public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty();
    }
}
```

**3. Infrastructure Layer** (persistence, external services)
```csharp
// Repository
// src/Services/Ordering/EShop.Ordering.Infrastructure/Repositories/OrderRepository.cs
public class OrderRepository : IOrderRepository
{
    public async Task AddAsync(Order order, CancellationToken cancellationToken) { ... }
}

// External service client
// src/Services/Ordering/EShop.Ordering.Infrastructure/Services/CatalogServiceClient.cs
public class CatalogServiceClient : ICatalogService
{
    public async Task<List<ProductDto>> GetProductsByIdsAsync(List<Guid> ids, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync("api/products/batch", ids, cancellationToken);
        return await response.Content.ReadFromJsonAsync<List<ProductDto>>(cancellationToken: cancellationToken);
    }
}
```

**4. API Layer** (endpoints)
```csharp
// src/Services/Ordering/EShop.Ordering.API/Controllers/OrdersController.cs
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var command = new CreateOrderCommand(GetUserId(), request.ShippingAddress, request.Items);
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetOrderById), new { id = result.Id }, result);
    }
}
```

### File Organization Rules
- **One command/query per file** (not multiple in one)
- **Handler immediately after** (or in same file for small handlers)
- **Validator in same folder**
- **DTOs in `/Application/DTOs/`**
- **Domain entities in `/Domain/Entities/`**

---

## Testing Strategy

### Unit Tests (Domain + Application Logic)
```csharp
[Fact]
public void CreateOrder_WithValidItems_CreatesOrderSuccessfully()
{
    // Arrange
    var userId = Guid.NewGuid();
    var items = new List<(Guid, string, decimal, int)> { (productId, "Product", 10m, 1) };
    
    // Act
    var order = Order.Create(userId, address, items);
    
    // Assert
    order.UserId.Should().Be(userId);
    order.Items.Should().HaveCount(1);
    order.DomainEvents.Should().ContainSingle(e => e is OrderCreatedDomainEvent);
}
```

### Test Naming Convention
`Method_Scenario_ExpectedResult`

Examples:
- `CreateOrder_WithValidItems_ReturnsOrderDto`
- `Login_WithInvalidCredentials_ThrowsBusinessRuleException`
- `GetProducts_WithEmptyList_ReturnsEmptyCollection`

---

## Performance & Monitoring

### Check Service Health
```bash
# Gateway health
curl http://localhost:5000/health

# Identity health
curl http://localhost:5213/health

# View metrics in Seq
# http://localhost:8081 → Search for metrics
```

### Monitor Logs in Real-Time
```bash
# Tail logs from all services
docker compose logs -f

# Tail specific service
docker compose logs -f ordering-api
```

---

## Useful VSCode Extensions (if needed)
- C# (OmniSharp)
- NuGet Package Manager
- REST Client (for API testing)

---

## Further Reading
- See `CLAUDE.md` for architecture overview
- See `docs/ADR/` for decision records
- See `docs/PROGRESS.md` for project history
