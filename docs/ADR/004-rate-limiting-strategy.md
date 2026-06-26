# ADR 004: Rate Limiting Strategy (Extension Methods Pattern)

**Date:** 2026-06-27  
**Status:** Accepted  
**Context:** Need to prevent abuse and protect services; configuration should be flexible  

## Decision

Implement rate limiting via **extension methods** (AddCustomRateLimiting) at the API Gateway, with configuration in appsettings.json.

## Rationale

1. **Maintainability:** Rate limiting logic extracted from Program.cs (40+ lines → 1 line)
2. **Configuration-Driven:** Change limits without code changes (swap appsettings)
3. **Scalability:** Easy to switch from in-memory to Redis backend (just config change)
4. **Reusability:** Can apply same extension to multiple services
5. **Testing:** Logic can be unit tested independently

## Architecture

```
Program.cs:
  builder.Services.AddCustomRateLimiting(builder.Configuration);

↓

Extensions/RateLimitingExtensions.cs:
  public static IServiceCollection AddCustomRateLimiting(
    this IServiceCollection services,
    IConfiguration configuration)
  {
    var options = configuration.GetSection("RateLimiting").Get<RateLimitingOptions>();
    services.AddRateLimiter(limiterOptions =>
    {
        limiterOptions.AddPolicy<string>("auth", CreateAuthPolicy(options.Auth));
        limiterOptions.AddPolicy<string>("general", CreateGeneralPolicy(options.General));
    });
  }

↓

appsettings.json:
  {
    "RateLimiting": {
      "Auth": {
        "PermitLimit": 10,
        "WindowMinutes": 1
      },
      "General": {
        "PermitLimitAuthenticated": 100,
        "PermitLimitAnonymous": 30,
        "WindowMinutes": 1
      }
    }
  }

↓

Applies to routes:
  - /api/auth/* → "auth" policy (per-IP, 10 requests/minute)
  - /api/orders/* → "general" policy (per-user 100 req/min, or per-IP 30 req/min)
  - /api/products/* → "general" policy
```

## Rate Limiting Policies

### Auth Policy (Brute-Force Protection)
```json
"Auth": {
  "PermitLimit": 10,          // 10 requests
  "WindowMinutes": 1          // per 1 minute
}
```
- **Partition Key:** IP address
- **Use Case:** Prevent brute-force login attempts
- **Limits:** 10 login attempts per IP per minute

### General Policy (Service Protection)
```json
"General": {
  "PermitLimitAuthenticated": 100,  // authenticated users
  "PermitLimitAnonymous": 30,        // unauthenticated users
  "WindowMinutes": 1
}
```
- **Partition Key (Authenticated):** UserID from JWT claims
- **Partition Key (Anonymous):** IP address
- **Use Case:** Protect services from DoS attacks
- **Limits:**
  - 100 requests/minute per authenticated user
  - 30 requests/minute per IP (unauthenticated)

## Configuration Options (appsettings.json)

```json
{
  "RateLimiting": {
    "Auth": {
      "PermitLimit": 10,
      "WindowMinutes": 1
    },
    "General": {
      "PermitLimitAuthenticated": 100,
      "PermitLimitAnonymous": 30,
      "WindowMinutes": 1
    }
  }
}
```

## Route Application (in appsettings.json)

```json
{
  "ReverseProxy": {
    "Routes": {
      "auth-route": {
        "Match": { "Path": "/api/auth/{**catch-all}" },
        "Metadata": { "RateLimiterPolicy": "auth" }
      },
      "orders-route": {
        "Match": { "Path": "/api/orders/{**catch-all}" },
        "Metadata": { "RateLimiterPolicy": "general" }
      }
    }
  }
}
```

## Scaling to Redis Backend

### Current (In-Memory)
```csharp
// All requests in this instance only
// Loss on app restart
```

### Future (Redis)
```csharp
// One-line config change:
services.AddStackExchangeRedisCache(...);
services.AddDistributedRateLimiter<RedisRateLimiter>();

// Shared across all instances
// Survives app restarts
```

## Response When Rate Limited

```
HTTP 429 Too Many Requests

Retry-After: 60
```

Client should back off and retry after the specified seconds.

## Monitoring

### In Logs (Seq)
```
[WRN] Rate limit exceeded for partition: ip:192.168.1.100
[WRN] Rate limit exceeded for partition: user:123e4567-e89b-12d3-a456-426614174000
```

### In Metrics
- Count of 429 responses
- Most rate-limited IPs/users
- Peak rate limiting activity

## Alternatives Considered

1. **Inline in Program.cs:** Simpler initially, but harder to maintain (40+ lines of config)
2. **Separate Service:** Overkill for this size, adds network latency
3. **No Rate Limiting:** Leaves services vulnerable to DoS

## Consequences

- **Positive:**
  - ✅ Clean, maintainable code
  - ✅ Configuration-driven (no code changes for limit tuning)
  - ✅ Easy to migrate to distributed backend (Redis)
  - ✅ Per-IP and per-user support

- **Negative:**
  - ❌ In-memory only (doesn't survive restarts)
  - ❌ Per-instance limits (not shared across replicas)

## Future Enhancements

1. **Redis Backend:** Shared limits across all gateway instances
2. **Adaptive Limits:** Auto-adjust based on load
3. **Rate Limit Headers:** Include X-RateLimit-* in responses
4. **Whitelist/Blacklist:** Exception lists for certain IPs

## Related ADRs

- ADR-001: Microservices Architecture
- ADR-004: Lightweight Logging (rate limit events logged to Seq)
