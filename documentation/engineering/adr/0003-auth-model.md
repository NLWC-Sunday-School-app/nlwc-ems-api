# ADR-0003: Auth model
Status: Accepted (Q5 answered 31 Oct 2026)

**Decision:** email + password (argon2id, breached-password check, rate limits, lockout) and Google OIDC, as designed in the prototype. **No phone OTP**; email is required at sign-up. Opaque server-side sessions in httpOnly SameSite=Lax cookies with CSRF tokens; no JWTs in browser storage. Staff: mandatory TOTP MFA.
**Consequences:** people with no email cannot self-register; staff-assisted entry in admin covers them. The beneficiary claim flow verifies by email, not phone. `verified_contact` keeps a `kind` column so phone can be added later without a migration.
**DOB** is collected in a "complete your profile" step before first registration, not at sign-up.
**No auto-linking** of accounts by name or contact alone.

**Google sign-in (built 1 Nov 2026):** server-side authorization code + PKCE, run by the API (`/api/v1/auth/google/start` and `/callback`); the browser never holds a Google token and ends up with the same session cookie as a password login. Web surface only; staff and gate sessions stay password + MFA. **Linking rule:** a Google identity joins an existing account only if Google reports `email_verified` and that account's email is already verified (otherwise: log in with the password first). A new Google user gets a password-less account with a verified email contact. The ID token comes straight from Google's token endpoint over TLS, so its signature is not re-checked (OIDC Core 3.1.3.7); issuer, audience, expiry and nonce are.

