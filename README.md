# CHL NRB Verification Gateway

**Project Reference:** `CICT/10032601/NRB` — Continental Holdings Limited ICT Project

---

## Project Overview

The **CHL NRB Verification Gateway** provides a centralised identity verification service for all CHL subsidiaries, proxying requests to the National Registration Bureau (NRB) of Malawi. It implements a **cache-first** lookup pattern before making live NRB API calls, reducing redundant biometric queries.

The solution is a single deployable process today but is **structured to split into two separate deployables** (Gateway and Portal) with minimal rework — see the *Future Split* section below.

---

## Source of Truth Documents

Before modifying any entity, business rule, tier configuration, or security constraint, read:

| Document | Location | Purpose |
|---|---|---|
| `CHL_NRB_Gateway_System_Architecture.pdf` | Project root (hand off from ICT) | Cache-first pattern, tier structure, role separation, General Request Pattern |
| `CHL_Group_KYC_Data_Schema.pdf` | Project root (hand off from ICT) | Entity definitions, relationships, config schema section 8.4 |

> **Important:** The documents win over any code comment or README section if they conflict. Ask before deviating.

---

## Solution Structure

```
CHL.NrbGateway.sln
├── src/
│   ├── CHL.NrbGateway.Domain/              — Entities, Enums, zero dependencies
│   ├── CHL.NrbGateway.Application/         — Interfaces, DTOs, business logic services
│   │                                         (no EF Core or ASP.NET references)
│   ├── CHL.NrbGateway.Infrastructure/      — EF Core DbContexts, migrations,
│   │                                         NRB HTTP adapters, repositories, auth services
│   └── CHL.NrbGateway.Api/                 — Controllers, auth handlers, Program.cs
│       ├── Gateway/                          → Subsidiary-facing endpoints (X-Api-Key auth)
│       └── Portal/                           → ICT Admin-facing endpoints (JWT Bearer auth)
└── tests/
    └── CHL.NrbGateway.Tests/               — xUnit unit tests
```

### DbContext / Schema / Role Separation

| DbContext | Postgres Schema | Postgres Role | Scope |
|---|---|---|---|
| `KycDbContext` | `kyc` | `gateway_role` | Individual/Organization KYC data, verification events, gateway audit log |
| `ConfigDbContext` | `config` | `portal_role` | Admin users, subsidiaries, API keys, tier settings, NRB environment settings |

Both contexts target the **same Postgres database** but are strictly isolated by schema and role. `gateway_requests.SubsidiaryId` is a bare `Guid` FK value — there is **no EF navigation property** crossing DbContext boundaries (see note below).

---

## MVP Scope

**Launched tier: Intermediate Middleware only** (`POST /middleware/iVerify`).

| Feature | Status |
|---|---|
| Cache-first verification (kyc schema) | ✅ MVP |
| NRB Intermediate Adapter (OAuth + `X-Api-Timestamp`) | ✅ MVP |
| Gateway endpoint `POST /api/v1/gateway/verify/intermediate` | ✅ MVP |
| Portal CRUD: subsidiaries, API keys, tier settings, NRB env | ✅ MVP |
| Portal admin login (JWT) | ✅ MVP |
| `GET /health` | ✅ MVP |
| Basic / Text Lookup / Advanced tiers | 🔲 Interfaces defined — not implemented |
| MFA for admins | 🔲 Not yet |

---

## Getting Started (Local Dev)

### Prerequisites
- .NET 10 SDK
- Docker Desktop (for PostgreSQL 18)

### 1. Start PostgreSQL

```bash
docker compose up -d
```

This starts `postgres:18-alpine`, runs `scripts/01-init.sql` which:
- Enables `pgcrypto`
- Creates schemas `kyc` and `config`
- Creates roles `gateway_role` and `portal_role` with least-privilege grants scoped to their schemas

### 2. Run EF Core Migrations

```bash
# KYC schema migrations
dotnet ef migrations add InitialKyc \
  -p src/CHL.NrbGateway.Infrastructure \
  -s src/CHL.NrbGateway.Api \
  -c KycDbContext

# Config schema migrations
dotnet ef migrations add InitialConfig \
  -p src/CHL.NrbGateway.Infrastructure \
  -s src/CHL.NrbGateway.Api \
  -c ConfigDbContext

# Apply both
dotnet ef database update \
  -p src/CHL.NrbGateway.Infrastructure \
  -s src/CHL.NrbGateway.Api \
  -c KycDbContext

dotnet ef database update \
  -p src/CHL.NrbGateway.Infrastructure \
  -s src/CHL.NrbGateway.Api \
  -c ConfigDbContext
```

### 3. Run the API

```bash
dotnet run --project src/CHL.NrbGateway.Api
```

Swagger UI is available at: `https://localhost:{port}/swagger`

**Dev seed data** is auto-seeded on startup (in-memory or Postgres):
- Admin: `admin@continental.mw` / `Admin123!`
- Subsidiary: CDH Investment Bank (`CDHIB`)
- Dev API key: `chl_live_cdhib_dev_key_12345`

### 4. Run Tests

```bash
dotnet test
```

---

## API Quick Reference

### Gateway (Subsidiary-facing, `X-Api-Key` auth)

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/v1/gateway/verify/intermediate` | PIN + biometric verification |

### Portal (ICT Admin, JWT Bearer auth)

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/v1/portal/auth/login` | Admin login, returns JWT |
| `GET` | `/api/v1/portal/subsidiaries` | List subsidiaries |
| `POST` | `/api/v1/portal/subsidiaries` | Create subsidiary |
| `GET` | `/api/v1/portal/subsidiaries/{id}/api-keys` | List API keys |
| `POST` | `/api/v1/portal/subsidiaries/{id}/api-keys` | Issue/rotate API key |
| `POST` | `/api/v1/portal/subsidiaries/{id}/api-keys/{keyId}/revoke` | Revoke API key |
| `GET` | `/api/v1/portal/settings/tiers` | Get tier settings |
| `PUT` | `/api/v1/portal/settings/tiers/{tier}` | Enable/disable a tier |
| `GET` | `/api/v1/portal/settings/nrb-environment` | Get NRB environment URLs |
| `PUT` | `/api/v1/portal/settings/nrb-environment` | Update NRB environment |
| `GET` | `/health` | Health check |

---

## Security Notes

- **PIN (national_id):** A deterministic HMAC-SHA256 hash is stored in `individuals.national_id_hash` for fast indexed lookups. The raw PIN is stored AES-256 encrypted in `individuals.national_id_encrypted`.
- **API Keys:** Stored as SHA-256 hashes only. Plaintext returned **once only** at creation/rotation.
- **Passwords:** BCrypt-hashed.
- **Production secrets:** Must come from a proper secrets store (Azure Key Vault / AWS Secrets Manager / environment variables). The placeholder values in `appsettings.Development.json` are **not safe for production**.

---

## Cross-Schema Navigation Note

`gateway_requests` in the `kyc` schema references `subsidiary_id` from the `config` schema's `subsidiaries` table. To maintain clean DbContext boundary separation and enable the future split, this is stored as a bare `Guid` with **no EF Core navigation property** across DbContext boundaries.

**To look up a subsidiary for a request**: query `ConfigDbContext.Subsidiaries` separately using the `SubsidiaryId` value. Each subsidiary will only match requests with `GatewayRequest.SubsidiaryId == subsidiary.Id` — the boundary is by subsidiary identity, not by data sharing.

---

## Future Split — Gateway and Portal as Separate Deployables

The code is pre-structured for a clean split:

1. **Move files**: `Api/Gateway/` → `CHL.NrbGateway.GatewayApi/`, `Api/Portal/` → `CHL.NrbGateway.PortalApi/`
2. **Split `Program.cs`**: Each deployable gets its own startup, only registering its own DbContext and auth scheme
3. **Config propagation**: Use Postgres `LISTEN/NOTIFY` on the `config` schema for the Gateway to pick up tier/environment changes pushed by the Portal — no polling needed
4. **No re-architecture**: All interfaces, services, and Domain entities remain unchanged

The two Postgres roles (`gateway_role` / `portal_role`) are already defined with the correct least-privilege grants to support independent deployments.
