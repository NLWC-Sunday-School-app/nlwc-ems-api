# ADR-0008: The API follows the clusterv2 service architecture
Status: Accepted · 31 Oct 2026

**Decision:** `nlwc-ems-api` is structured like the clusterv2 services (`clusterv2-core`, `-auth`, `-survey`…), with EF Core as the
data engine. The first version used a flat minimal-API layout with hand-written SQL; it was replaced.

## What is the same
- One `.api` project and one `.Tests` project on a shared `Base` library.
- Entity (`BaseEntity`, `IFilterable`) → EF `IEntityTypeConfiguration` → DAO (`IBaseDao`/`BaseDao`, snake_case via
  `EFCore.NamingConventions`) → service (`IBaseService`/`BaseService` with `PreSaveAsync`/`PreUpdateAsync`, `ServiceResponse`) →
  controller (`BaseApiController`, `ApiResponse` envelope, `ExceptionHandlingMiddleware`).
- DAOs and services are registered by namespace scan (Scrutor); DTOs carry per-field DataAnnotations only; cross-field rules live in services.
- EF migrations generated with `dotnet ef`, applied at startup with `Database.Migrate()`. Seed data (roles, templates) is `HasData`.
- Permission checks are `ViewPermissionCodes` / `*PermissionCode` overrides and `[RequirePermission]`; generic status/delete are
  hidden with `[NonAction]` where they don't belong. `[Auditable]` entities are audited by `BaseDao`.
- Tests: Moq + FluentAssertions unit tests of services and rules, plus tests that need the real host (routes, exposure).

## Where EMS deliberately differs, and why
- **`nlwc-ems.Base` is our own library, not a reference to Qore's `clusterv2.Base`.** The real Base is Qore's code, and is wired to
  Qore's JWT issuer, RabbitMQ audit relay and approval service. EMS is a separate NLWC project. The types mirror Base's names and
  shapes so the two stay recognisably the same, and a swap to the real thing would be a small change.
- **Cookie sessions, not JWTs.** Opaque server-side sessions in an httpOnly cookie, with CSRF double-submit and an origin allow-list
  (ADR-0003). `SessionAuthenticationHandler` turns the session into claims, so the rest looks like clusterv2: permissions are claims.
- **Permissions are `resource:level` with levels, scoped to an edition**, not flat codes. A held level carries the lower ones
  (`health:full` includes `health:flags`); an edition-scoped grant yields `code@editionId`.
- **No maker-checker service.** Separation of duties for prices is enforced in the service and by a database `CHECK`
  (drafter ≠ approver). Generic approvals can be added later if a second use appears.
- **Audit goes to our own hash-chained, append-only table**, through the same `IAuditEventPublisher` seam, instead of RabbitMQ to an
  audit service. `AuditEvent` is therefore not a `BaseEntity`: it is immutable and ordered by identity, not by timestamps.
- **`ApiResponse` has a `code`.** Clients branch on `mfa_required` or `dob_locked` rather than on message text.
- **Owner-scoped flows are plain controllers** (profile, dependants, collectors, auth), not `BaseApiController`s. The base
  controller's by-id actions are not owner-safe: exposing it for these would let any signed-in user list everyone.
  `ArchitectureTests` pins this: every action needs sign-in unless on a short allow-list, and routes can't collide.

## Consequences
- The earlier hand-written SQL migrations were replaced by one EF `InitialCreate` (with the triggers added as raw SQL). Table names
  are plural snake_case (`people`, `accounts`, `role_grants`), status columns are string enums, and every table carries `BaseEntity`'s
  created/modified stamps. No production data existed.
- Routes are `api/v1/[controller]` and every body is the envelope. The frontends had not started calling the API.
- Postgres still enforces the invariants that matter (one active registration per person per edition, immutable published rates,
  append-only history, money as integer kobo); EF just declares most of them.
