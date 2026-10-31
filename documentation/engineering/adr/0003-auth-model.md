# ADR-0003: Auth model
Status: Accepted (Q5 answered 31 Oct 2026)

**Decision:** email + password (argon2id, breached-password check, rate limits, lockout) and Google OIDC, as designed in the prototype. **No phone OTP**; email is required at sign-up. Opaque server-side sessions in httpOnly SameSite=Lax cookies with CSRF tokens; no JWTs in browser storage. Staff: mandatory TOTP MFA.
**Consequences:** people with no email cannot self-register; staff-assisted entry in admin covers them. The beneficiary claim flow verifies by email, not phone. `verified_contact` keeps a `kind` column so phone can be added later without a migration.
**DOB** is collected in a "complete your profile" step before first registration, not at sign-up.
**No auto-linking** of accounts by name or contact alone.
