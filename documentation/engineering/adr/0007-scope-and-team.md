# ADR-0007: Scope for a 3-engineer team and a free Bootcamp
Status: Accepted (Q1, Q3, Q4 answered 31 Oct 2026)

**Facts:** 3 engineers. Bootcamp 2026 is free. Camp is for ages 10–16 only.
**Consequences:**
- **Payments, orders, inventory and Paystack are Phase 2.** The pricing tables exist; Bootcamp publishes explicit `FREE` rules. No payment step in the Bootcamp wizard (edition capability "Paid registration" off).
- **Camp:** `camp_min_age = 10`, `camp_max_age = 16` on the edition. Camp is a request reviewed by a human; roll-call included; no room assignment.
- **Cut line applied now, not at M3** (plan 11.3), with three engineers covering api, web, admin and checkin:
  1. Registration desk: out (staff enter via admin registrations).
  2. Camp review queue UI: out (spreadsheet plus a status change in admin).
  3. Sponsor "register someone else" direct path: out (invite only).
  4. Communications campaigns: out (templates only).
  5. Reports beyond attendance and registration counts: out.
  6. Animated hero and dark-theme polish: last.
  7. Phone OTP: out (Q5).
- **Never cut:** custody verification, per-day records, staff MFA, audit log, restore drill, pre-launch security review.
- Proposed split: Eng 1 api (identity, registration, access, audit) and tech lead; Eng 2 api attendance/custody plus `nlwc-ems-checkin`; Eng 3 `nlwc-ems-web`, `nlwc-ems-admin` and `@nlwc/ui`. QA/DevOps is shared; automate early.
