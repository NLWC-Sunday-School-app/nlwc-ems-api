# nlwc-ems-api
ASP.NET Core (.NET 10) modular monolith, PostgreSQL 16. See `documentation/engineering/ENGINEERING_PLAN.md`.

```
docker compose up -d postgres
cp .env.example .env   # export the variables
dotnet run --project src/NlwcEms.Api -- --migrate
dotnet run --project src/NlwcEms.Api
# first super admin (sign up first, then):
dotnet run --project src/NlwcEms.Api -- --bootstrap-admin you@example.com
dotnet test            # needs Docker (Testcontainers)
```
- `migrations/` forward-only SQL, embedded in the API assembly.
- `shared/ui` design tokens and components; `shared/domain` display-only helpers. Web, admin and checkin install these as git dependencies.
- The API contract is OpenAPI at `/v1/openapi.json`; frontends generate typed clients from it.
