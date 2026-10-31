# ADR-0001: Stack and EGFM stance
Status: Accepted · revised 31 Oct 2026 (API is .NET, per the product owner)

**Decision:** New codebase. **Backend: ASP.NET Core on .NET 10**, EF Core (Npgsql) with EF-generated migrations, PostgreSQL 16, structured like the clusterv2 services (ADR-0008). Frontends: Next.js (`nlwc-ems-web`), Vite + React (`nlwc-ems-admin`, `nlwc-ems-checkin`).
**Supersedes** the first draft of this ADR (TypeScript/NestJS). A NestJS API had been built and pushed in one commit; it was ported to .NET with the same schema, behaviour and tests, and removed.
**EGFM:** reuse ideas only, no code, until rights (D-27) and security (D-28) are confirmed. The .NET choice does not change that.
**Consequences:**
- No shared Zod contracts. The API publishes OpenAPI (`/v1/openapi.json`) and the frontends generate typed clients from it. The OpenAPI diff in CI stays.
- Jobs: pg-boss is replaced by a Postgres-backed outbox plus a hosted background worker (Quartz.NET or a small `BackgroundService`), decided when the notifications module starts.
- Tests: xUnit, `WebApplicationFactory`, Testcontainers (real Postgres).
**Why Postgres:** exclusion constraints and partial unique indexes enforce capacity and one-registration rules declaratively.
