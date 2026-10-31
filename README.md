# nlwc-ems-api

ASP.NET Core (.NET 10) API for the NLWC Event Management System. PostgreSQL 16 through EF Core. Structured the way the
clusterv2 services are; see [ADR-0008](documentation/engineering/adr/0008-clusterv2-service-architecture.md).

```
nlwc-ems.slnx
  nlwc-ems.Base/     shared infrastructure: BaseEntity, BaseDao, BaseService, ServiceResponse, ApiResponse, BaseApiController, middleware
  nlwc-ems.api/      the service
    Controllers/       one controller per concern; inherit BaseApiController for generic CRUD
    Core/Models/       entities (BaseEntity + IFilterable), Filters/, Enums/, Security/
    Data/              DAO interfaces (IDaos.cs)
    Data.EFCore/       EmsDbContext, EntityConfigurations/, Implementations/ (DAOs), Migrations/
    Services/          Interfaces/, Implementations/, Options/
    Dtos/              request and response types (DataAnnotations only)
  nlwc-ems.Tests/    Services/ + Core/ (Moq unit tests), Integration/ (real app + real Postgres in a container)
  shared/            ui tokens and display-only helpers, installed by web/admin/checkin as git dependencies
  documentation/     requirements, design, engineering plan and ADRs
```

## Run it

```
docker compose up -d postgres
dotnet run --project nlwc-ems.api              # migrates the database on startup; http://localhost:4000
```
Development settings (local Postgres, a throwaway master key, non-secure cookie) are in `appsettings.Development.json`.
Anywhere else, supply `ConnectionStrings__EmsDatabase`, `Security__MasterKey` (64 hex chars) and `Cors__AllowedOrigins__0…`.

- Health: `GET /health`. API contract: `GET /swagger/v1/swagger.json` (Development), which the frontends generate clients from.
- First super admin: sign up, then `dotnet run --project nlwc-ems.api --no-launch-profile -- --bootstrap-admin you@example.com`.

## Test it

```
dotnet test        # needs Docker: the integration tests start a Postgres container
```

## Change the database

```
dotnet ef migrations add <DescriptivePascalCaseName> -o Data.EFCore/Migrations --project nlwc-ems.api
```
Run `git diff -- nlwc-ems.api/Core/Models` first: EF bundles every uncommitted model change into the migration. Triggers and
other raw SQL go in the migration by hand (see `InitialCreate`). Migrations are forward-only.

## Conventions worth knowing

- Every response is the `ApiResponse` envelope (`success`, `message`, `code`, `data`). `code` is a stable machine-readable
  reason (`mfa_required`, `dob_locked`, `pricing_not_published`…); `message` is for people.
- Permissions are `resource:level` codes (`config:edit`) with ordered levels. They reach controllers as claims, so use
  `[RequirePermission(...)]` for a one-off action, or the base controller's `ViewPermissionCodes` / `*PermissionCode` overrides.
  Staff permissions only count on an MFA-verified session.
- Business-rule failures come back as `ServiceResponse` (expected) or as `AppException`/`ValidationException` (unexpected or
  edge), which the exception middleware turns into the envelope.
