# ADR-0001: Stack and EGFM stance
Status: Accepted

**Decision:** New codebase. **Backend: ASP.NET Core on .NET 10**, EF Core (Npgsql) with EF-generated migrations, PostgreSQL 16, structured like the clusterv2 services (ADR-0008). Frontends: Next.js (`nlwc-ems-web`), Vite + React (`nlwc-ems-admin`, `nlwc-ems-checkin`).
**EGFM:** reuse ideas only, no code, until rights (D-27) and security (D-28) are confirmed. The .NET choice does not change that.
**Consequences:**
- No shared request contracts. The API publishes OpenAPI and the frontends generate typed clients from it. An OpenAPI diff in CI blocks breaking changes.
- Jobs: a Postgres-backed outbox plus a hosted background worker (Quartz.NET or a small `BackgroundService`), decided when the notifications module starts.
- Tests: xUnit, `WebApplicationFactory`, Testcontainers (real Postgres).
**Amendment (2026-11-03): the database is now MySQL 8** through Pomelo (as the Registrar), which pins the API to EF Core 9. Partial unique indexes became stored generated columns plus a unique index; the append-only and immutability triggers were rewritten for MySQL; the audit chain lock is a row lock; the exclusion-constraint idea below is not used. The paragraph below records the original reasoning.
**Why Postgres:** exclusion constraints and partial unique indexes enforce capacity and one-registration rules declaratively.
