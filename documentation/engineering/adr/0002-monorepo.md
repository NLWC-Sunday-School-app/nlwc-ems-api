# ADR-0002: Monorepo for api, web, admin and shared; check-in is separate
Status: Accepted (provisional, pending Q7) · updated 31 Oct 2026

**Decision:** one repo (pnpm workspaces + Turborepo) holds `nlwc-ems-api`, `nlwc-ems-web`, `nlwc-ems-admin` and `nlwc-ems-shared/{contracts,ui,domain}`. `nlwc-ems-checkin` is a separate repo (ADR-0004) that depends on published versions of the shared packages.
**Risk:** contract drift between the two repos. Mitigation: OpenAPI diff in the API's CI, and the checkin repo pins a contracts version and runs a contract test against staging.
