# ADR-0001: Stack and EGFM stance
Status: Accepted (provisional, pending Q2) · 2026-10-31

**Decision:** New codebase. TypeScript end to end: NestJS API, Next.js web, Vite + React admin, PostgreSQL 16, pg-boss jobs, Drizzle/Kysely with hand-written SQL migrations.
**EGFM:** reuse ideas only, no code, until rights (D-27) and security (D-28) are confirmed.
**Assumption to confirm (Q2):** no EGFM source rights; team knows TypeScript.
**If wrong:** API can be swapped to .NET 10; contracts, schema and frontends are unaffected.
**Why Postgres:** exclusion constraints and partial unique indexes enforce capacity and one-registration rules declaratively.
