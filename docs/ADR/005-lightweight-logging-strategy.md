# ADR 005: Lightweight Logging Strategy (Serilog + Seq)

**Date:** 2026-06-27  
**Status:** Accepted  
**Context:** Need observability for development and debugging without heavy infrastructure  

## Decision

Use **Serilog** (logging library) + **Seq** (local log aggregation) for development. No ELK stack or Databricks integration for now.

## Rationale

1. **Developer Experience:** Seq UI is beautiful, easy to search logs
2. **Resource Efficiency:** Doesn't consume much PC resources (unlike ELK)
3. **Configuration-Driven:** Easy to swap Seq for Elasticsearch in appsettings (no code changes)
4. **Scalability:** Can migrate to ELK or Databricks by changing Serilog sink config
5. **Structured Logging:** Automatic context logging (machine name, thread ID, service name)

## Architecture

```
┌─────────────────────┐
│   Application Code  │
│   (handlers, APIs)  │
└──────────┬──────────┘
           │
           │ ILogger<T> injection
           ↓
┌─────────────────────┐
│   Serilog Library   │
│   (Log collector)   │
└──────────┬──────────┘
           │
    ┌──────┴──────┐
    ↓             ↓
Console        Seq HTTP
(stdout)    (http://localhost:5341)
    │             │
    ↓             ↓
Terminal      ┌──────────────────────┐
          │  Seq Service           │
          │  - Stores logs in DB   │
          │  - UI: localhost:8081  │
          │  - Search + Filter     │
          └──────────────────────┘
```

## Configuration

### In appsettings.json
```json
{
  "Serilog": {
    "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.Seq" ],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Yarp": "Information"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "Seq",
        "Args": { "serverUrl": "http://localhost:5341" }
      }
    ],
    "Enrich": [ 
      "FromLogContext",
      "WithMachineName",
      "WithThreadId"
    ]
  }
}
```

### In Program.cs (All Services)
```csharp
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));
```

### Request Logging Middleware
```csharp
// In middleware pipeline
app.UseSerilogRequestLogging();
```

This automatically logs:
- HTTP method, path, query string
- Status code
- Duration (milliseconds)
- IP address
- User agent

## Log Levels Used

- **Information:** Normal operations (request started, order created, user logged in)
- **Warning:** Unexpected but handled (rate limit exceeded, validation failure)
- **Error:** Failed operations (database error, service unavailable)

## Example Logs

### Request Logs (Automatic from Middleware)
```
[INF] HTTP POST /api/orders responded 201 in 156.3ms
[INF] HTTP GET /api/products/batch responded 200 in 45.2ms
[WRN] HTTP POST /api/auth/login responded 401 in 23.5ms
[ERR] HTTP GET /api/orders/invalid-id responded 500 in 1234.5ms
```

### Business Logic Logs (Manual in Code)
```csharp
_logger.LogInformation("Creating order for user {UserId} with {ItemCount} items",
    userId, items.Count);

_logger.LogError("Products not found: {MissingIds}", string.Join(", ", missingIds));
```

Results in Seq:
```
[INF] Creating order for user 123e4567-e89b-12d3-a456-426614174000 with 3 items
[ERR] Products not found: prod-1, prod-2
```

## Viewing Logs

### Seq Web UI
```
URL: http://localhost:8081
Features:
  - Real-time log streaming
  - Full-text search
  - Filter by level, service, user
  - Query syntax: Status >= 400
```

### Console Output
```bash
dotnet run --project src/Services/Ordering/EShop.Ordering.API
# Logs appear in terminal (Console sink)
```

### Docker Logs
```bash
docker compose logs -f ordering-api
# Streams logs from container
```

## Scaling to Elasticsearch

### Current Setup
```json
"WriteTo": [
  {
    "Name": "Seq",
    "Args": { "serverUrl": "http://localhost:5341" }
  }
]
```

### Production Setup (Elasticsearch)
```json
"WriteTo": [
  {
    "Name": "Elasticsearch",
    "Args": {
      "nodeUris": [ "http://elasticsearch:9200" ],
      "indexFormat": "eshop-logs-{0:yyyy.MM.dd}"
    }
  }
]
```

**No code changes needed!** Just change appsettings.json.

### Then add Kibana for visualization:
```yaml
kibana:
  image: docker.elastic.co/kibana/kibana:8.0.0
  ports:
    - "5601:5601"
```

## Scaling to Databricks

### Option: Databricks Delta Lake
```csharp
// Send logs to S3 (Databricks managed)
"WriteTo": [
  {
    "Name": "AmazonS3",
    "Args": {
      "bucketName": "my-logs-bucket",
      "path": "eshop-logs/"
    }
  }
]
```

Then Databricks can ingest and analyze.

## What NOT to Log

```csharp
// ❌ DON'T log passwords
_logger.LogInformation($"User password: {password}");

// ❌ DON'T log credit card numbers
_logger.LogInformation($"Card: {cardNumber}");

// ❌ DON'T log at DEBUG level too much (noise)
_logger.LogDebug($"Variable x = {x}"); // Use breakpoints instead

// ✅ DO log business events
_logger.LogInformation("Order {OrderId} created", orderId);

// ✅ DO log errors with context
_logger.LogError(ex, "Failed to process order {OrderId}", orderId);

// ✅ DO use structured fields
_logger.LogInformation("User {UserId} logged in from {IpAddress}",
    userId, ipAddress);
```

## Troubleshooting

### Seq not receiving logs?
1. Check Seq is running: `docker compose ps seq`
2. Verify serverUrl in appsettings: `http://localhost:5341`
3. Check network (Docker): services can reach `seq` hostname
4. View Seq logs: `docker compose logs seq`

### Too many logs (noise)?
```json
"MinimumLevel": {
  "Default": "Information",
  "Override": {
    "Microsoft.AspNetCore": "Warning",  // Suppress framework logs
    "System.Net.Http": "Warning"        // Suppress HTTP client logs
  }
}
```

### Want DEBUG level for specific component?
```json
"Override": {
  "EShop.Ordering.Application": "Debug"  // Only this service at Debug
}
```

## Monitoring Checklist

- [ ] Services are logging to Seq (check http://localhost:8081)
- [ ] No sensitive data in logs (passwords, tokens, PII)
- [ ] Error logs have stack traces
- [ ] Business events are logged (order created, user logged in)
- [ ] Performance metrics logged (order creation time)

## Related ADRs

- ADR-001: Microservices Architecture
- ADR-003: Decentralized Authentication (rate limit events logged)
- ADR-004: Rate Limiting (limit exceeded events logged)

## Future Enhancements

1. **Alerting:** Email/Slack when error rate spikes
2. **Correlation IDs:** Track request across services
3. **Custom Metrics:** Application-specific business metrics
4. **Log Retention:** Clean up old logs (cost savings)
