# ADR-0002: One repository per app; shared packages live in the API repo
Status: Accepted (revisit after Q7) · updated 31 Oct 2026

**Decision:** `nlwc-ems-api`, `nlwc-ems-web`, `nlwc-ems-admin` and `nlwc-ems-checkin` are separate repositories. The shared UI and display-only domain packages live in `nlwc-ems-api/shared/{ui,domain}` and the frontends install them as git dependencies (`github:Oduology/nlwc-ems-api#path:/shared/<pkg>`). The API repo also holds the documentation.
**Risk:** contract drift between repos, and a private API repo needs a token for the frontends' CI to fetch the shared packages. Mitigation: the frontends generate their clients from the API's OpenAPI document, and CI supplies a read token.
