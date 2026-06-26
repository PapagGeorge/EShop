# Architecture Decision Records (ADR)

This directory contains **Architecture Decision Records** - documents that explain the "why" behind key architectural choices in the EShop project.

## Quick Reference

| # | Title | Status | Context |
|---|-------|--------|---------|
| **001** | [Microservices Architecture](./001-microservices-architecture.md) | ✅ Accepted | Three services + Gateway |
| **002** | [Clean Architecture & DDD](./002-clean-architecture-ddd.md) | ✅ Accepted | Domain-driven design, layer separation |
| **003** | [Decentralized Authentication](./003-decentralized-authentication.md) | ✅ Accepted | Each service validates JWT (not gateway) |
| **004** | [Rate Limiting Strategy](./004-rate-limiting-strategy.md) | ✅ Accepted | Extension methods, config-driven |
| **005** | [Lightweight Logging](./005-lightweight-logging-strategy.md) | ✅ Accepted | Serilog + Seq (no ELK) |

---

## What is an ADR?

An ADR is a short markdown document that records:
- **What decision** was made
- **Why** it was made (rationale, benefits)
- **What alternatives** were considered
- **Consequences** (positive and negative)

Each ADR is **immutable once accepted** (never modify historical records). If a decision changes, create a new ADR that supersedes the old one.

---

## How to Use These

### For Development
- Read ADR-002 (Clean Architecture) to understand code organization
- Read ADR-003 (Auth) to understand how JWT validation works
- Read ADR-004 (Rate Limiting) before modifying limits

### For New Features
- Check ADRs to understand architectural constraints
- Example: Adding a new service? Follow patterns in ADR-001 and ADR-002

### For AI Sessions (Claude)
These ADRs provide context on the "why" so Claude can make better suggestions and understand constraints.

---

## Decision Flow

```
Problem
   ↓
Discuss alternatives
   ↓
Make decision
   ↓
Document in ADR ← (you are here)
   ↓
Implement
   ↓
Monitor & adapt
```

---

## Adding a New ADR

When making a significant architectural decision:

1. **Create new file:** `docs/ADR/NNN-decision-title.md` (use next number)
2. **Copy template:**
   ```markdown
   # ADR NNN: Decision Title
   
   **Date:** YYYY-MM-DD  
   **Status:** Proposed|Accepted|Deprecated  
   **Context:** Background that led to this decision  
   
   ## Decision
   
   What decision was made?
   
   ## Rationale
   
   Why was this chosen?
   
   ## Alternatives Considered
   
   What else could we have done?
   
   ## Consequences
   
   What are the positive and negative impacts?
   
   ## Related ADRs
   
   Links to other ADRs
   ```
3. **Update this README** with the new ADR
4. **Commit with:** `docs: add ADR-NNN: decision title`

---

## Current Status

| Aspect | Covered |
|--------|---------|
| Architecture | ✅ ADR-001, ADR-002 |
| Authentication | ✅ ADR-003 |
| Performance | ✅ ADR-004 (rate limiting) |
| Observability | ✅ ADR-005 (logging) |
| Database | ⏳ Planned |
| Caching | ⏳ Planned |
| Testing | ⏳ Planned |

---

## For Claude Sessions

When starting a Claude session on this project:
1. Claude loads **CLAUDE.md** for overview
2. Claude reads **DEVELOPMENT.md** for practical details
3. Claude references **ADRs** to understand "why"

This ensures Claude understands the constraints and can provide contextually-aware suggestions.

---

## Questions?

- Can't understand a decision? → Read the relevant ADR
- Disagree with a decision? → Create a new ADR proposing change (don't modify existing)
- Need context for a feature? → Check related ADRs
