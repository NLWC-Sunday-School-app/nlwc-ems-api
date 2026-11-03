# nlwc-ems-api

ASP.NET Core (.NET 10) API for the NLWC Event Management System. MySQL 8 through EF Core (Pomelo, as the Registrar does). Structured the way the
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
  nlwc-ems.Tests/    Services/ + Core/ (Moq unit tests), Integration/ (real app + real MySQL in a container)
  shared/            ui tokens and display-only helpers, installed by web/admin/checkin as git dependencies
  documentation/     requirements, design, engineering plan and ADRs
```

## Run it

```
docker compose up -d mysql
dotnet run --project nlwc-ems.api              # migrates the database on startup; http://localhost:4000
```
Development settings (local MySQL on port 3308, a throwaway master key, non-secure cookie) are in `appsettings.Development.json`.
Anywhere else, supply `ConnectionStrings__EmsDatabase`, `Security__MasterKey` (64 hex chars) and `Cors__AllowedOrigins__0…` (production defaults to `https://events.nlwc.church`; the localhost origins live in `appsettings.Development.json`).

### Production deployment

The production workflow builds pull requests targeting `main` without deploying. A push to `main` or a manual run on `main` builds and publishes a commit-tagged GHCR image, then deploys it to the `prod` GitHub environment. Configure any required environment approval rules in GitHub.

Set `SERVER_HOST`, `SERVER_USERNAME`, `SERVER_KEY`, `SERVER_KEY_PASSPHRASE`, `DOPPLER_TOKEN`, and `GHCR_PAT` as secrets for the `prod` environment. `SERVER_KEY_PASSPHRASE` is the passphrase protecting the SSH private key. The PAT must allow the server to read the GHCR package, and the GitHub Actions token must have permission to publish it. Install Docker Compose and the Doppler CLI on the server; Doppler must provide the production application settings, including a reachable MySQL connection. The API is exposed on server port `8082` for the reverse proxy; confirm that port is free and restrict direct access using the server firewall. The workflow stores the downloaded settings in `~/nlwc-ems-api/.env`.

- Health: `GET /health`. API contract: `GET /swagger/v1/swagger.json` (Development), which the frontends generate clients from.
- First super admin: sign up, then `dotnet run --project nlwc-ems.api --no-launch-profile -- --bootstrap-admin you@example.com`.

## Test it

```
dotnet test        # needs Docker: the integration tests start a MySQL container
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
