# ADR-0004: Check-in PWA is its own repo, `nlwc-ems-checkin`
Status: Accepted (Q6 answered 31 Oct 2026). Supersedes the "inside admin" default.

**Decision:** the check-in PWA is a separate repository and deployable (Vite + React, Workbox, always dark `data-surface="device"`, 56 px targets, device-registered, shorter session than the console).
**Consequences:**
- It cannot import workspace packages directly. It consumes `@nlwc/contracts` and `@nlwc/ui` as versioned packages (private registry or git-tag dependency). Until one is chosen, it links them with `file:` paths in local dev.
- It needs its own CI, and the API's CORS/CSRF config must allow its origin (third origin).
- Auth: staff sign in on the checkin origin with the same API session mechanism, role-scoped to check-in roles only. Session timeout is shorter than the console.
- The offline policy (ADR-0006) is unchanged.
