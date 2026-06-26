# ADR 003: Decentralized Authentication (JWT Validation per Service)

**Date:** 2026-06-27  
**Status:** Accepted  
**Context:** Migrated from centralized gateway auth to service-level validation  

## Decision

Remove JWT validation from API Gateway. Each microservice validates JWT tokens independently using the same shared secret.

## Rationale

### Why Decentralized?

1. **Service Independence:** Services work standalone (can be called directly, not just via gateway)
2. **Horizontal Scaling:** No single authentication bottleneck
3. **Microservice Principle:** Each service owns its security
4. **Failure Isolation:** Gateway failure doesn't break all service authentication

### Why Shared Secret?

- Simple to implement
- Services don't need to call Identity service for every request
- Token validation is fast (cryptographic signature check)
- Works in distributed systems

## Flow

```
┌─────────────┐
│   Client    │
└──────┬──────┘
       │
       │ 1. POST /api/auth/login (email, password)
       ↓
    ┌──────────────────┐
    │  Identity (5213) │
    │  ✅ Validates credentials (password hash check)
    │  ✅ Generates JWT token (signed with secret)
    │  ← Returns token
    └──────────────────┘
       │
       │ 2. GET /api/orders with Authorization: Bearer {token}
       ↓
    ┌──────────────────────┐
    │   Gateway (5000)     │
    │   Routes to service  │
    │   (no JWT validation)│
    └──────────────────────┘
       │
       ↓
    ┌──────────────────┐
    │ Ordering (5281)  │
    │ [Authorize]      │ ← Validates JWT signature & expiry
    │ ✅ Same secret   │
    │ ✅ Extracts UserId from claims
    └──────────────────┘
```

## Configuration

### All Services Use Same Secret
```json
{
  "Jwt": {
    "Secret": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "EShop.Identity",
    "Audience": "EShop"
  }
}
```

### Validation in Each Service
```csharp
// Ordering/Identity/Catalog Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = config["Jwt:Issuer"],
            ValidAudience = config["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Secret"]!))
        };
    });

// In controllers
[Authorize]
public class OrdersController : ControllerBase
{
    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}
```

## Security Considerations

### Strengths ✅
- Token tampering is detected (signature verification fails)
- Expired tokens are rejected
- Each service validates independently
- No dependency on Identity service for every request

### Mitigations
- **Secret Management:** Should use vault (Azure Key Vault, HashiCorp Vault) in production
- **Token Rotation:** Consider refresh tokens (future enhancement)
- **HTTPS Only:** Tokens only sent over TLS
- **Short Expiry:** Tokens expire after 1 hour (configurable)

## Alternatives Considered

1. **Centralized Auth at Gateway:** Simpler initially, but service isolation breaks
2. **Introspection Endpoint:** Call Identity on every request (performance hit)
3. **API Key:** Simpler, but no user context in token

## Consequences

- **Positive:**
  - ✅ Services are truly independent
  - ✅ No gateway bottleneck
  - ✅ Fast token validation (no network calls)
  - ✅ Scales to many services easily

- **Negative:**
  - ❌ Token validation happens N times (per service)
  - ❌ Same secret must be kept secure everywhere
  - ❌ No way to instantly revoke tokens (without blacklist)

## Future Enhancements

1. **Token Blacklist:** For immediate revocation (requires Redis)
2. **Refresh Tokens:** Separate long-lived vs. short-lived tokens
3. **JWKS Endpoint:** Services fetch Identity's public key instead of shared secret
4. **Audit Logging:** Track token generation, validation, failures

## Related ADRs

- ADR-001: Microservices Architecture (drives need for service independence)
- ADR-004: Rate Limiting Strategy
