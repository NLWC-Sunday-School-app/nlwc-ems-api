# NLWC EMS: Engineering Plan

**Applications:** `nlwc-ems-web` (attendee portal) · `nlwc-ems-admin` (staff console + check-in PWA) · `nlwc-ems-api` (backend)
**Status:** Draft v0.1 for review, 31 Oct 2026 · **Hard date:** Bible Bootcamp, Mon 21 – Wed 23 Dec 2026 (51 days)
**Inputs:** BRD v3.1, EGFM Reuse & Gap Analysis v1.1, Pricing Handoff v3.1, [FRONTEND_DESIGN.md](../design/FRONTEND_DESIGN.md), [DESIGN_SYSTEM.md](../design/DESIGN_SYSTEM.md), the HTML prototypes in `documentation/design/` (portal, admin, design-system page, email template), all reviewed in full

> Facts here come from those documents. Anything I am recommending rather than reading from them is marked **Recommendation**. Anything that needs a decision from you or NLWC is in section 2, and referenced as **Q#** in the text.

---

## 1. The plan in one page

1. **Build one modular backend, two thin frontends.** All rules (eligibility, pricing, capacity, custody, consent, permissions) live in `nlwc-ems-api`. Web and admin render state and send intent; they never decide money, eligibility or who may collect a child.
2. **Design for all three event types from day one, ship Bible Bootcamp first.** The data model, pricing engine and event templates cover Bootcamp, Singlz and Couples. The 51-day build delivers only what Bootcamp needs (section 11). Singlz accommodation and payments follow in January.
3. **Child safety is the release gate.** Per-day check-in and checkout with authorised-collector verification must pass a real-device rehearsal before 21 Dec. If something has to slip, it will not be this.
4. **Money and inventory correctness are built in from the first migration, not retrofitted.** Integer kobo, server-side quotes, price snapshots, idempotent webhooks, database-enforced capacity. Bootcamp 2026 may be free, but the pricing and order tables still exist so Singlz does not need a rewrite.
5. **Do not reuse EGFM code until rights and security are confirmed** (BRD D-27, D-28). The plan assumes a new codebase that reuses EGFM *ideas*, and is cheap to change if the audit later says reuse is safe.

**Milestones (week numbers count from Mon 2 Nov):**

| Date | Milestone | Exit criteria |
|---|---|---|
| Fri 6 Nov | **M0 Foundations** | Decisions Q1–Q8 answered; repos, CI, staging live; schema v0 and API contract v0 agreed |
| Fri 20 Nov | **M1 Internal alpha** | Sign up → profile → add child → register for Bootcamp works end to end on staging, in web |
| Fri 27 Nov | **M2 Public registration opens** | Real families can register; consent, notifications and passes work; admin can see registrations |
| Fri 4 Dec | **M3 Operations ready** | Staff roles and MFA, registrations queue, camp review, reports, comms |
| Fri 11 Dec | **M4 Check-in complete + code freeze** | Per-day check-in/out, collector verification, exceptions; feature freeze, fixes only |
| 14–18 Dec | **M5 Rehearsal** | Gate drills on real phones, load test, backup restore drill, SOPs printed |
| Sat 19 Dec | **Go / no-go** | Release gates in section 11.4 all green |
| 21–23 Dec | **Event** | Daily ops, on-call, hypercare |
| Jan 2027 | Phase 2 (Singlz) → Phase 3 (Couples) | Section 11.5 |

---

## 2. Decisions needed from you (ranked by what they block)

Where I have a default I will proceed on it, and I'll change it if you disagree. **Blocking** means I cannot finish a milestone without an answer.

### Blocking before M0 (this week)

| # | Question | Why it matters | My default |
|---|---|---|---|
| **Q1** | **Team and capacity:** how many engineers (backend, frontend, mobile/PWA, QA/DevOps), and who? Is it you plus me, or a team? | The 51-day plan assumes roughly 4 to 5 engineers. With fewer, scope must be cut harder (section 11.3). | Plan as if 4 engineers. |
| **Q2** | **Tech stack and EGFM reuse.** Do you have source/IP rights to EGFM Registrar (D-27), and does your team already know .NET 10 or TypeScript? | Decides the whole backend. Reusing EGFM is a legal and security question first. | **Recommendation:** new codebase, **TypeScript** end to end, **PostgreSQL**. See section 4. |
| **Q3** | **Is Bible Bootcamp 2026 free, including camp?** (D-02) The prototype and event page show "Free". | If free, Paystack and orders move out of the 51-day scope. If any fee exists, payments are in scope and the timeline gets much tighter. | Free. Payments in Phase 2. |
| **Q4** | **Is camping available in 2026, and for which ages?** (D-02, D-03) The event page says camp 10–16, reviewed by the team. | Camp review, roll-call and room assignment are real work. Without camping there is no camp review queue. | Camp is a request; human review; no beds assigned. Roll-call included, room assignment out. |
| **Q5** | **Sign-in and identity method.** The BRD and portal design are phone-first: profile shows a *verified phone* with email optional, the claim flow ends with "create my profile with this phone number", and the sponsor form takes "phone or email". The sign-up page we just built requires an email and a password. | These are different products. A required email locks out people with no email or smartphone (BRD 6.3), and breaks the claim flow. | An account needs **at least one verified contact (phone or email)**. Email + password and Google as designed, **plus** phone-OTP for people without email. Sign-up makes email optional if a phone is given. Needs an SMS vendor (Q10). |
| **Q6** | **Where does the check-in PWA live?** You named 3 apps; the BRD and design system describe a 4th client. | One fewer repo and shared login if it sits inside admin, but it needs its own installable shell and device theme. | Inside `nlwc-ems-admin` at `/checkin`, installable, role-scoped. |

### Needed by M1 (by Fri 13 Nov)

| # | Question | Default |
|---|---|---|
| **Q7** | **Hosting and budget.** Cloud/provider, region, managed Postgres allowed, monthly budget, domain names (for example `events.nlwc.church`). | Containers on one managed platform, managed Postgres, one region, staging and production. |
| **Q8** | **Safeguarding SOP owner.** Who signs off the collector-verification procedure, late-pickup escalation, lost-child process (D-14, D-15)? | I draft the in-app flow; NLWC safeguarding lead approves before M4. No child can be checked out without verification. |
| **Q9** | **Legal sign-off.** Who approves Terms of Use, Privacy Notice, guardian, medical and media consent wording (D-18)? Is there a privacy lead? | Wording is versioned content, loaded from admin; placeholders block public launch. |
| **Q10** | **Vendors.** Email (SendGrid was used by EGFM), SMS/WhatsApp for OTP and notices, and later Paystack (D-16). | Email at M2. SMS for OTP if Q5 default is accepted. WhatsApp deferred. Paystack in Phase 2. |
| **Q11** | **Who are the staff users at the gate?** Rough numbers, devices (own phones vs church tablets), network at Faith Plaza. | Own Android phones; flaky network assumed; offline-safe arrival scanning only. |

### Needed before they bite (can wait)

| # | Question | When |
|---|---|---|
| Q12 | 17+ participants: any extra rules beyond daily attendance (D-04)? | M2 |
| Q13 | Studio team and Parent Interactive Forum details (D-19). The FAQ now says no forum this year. | M3 |
| Q14 | Photo/ID on passes (D-17), and what online participants receive (D-22). | M2 |
| Q15 | Data retention periods and staff access to medical data (D-18). | M3 |
| Q16 | Singlz and Couples decisions: dates, chalet pairing basis, 7–12 non-hostel rate, Couples fee basis (D-05 to D-10, D-24 to D-26). | Before Phase 2 |
| Q17 | Is the "Giving" account (FAQ #10) a static bank account, or should the app take giving? | M2 (content only) |
| Q18 | FAQ #6 says "children age 17 and above". Is the camp age 10–16, and do 17+ walk in only? | M1 (content) |
| Q19 | **Event name:** the BRD says *Couples Programme*; the design system, portal and admin say *Couples Retreat*. Which is official? | M1 (content) |
| Q20 | **Does an unanswered photo/video answer hold a child's pass as "Pending"?** The wizard says so ("Tobi's passes stay Pending until his photo permission is answered") while the BRD says media never blocks registration. Answering *No* must never block; *unanswered* is the open question. | M2 |
| Q21 | **Camp capacity** (admin shows "Capacity not set, approvals stay disabled") and whether camp approvals happen at all before 21 Dec. Overlaps Q4. | M3 |
| Q22 | **Design assets:** SVG logo and a mark-only icon (DS-4), official brand values (DS-1), approval of event accents and the Marcellus display font (DS-2, DS-3). The PNG logos are prototype-only. | M1 |

---

### Answers recorded 31 Oct 2026

| Q | Answer | Effect |
|---|---|---|
| Q1 | 3 engineers | Cut line applied now (ADR-0007) |
| Q2 | Default | New TypeScript codebase on Postgres (ADR-0001) |
| Q3 | Bootcamp is free | Payments are Phase 2 |
| Q4 | Camp ages 10–16 only | `camp_min_age=10`, `camp_max_age=16` |
| Q5 | Email + password and Google; no phone OTP | Email required at sign-up (ADR-0003) |
| Q6 | Separate repo `nlwc-ems-checkin` | ADR-0004, ADR-0002 updated |
| Q7–Q22 | Still open | Q7 needed by 13 Nov |

---

## 3. Ground truth and constraints

- **Date:** event days are fixed. Registration should open about 3.5 weeks before (27 Nov).
- **EGFM evidence is documentary.** The reuse analysis says plainly it did not inspect the repositories. There are no code assets in this repo yet. Only design prototypes exist.
- **Reported EGFM defects to design out:** frontend-calculated prices, per-statement financial commits, secrets in source, weak JWT validation, destructive archive on event close, single-process 5-second worker, one `CheckedIn` flag.
- **No attendee data migration** from EGFM by default (D-30).
- **Targets:** low-end Android over congested mobile networks; admin MFA; Africa/Lagos for event time, UTC in storage.
- **Design prototypes** in `documentation/design/` are the UX source of truth (14 portal screens, 12 admin screens, check-in device). They are static; there is no backend.

---

## 4. Architecture

### 4.1 System context

```
   Attendees / guardians / sponsors                Staff (event, finance, safety, gate)
              │                                                  │
      ┌───────▼────────┐                               ┌─────────▼─────────┐
      │  nlwc-ems-web  │                               │  nlwc-ems-admin   │
      │ attendee portal│                               │ console + /checkin│
      └───────┬────────┘                               └─────────┬─────────┘
              │      HTTPS, JSON, cookie sessions, CSRF          │
              └──────────────────────┬───────────────────────────┘
                              ┌──────▼───────┐
                              │ nlwc-ems-api │  modular monolith
                              └──┬───┬───┬───┘
              ┌──────────────────┘   │   └─────────────────┐
        ┌─────▼─────┐        ┌───────▼──────┐       ┌──────▼───────┐
        │ PostgreSQL│        │ Job worker   │       │ Object store │
        │ (source of│        │ (same codebase│      │ (PDFs, QR,   │
        │  truth)   │        │  outbox + cron)│     │  exports)    │
        └───────────┘        └───────┬──────┘       └──────────────┘
                                     │
              Email · SMS/WhatsApp · Paystack (Phase 2) · Error + metrics
```

### 4.2 Recommendation: stack and why

| Layer | Choice | Reasoning |
|---|---|---|
| Language | **TypeScript** everywhere | One language across three apps, shared contracts and types, fastest path for a small team in 51 days. If Q2 says the team is .NET-strong and EGFM reuse is cleared, swap the API to .NET 10; every other section still applies. |
| API | **NestJS** (modules map to bounded contexts) on Node LTS; **Zod** schemas for validation; OpenAPI generated from them | Enforces module boundaries; generated typed client for both frontends. |
| Database | **PostgreSQL 16** | Needed for the invariants the BRD demands: partial unique indexes (one active registration per person per edition), **exclusion constraints** on date ranges (no two active assignments for one bed on one day), transactional row locks for holds, JSONB for form responses. MySQL (EGFM) cannot enforce the bed/date rule declaratively. |
| ORM / SQL | **Drizzle** or **Kysely** with hand-written migrations | Explicit SQL for constraints and transactions; no hidden lazy loading. |
| Jobs | **pg-boss** (Postgres-backed queue) + transactional **outbox** | Durable, multi-replica safe, no extra infrastructure. Replaces EGFM's in-process 5-second loop. |
| Web | **Next.js** (App Router), React, server components for public pages | SEO and fast first paint on public event pages; client components for wizards. |
| Admin | **Vite + React SPA** with TanStack Router/Query | Auth-gated, no SEO need, simplest build; PWA shell for `/checkin` via Workbox. |
| UI | Shared **`@nlwc/ui`** package built from the existing `tokens.css` and `components.css` | The prototypes already define tokens, status badges, price states. Do not redesign in code. |
| Auth | Server-side sessions in httpOnly, SameSite cookies; **argon2id**; TOTP MFA for staff; Google OIDC; optional phone OTP | No JWTs in browser storage; revocable; removes the EGFM JWT-validation class of bugs. |
| Observability | OpenTelemetry → provider of choice; Sentry for errors; structured JSON logs | Required by BRD section 24 (payment mismatch, over-allocation, missing child alerts). |

### 4.3 Repositories

Three deployables, as you asked, plus one shared package:

```
nlwc-ems-api/      NestJS app, migrations, worker entry point
nlwc-ems-web/      Next.js app
nlwc-ems-admin/    Vite SPA (+ /checkin PWA)
nlwc-ems-shared/   (npm workspace or private package)
   ├─ contracts/   Zod schemas + generated OpenAPI client
   ├─ ui/          tokens, components (from documentation/design)
   └─ domain/      pure functions used by both sides for display only
                   (age band, price-state formatting). Never authoritative.
```

Recommendation: a single monorepo (pnpm workspaces + Turborepo) with the three apps and shared package as folders. It keeps contracts in lockstep and CI simple. Three separate git repos remain possible if NLWC prefers; say so under Q7.

### 4.4 Environments

`local` (docker compose: Postgres, MinIO, MailHog) → `staging` (auto-deploy from `main`, seeded with synthetic data only) → `production` (manual promote, migrations gated). Preview deployments per PR for web and admin. No real child data in non-production, ever.

---

## 5. `nlwc-ems-api`

### 5.1 Modules (bounded contexts)

Each module owns its tables and exposes a service interface. Cross-module calls go through those interfaces only; no module reads another's tables. This follows the "separate domain concepts" rule in the gap analysis.

| Module | Owns | Key rules (BRD ref) |
|---|---|---|
| **identity** | Person, Account, VerifiedContact, ClaimToken, Session, MFA | One stable Person ID; duplicates reviewed by a human, never auto-merged (PRO-02, PRO-01) |
| **family** | PersonRelationship (spouse, guardian, dependant, collector), GuardianAuthorization | Dependants 0–12 are Persons; 13+ have their own profile; guardianship is separate from payment (PRO-03, PRO-04, PRO-05) |
| **catalog** | Organization, EventTemplate, EventEdition, EventDay, Venue, EventPolicyVersion, FormDefinition, **EditionCapability**, edition lifecycle | Template ≠ edition; clone config only, never people (EVT-01 to EVT-03, AT-16). Capabilities gate screens **and** API per edition (5.11) |
| **registration** | Registration (person × edition), RegistrationActor, StatusHistory, Invitation, Sponsor link | Partial unique index: one active registration per person per edition. Registrant ≠ beneficiary (REG-01 to REG-05) |
| **consent** | ConsentDefinition(versioned), ConsentRecord, HealthDisclosure, MediaPreference | Per child, per event, versioned, actor and time recorded; media "no" never blocks registration; health data restricted (SEC-01, BB-06, AT-14) |
| **pricing** | RatePlan, RatePlanVersion, RateRule, RateApproval, PriceQuote, QuoteLine | Integer kobo; free (₦0) must be an explicit published rule; unconfigured blocks checkout; drafter ≠ approver; copy-as-draft only (PRICE-01 to PRICE-04) |
| **orders** *(Phase 2)* | Cart, Order, OrderItem, OrderAllocation, PaymentAttempt, PaymentTransaction, Refund, LedgerEntry | Payer independent of beneficiary; webhook idempotent on provider event ID (PAY-01 to PAY-03, AT-13) |
| **inventory** *(Phase 2)* | Category, Unit, BedSlot, InventoryDate, Booking, BookingOccupant, Hold, Allocation, Stay | DB exclusion constraint on bed × date; holds expire; paid-but-unallocated queue (INV-01 to INV-03, AT-01) |
| **credentials** | EventCredential, CredentialScan | One revocable credential per person per edition; opaque token, no personal data in the QR (REG-06) |
| **attendance** | EventEntry, DayAttendance, CampStay, CampRollCall, AuthorizedPickup, CustodyHandover, Incident, AttendanceException | One row per person per day; append-only; no "force success" (BB-03 to BB-05, OPS-02, AT-08 to AT-11) |
| **studio** | StudioRequest (per child, per edition) | Interest only; no automatic selection; roster is a coordinator task (BB-07, D-19) |
| **notifications** | Template, Notification, Delivery, Preference | Beneficiary and payer messaged separately; marketing opt-in separate from event messages (COM-01 to COM-03) |
| **access** | Role, RoleGrant(scope: org or edition), Permission, AuditEvent | Event-scoped RBAC; medical and finance never bundled with general admin (OPS-01, AT-17, SEC-02) |
| **reporting** | Read models, ExportJob | Numbers reconcile to source rows (RPT-01) |

### 5.2 Non-negotiable invariants (enforced in the database, not only in code)

| Invariant | Mechanism |
|---|---|
| One active registration per `(person_id, edition_id)` | Partial unique index on non-cancelled rows |
| One day-attendance row per `(person_id, edition_id, day)` | Unique constraint; corrections are new audited rows |
| No double-booked bed on a day | `EXCLUDE USING gist (bed_id WITH =, stay_range WITH &&) WHERE (active)` |
| Money is integer kobo, with currency | `bigint` columns; no floats anywhere |
| Provider payment reference unique | Unique index on `(provider, provider_event_id)` |
| Published rate versions are immutable | Rows are never updated once published; changes create a new effective-dated version |
| Audit and custody history is append-only | Insert-only tables; DB role without UPDATE/DELETE; periodic hash chain checkpoint |
| Timestamps UTC; event rules evaluated in Africa/Lagos | `timestamptz`; age computed at the edition's reference date |
| Age band and price snapshotted on registration/quote | Stored with the rule version used (GAP-06) |

### 5.3 State machines (separate, not one status field)

Implemented as explicit transition tables with the actor, permission and side effects per transition, as the BRD section 21 requires:

- **Registration:** `DRAFT → PROVISIONAL_PENDING_CLAIM → SUBMITTED → CONFIRMED → COMPLETED`, plus `NEEDS_ACTION`, `CANCELLED`, `REJECTED`, `EXPIRED`
- **Payment** *(P2)*: `NOT_REQUIRED | UNPAID → PENDING → PAID`, `FAILED`, `REFUND_PENDING → REFUNDED`, `CHARGEBACK`
- **Booking** *(P2)*: `QUOTED → HELD → PENDING_PAYMENT → CONFIRMED → ALLOCATED → OCCUPIED → CHECKED_OUT`
- **Day attendance:** `EXPECTED → CHECKED_IN → CHECKED_OUT`, with `ABSENT`, `LATE_PICKUP`, `ESCALATED`
- **Camp occupancy:** `EXPECTED → ARRIVED → OCCUPIED → DEPARTED`, with `TEMPORARILY_OUT`, `NO_SHOW`, `EXCEPTION`
- **Account claim:** `CLAIM_INVITED → CLAIM_VERIFIED → PROFILE_COMPLETED`, with `EXPIRED`, `DECLINED`, `DISPUTED`
- **Spouse pairing** *(P2)*: `PENDING_DETAILS → LINK_PENDING → LINKED → PAIRING_PENDING_PAYMENT → PAIRING_CONFIRMED`

The portal's "four status axes" (Registration, Payment, Booking, Pass) map one-to-one to these.

### 5.4 Pricing engine (Phase 1 builds the model; Phase 2 turns on charging)

- A **RatePlan** belongs to one edition. It has **versions** with states `draft → approved → published → retired`, effective-dated.
- A **RateRule** matches on age band, participation mode, accommodation category and eligibility, and has exactly one of: `AMOUNT(kobo)`, `FREE` (explicit ₦0), or no row (**unconfigured, blocks checkout**). This is the "Amount / Free / Unconfigured / Not offered" matrix in the admin prototype.
- `POST /quotes` returns a signed quote with line items, the rule versions used, the age on the reference date, an expiry and a `blocked` list with reasons ("rate pending approval"). The client sends the quote ID, **never an amount**.
- Orders copy quote lines as snapshots. Changing next year's rates cannot change a past order, receipt or report (PRICE-02, AT-02).
- Bootcamp 2026: if Q3 is "free", the edition publishes an explicit `FREE` rule for each applicable band, which satisfies PRICE-03 and keeps the event page showing "Free".

### 5.5 Payments *(Phase 2, designed now)*

Paystack (or the vendor chosen under Q10): initiate from an order; verify the webhook signature, amount, currency and order; record the raw event first, then process it exactly once by unique provider event ID inside one DB transaction (fulfil order, confirm inventory, write ledger entries, enqueue notification, enqueue credential). A reconciliation job compares provider settlements to the ledger daily and alerts on mismatches. Manual cash payments need a separate permission, evidence and approval. Refunds create reversing ledger entries; they never delete history.

### 5.6 Inventory and holds *(Phase 2, designed now)*

Holds are rows with `expires_at`, created and confirmed in one transaction with `SELECT … FOR UPDATE` on the bed or unit. The exclusion constraint is the last line of defence. A job releases expired holds. If payment succeeds after a hold expired and capacity is gone, the order lands in the **paid-but-unallocated queue** with a remedy (reallocate or refund); it is never shown as "confirmed bed" (GAP-19, AT-01).

### 5.7 Attendance and custody (Phase 1, safety-critical)

- Scan modes are explicit and each has its own authorisation rule: `EVENT_ENTRY`, `DAY_CHECKIN`, `DAY_CHECKOUT`, `CAMP_ARRIVAL`, `CAMP_DEPARTURE`, `SESSION`, `MINOR_HANDOVER`.
- **Checkout of a child never succeeds on a QR scan alone.** The API requires `collector_id` (from the registered authorised list) *and* an `identity_verified: true` confirmation from the officer, or it returns `ESCALATE`. An unlisted adult routes to a supervisor workflow that records who approved and why (BB-05, AT-09).
- 17+ walk-ins skip the custody flow (BB-02, AT-11).
- Camper stay, daily roll-call and day check-in are three separate records (BB-04, AT-10).
- **Offline policy:** the PWA may queue `EVENT_ENTRY`, `DAY_CHECKIN` and `CAMP_ARRIVAL` scans locally with a device timestamp and sync with conflict reporting. `DAY_CHECKOUT` and `MINOR_HANDOVER` **require a live connection**; if offline, the screen says so and points to the printed manual SOP (OPS-03, GAP-50).

### 5.8 Authentication and authorisation

- **Accounts:** email + password (argon2id, breached-password check, rate limits and lockout), Google OIDC, optional phone OTP (Q5). Email and phone verified before use. No auto-linking by name or contact alone (PRO-02, AT-18).
- **Sessions:** opaque server-side sessions, httpOnly + Secure + SameSite=Lax cookies, CSRF token on mutations, rotation on privilege change, absolute and idle timeouts, remote revoke.
- **Minors:** guardian-managed dependants have no account. 13+ participants may have their own account; guardian controls still apply to minor activities (PRO-04, AT-19).
- **Staff:** mandatory TOTP MFA; roles granted at organisation or edition scope with expiry; permission checks in a single policy layer (`can(actor, action, resource, edition)`).
- **Permissions have levels, not just on/off.** The admin "Staff & roles" screen is a role × capability matrix with levels: **Pricing** (Draft / Approve), **Money** (View / Edit), **Health** (Flags only / View / Full·logged), **Custody** (Scan only / Act / View), **Config** (View / Edit). Model a permission as `(resource, level)` and check `can(actor, resource, level, edition)`. Roles in the prototype: Super Admin, Event Admin, Finance Officer, Child Check-in Officer, Child Safety Officer, Accommodation Manager, Medical/Welfare, Volunteer/Usher; the BRD adds Registration Officer, Camp Coordinator, Communications Officer, Auditor.
- **Separation of duties:** the person who drafts a rate plan cannot approve it (needs a different person with the *Finance Approver* permission); an override of a custody exception needs a supervisor role and a reason. Enforced in the service layer and tested.
- **Grant metadata:** each grant records scope (church-wide or edition), who granted it, when, and an expiry. Elevated roles require MFA. Access to Singlz roles never exposes Bootcamp data.
- Medical and finance are never included in a generic admin role. A test generates a request for every endpoint × role × level and asserts allow/deny from the matrix (AT-17).
- **Sensitive reads** (health, collector lists, minors' contact details) write an access log row.

### 5.9 Notifications

Outbox pattern: business transaction writes the message row; the worker sends and records the provider result; retries with backoff. Templates are versioned content with merge fields (the confirmation email in `documentation/design/email/` is the first template). Beneficiary and payer get separate messages; marketing is a separate preference, never pre-ticked (COM-01 to COM-03, SEC-01).

### 5.10 Files and exports

PDFs (passes, receipts, manifests) are generated by the worker into the object store with signed, short-lived download URLs. Exports are jobs, permission-checked per edition, logged, and expire.

### 5.11 Edition lifecycle and capabilities (from the admin "Event editions" screen)

- **Lifecycle:** `Draft → Preview → Registration open → Closed → Live → Completed → Archived`. Visibility, registration window and live-event state are separate fields with validated transitions (GAP-02). *Archived* is a status; nothing is copied or deleted (GAP-43).
- **Capabilities** are per-edition switches: Accommodation, Daily child check-in/out (3–16), Camp review, Studio requests, Parent Interactive Forum, Paid registration. "Switching a capability off removes its screens and API for this edition." Implement as an `edition_capability` table; every endpoint for a capability returns `404 capability_disabled` for editions without it, and the frontends build menus and wizard steps from the capability list. This is how Couples gets no room UI and how a free Bootcamp has no payment step.
- **Edition settings** the designs expect as data: dates, timezone, **age reference date** (default: edition start), venue, **pick-up window**, camp age range and capacity, last-day pick-up rule, FAQ and copy, consent documents and their version IDs, rate plan.
- **Clone** copies structure and rules, never people, payments or health data (AT-16).

### 5.12 Search, references and duplicate detection

- **Global admin search** by name, phone or reference (top bar in the admin design). Phones are normalised to E.164 and indexed (plus a trigram index for names). Every search that returns minors or contact details writes an access-log row; results are limited to the caller's edition scope.
- **Human-readable references** such as `BB26-0412-A7` (edition code + year + sequence + check character) generated server-side from a per-edition counter, never from the primary key. Consents carry their own document IDs (`BB26-consent v1`, `SS-terms v1`, `CP-terms v1`).
- **Duplicate detection** produces a *confidence level* (the admin queue shows "Medium confidence, matched on phone") and only ever opens a review. Actions are *send ownership check to the verified contact*, *keep separate* or *escalate*. There is no merge endpoint.
- **No enumeration leaks.** The sponsor and spouse forms must answer the same way whether or not the person exists ("we can't say; we'll send a code"). Rate-limit these lookups and return uniform responses and timings.

### 5.13 Real-time and scheduled work

- **Live control room.** The safeguarding screen shows Expected / On site / Checked out / Overdue counts and open incidents. Serve these through **server-sent events** (fallback: 10-second polling), scoped to the edition and the caller's permission. Incident alerts ("Supervisor alerted") fan out through the notification module (in-app push plus SMS) and are acknowledged by a named person.
- **Scheduled jobs:** daily manifest generation (evening before), `LATE_PICKUP` flagging after the pick-up window closes (6:00pm), guardian reminder for the last day, claim-link expiry and nudges, hold expiry (Phase 2), payment reconciliation (Phase 2), retention.

### 5.14 API conventions

- REST over JSON, versioned `/v1`, generated OpenAPI, typed client in `nlwc-ems-shared/contracts`.
- Cursor pagination, filter and sort whitelist, consistent problem+json errors with stable codes, `Idempotency-Key` on every mutating call that creates money, registrations or custody events.
- Server returns **display-ready states** (price state, status axes, blocked reasons, next actions) so the frontends do not reimplement rules.
- Rate limiting per IP and per account; request size limits; strict CORS to the two known origins.

---

## 6. `nlwc-ems-web` (attendee portal)

Source of truth: [documentation/design/portal/index.html](../design/portal/index.html), 15 screens (login and sign-up are separate pages).

### 6.1 Route map

| Route | Screen | API dependencies | Phase |
|---|---|---|---|
| `/` | Home, event catalogue, hero, next-event card | `GET /editions?public=1` | 1 |
| `/events/[slug]` | Event page (modes, days, pick-up rule, FAQ, venue, registration card) | `GET /editions/:slug` | 1 |
| `/login`, `/signup`, `/forgot` | Log in, create account, reset (two separate pages, Google button on both) | `auth/*` | 1 |
| `/dashboard` | Needs-attention tasks, my registrations with four status axes | `GET /me/registrations`, `/me/tasks` | 1 |
| `/profile` | Details, spouse, dependants | `person`, `family` | 1 |
| `/events/[slug]/register` | Bootcamp wizard | `quotes`, `registrations`, `consents` | 1 |
| `/passes` | Per-person QR, daily record, lost phone | `credentials` | 1 |
| `/privacy` | Consent history, data requests | `consents`, `dsar` | 1 |
| `/register-for-someone` | Sponsor path, invite vs direct | `registrations` (proxy) | 1 (basic), 2 |
| `/claim/[token]` | Beneficiary claim | `claims` | 1 |
| `/events/singlz/register` | Singlz wizard + live cart | `quotes`, `bookings`, `orders` | 2 |
| `/events/couples/register` | Couples pair order | `orders` | 3 |
| `/payments`, `/receipts/[id]` | Payer vs beneficiary, snapshots | `orders` | 2 |

Notes from the templates: the header nav is **Events · Profile · Payments** ("My events" was removed), so `/dashboard` is reached from the account menu and from post-login redirect. The public site and the attendee portal are one app. The Singlz, sponsor, couples and claim wizards share one stepper, one order summary and one consent-card component.

### 6.2 Frontend rules

- **No business rules in the browser.** Eligibility, price, "can register" and blocked reasons come from the API. Shared `domain` helpers format only.
- **Wizard:** step list, back, **save draft and resume** (UX-02), per-step server validation, review-and-confirm. Form state persists in the API as a draft registration, not in `localStorage` alone. Consents default to unanswered; health has an exclusive "None" (BB-06).
- **Performance budget (low-end Android, 3G/4G):** public pages under 150 KB JS gzipped, LCP under 2.5 s on throttled "Slow 4G", forms usable before hydration finishes where possible. Fonts: subset, `font-display: swap`. The hero SVG scene is lazy and respects `prefers-reduced-motion`.
- **Accessibility:** WCAG 2.2 AA (DESIGN_SYSTEM.md): 44 px targets, visible focus, status is word + icon + colour, tested with keyboard and a screen reader on the wizard and sign-in flows.
- **Theming:** light/dark from the existing tokens; the check-in device is always dark.
- **Passes work offline:** the QR page is a PWA route cached after first load so a family at the gate with no data can still show it.
- **Content:** event copy, FAQ, pick-up rule and venue come from the edition record (admin-editable), not hard-coded, matching the prototype note that these "come from the 2026 terms supplied by NLWC".
- **Errors:** every screen has loading, empty and error states (flagged as "not yet designed" in FRONTEND_DESIGN.md section 3; design them in M1).

---

## 7. `nlwc-ems-admin` (staff console + check-in PWA)

Source of truth: [documentation/design/admin/index.html](../design/admin/index.html), 12 screens, plus the check-in device in the design system.

### 7.1 Route map

| Area | Routes | Roles | Phase |
|---|---|---|---|
| Overview | `/` dashboard: unique people vs registrations, exception queue | all | 1 |
| Events | `/editions`, `/editions/:id` (lifecycle, capability toggles, clone) | Super, Event | 1 |
| | `/editions/:id/content` (copy, FAQ, days, venue) | Event | 1 |
| | `/editions/:id/pricing` (matrix, draft → approve → publish, preview quote) | Event (draft), Finance (approve) | 1 (model + FREE rules), 2 (full) |
| | `/registrations` (four-axis table, detail drawer, restricted health) | Super, Event | 1 |
| | `/people` (duplicate review, claims; no merge button) | Super, Event | 1 |
| | `/pairing` (spouse links) | Super, Event, Accom | 2 |
| Operations | `/camp-review` (human decision queue) | Super, Event, Safety | 1 |
| | `/accommodation` (capacity, unit grid, paid-but-unallocated) | Super, Event, Accom | 2 |
| | `/safeguarding` (day tabs, expected/on site/out/overdue, exceptions, incidents) | Super, Event, Safety | 1 |
| Money | `/orders`, `/payments` (mismatches, webhook log, manual payment, refunds) | Super, Finance | 2 |
| Comms | `/communications` (templates, delivery log) | Comms | 1 (templates), 2 (campaigns) |
| Reports | `/reports` (occupancy, manifests, camper PDF) | Event, Leadership | 1 (attendance and registration), 2 |
| Governance | `/staff` (role × capability, scope, MFA, expiry) | Super | 1 |
| | `/audit` (append-only, filters) | Super, Finance | 1 |
| Registration desk | `/desk` (assisted on-site registration) | Registration Officer | 1 if Q-level cut allows, else 2 |
| **Check-in PWA** | `/checkin` (mode selector, scan, result panel, manual lookup, collector verification, supervisor escalation) | Check-in Officer, Camp Coordinator, Usher | 1 |

**Design status of each admin area.** *Designed in the prototype:* dashboard, editions, pricing, registrations, people & claims, pairing, camp review, accommodation, safeguarding control room, finance, staff & roles, audit, and the check-in device mock. *Not designed yet* (FRONTEND_DESIGN.md section 3): **Registration desk, Reports and manifests, Communications, Forms & rules editor, Studio/forum rosters**, admin mobile layout, empty/error/loading states, and print templates for receipts and passes. These are on the design backlog for weeks 0 to 1 (section 11.2). Anything not designed by its build week is built from the design-system components and reviewed afterwards.

### 7.2 Admin rules

- **Event scope always visible** in the top bar; navigation is **generated from the caller's permissions** (the API returns the permission set at login). Hiding a link is not security; every endpoint re-checks.
- **Every dangerous action asks for a reason** and writes an audit event (reassignment, manual payment, consent override, custody exception).
- **Sensitive fields show a lock** until the permission is held; reveal writes an access log row.
- **Queues with counts** in the sidebar (claims, camp review, exceptions, paid-unallocated).
- **Tables** use server-side filtering and pagination; large exports are jobs.

### 7.3 Check-in PWA specifics

- Installable, always dark (`data-surface="device"`), 56 px targets, 17 px base text, mode bar fixed at the top. The mock shows five modes (Entry, Check-in, Check-out, Camp, Session); the design-system spec and API add a sixth, **Handover**. Align the UI to six.
- Result panels use colour + icon + word: **✓ Checked in**, **⚠ Verify pick-up**, **✕ Do not release**, plus no-match and needs-review. Optional sound/haptic differs per result. A duplicate scan shows who, when and where the first scan happened. Connectivity pill always visible.
- Shows minimum data only: name, age, day, safety flag, authorised collectors. No DOB, medical detail, payment or address.
- Camera scanning (`BarcodeDetector` where available, `html5-qrcode`/ZXing fallback), plus accessible manual lookup by name or reference.
- Local queue (IndexedDB) for the offline-safe modes only, visible "N waiting to sync" indicator, conflict screen on sync. Custody modes disabled offline with a clear message.
- Screen stays awake; auto-lock on idle; session timeout shorter than the console; device registration so a lost phone can be revoked.
- Large-text mode; works one-handed; tested on low-end Android.

---

## 8. Data model (first migration set)

Phase 1 creates the tables below. Phase 2 and 3 tables are designed and reviewed now but migrated later, so nothing in Phase 1 has to change.

| Group | Phase 1 tables | Later |
|---|---|---|
| Identity | `person`, `account`, `verified_contact`, `account_claim`, `session`, `mfa_factor` | |
| Family | `person_relationship`, `guardian_authorization`, `authorized_collector` (relationship, validity: always or specific days), `emergency_contact` | `spouse_link` (P2) |
| Catalog | `organization`, `event_template`, `event_edition`, `edition_capability`, `event_day`, `venue`, `edition_content`, `policy_version`, `reference_counter` | |
| Registration | `registration`, `registration_actor`, `registration_status_history`, `invitation` | |
| Consent | `consent_definition`, `consent_record` (per person, per document version), `health_disclosure` (conditions list + free text), `media_preference`, `studio_request`, `communication_preference` | |
| Pricing | `rate_plan`, `rate_plan_version`, `rate_rule`, `rate_approval`, `price_quote`, `quote_line` | |
| Credentials | `event_credential` (state: pending, active, restricted, suspended, revoked, expired + reason), `credential_scan` | |
| Attendance | `day_attendance`, `custody_handover`, `camp_stay`, `camp_roll_call`, `incident` + `incident_event` (timeline), `attendance_exception` | |
| Access and audit | `role`, `permission`, `role_grant` (scope, granted_by, expires_at), `audit_event`, `access_log`, `device` (registered check-in devices, revocable) | |
| Notifications | `notification_template`, `notification`, `notification_delivery`, `outbox` | |
| Orders | | `cart`, `order`, `order_item`, `order_allocation`, `payment_attempt`, `payment_transaction`, `refund`, `ledger_entry` (P2) |
| Inventory | | `accommodation_category`, `unit`, `bed_slot`, `booking`, `booking_occupant`, `hold`, `allocation`, `stay` (P2) |
| Couples | | `couple_order_link` (P3) |

Conventions: UUIDv7 primary keys, `created_at`/`updated_at`/`created_by`, soft status not soft delete, event closure is a status (not a copy-and-delete), all migrations forward-only with a tested rollback plan for the first production release.

---

## 9. Cross-cutting engineering

### 9.1 Security

- Secrets only in the platform secret manager; pre-commit and CI secret scanning; no secrets in repo history from day one.
- Dependency audit in CI; weekly updates; lockfiles committed; container images pinned.
- OWASP ASVS L2 as the review checklist. External penetration test or at minimum a structured internal security review **before** public registration (M2), and again before Phase 2 payments go live (BRD D-28 and acceptance criterion 16).
- Encryption at rest (managed DB and object store) and in transit; field-level encryption for `health_disclosure` and collector phone numbers.
- Brute-force controls, CAPTCHA only after repeated failures, signed webhooks, strict CSP, secure headers, no PII in URLs or logs.

### 9.2 Privacy and data governance

- Data minimisation per screen and per role (the sponsor sees only "completed/pending" for a beneficiary).
- Consent records are immutable and versioned; a data-request workflow (export, correct, delete where lawful) is built into `/privacy` and the admin console. Nigeria's data protection law applies; counsel confirms specifics (Q9, Q15).
- Retention jobs run per policy once approved; until then nothing is auto-deleted and nothing is kept beyond purpose without a documented reason.
- No children's names, photos or details in public pages, analytics or logs.

### 9.3 Testing strategy

| Layer | What | Tooling |
|---|---|---|
| Unit | Pure rules: age on reference date, eligibility, rate resolution, state transitions | Vitest |
| Integration | API + real Postgres per test run (containers), migrations applied | Vitest + Testcontainers |
| **Concurrency** | Two buyers for last bed; webhook replayed 5×; simultaneous checkout of the same child | Dedicated parallel tests, run in CI nightly |
| **Authorisation matrix** | Every endpoint × role, generated from the permission matrix | Generated tests |
| Contract | OpenAPI diff in CI, breaking changes blocked | oasdiff |
| End-to-end | Register family, claim, check in, check out, escalate | Playwright (web, admin, mobile viewport) |
| Accessibility | Automated axe checks + manual keyboard/screen-reader pass | axe, manual |
| Performance | Registration burst, scan burst at the gate, throttled Android profile | k6, Lighthouse CI |
| Security | SAST, dependency, secrets scan, DAST on staging | CI + scheduled |
| **Gate rehearsal** | Real phones, real network, lost phone, duplicate scan, collector dispute, clock drift | Scripted drill in M5 |

BRD acceptance tests AT-01 to AT-20 map to layers as follows: AT-01, AT-13 concurrency; AT-02, AT-03 integration; AT-04, AT-05 integration + e2e; AT-08 to AT-11 e2e + rehearsal; AT-17 authorisation matrix; AT-15, AT-16 integration. Each AT has a test ID in the traceability sheet (section 12).

### 9.4 CI/CD

PR checks: typecheck, lint, unit, integration, contract diff, secret scan, a11y smoke, build. `main` deploys to staging automatically. Production is a manual promote with migration dry-run. Feature flags (a small table in the API) gate unfinished modules so Phase 2 code can ship dark. Rollback: previous image + forward-fix migrations; database changes are expand/contract.

### 9.5 Observability and operations

- Dashboards and alerts for what the BRD calls out: payment mismatch, booking over-allocation, high check-in failure rate, children not checked out by 6:00pm, webhook failures, queue depth, API 5xx.
- **Event-day on-call** for 21–23 Dec, with a runbook and a named human at each gate shift.
- Backups: automated daily + point-in-time recovery; **restore drill in M5**; documented RPO/RTO (propose RPO ≤ 15 min, RTO ≤ 2 h for event days).
- Printed fallback manifest per day (expected children and authorised collectors) is generated the evening before. This is the contingency if the network or API is down.

### 9.6 Content and configuration

Edition content (copy, FAQ, days, pick-up times, venue, camp ages, price rules) lives in the database and is edited in admin with preview and a publish step. Nothing event-specific is hard-coded, which is also how a new edition is created without a deployment (BRD acceptance criterion 1).

---

## 10. Design template review and design-to-build mapping

I reviewed every template: the portal (15 screens), the admin console (12 screens), the design-system page and `DESIGN_SYSTEM.md`, `tokens.css`/`tokens.json`, `components.css`, and the confirmation email. This section records what they commit engineering to, where they disagree with each other or with the BRD, and what is still undesigned.

### 10.1 What the templates commit us to

| Template | Engineering consequence |
|---|---|
| Portal wizard (Bootcamp) | Five steps: Participants → Health & care → Pick-up → Permissions → Review. Per-child participation mode, studio interest, health (Yes/No, multi-select conditions, free text, "None" exclusive), authorised collectors with validity (always, or specific days), an emergency contact, three permission items with a progress count, a per-child review with a "1 task left" state. Server-side drafts, a per-step validation endpoint, and a review summary the API computes. |
| Portal wizard (Singlz, sponsor, couples, claim) | Same stepper and order summary; live cart with hold countdown and signed quote ID ("Quote Q-7F3A · rate plan v2 · valid 14:32"); blocked checkout with a reason; sponsor flow with non-revealing duplicate warning; claim flow with a code to the verified phone, "This isn't me / Wrong details" dispute path, and pass activation on completion. |
| Portal dashboard and passes | Task list ("Needs your attention") computed by the API; four status axes per person; passes with a daily record (Mon 21 / Tue 22 / Wed 23), *Add to phone*, *Print*, *Lost my phone* (revoke and reissue), restricted-state reason, and a stream entitlement for online participants (D-22). |
| Portal privacy | Communication preferences (event messages always on; news optional; WhatsApp alerts), consent history, *Download my data*, *Request correction* → data-request workflow. |
| Portal profile | Name parts, DOB (changes reviewed and logged), gender, marital status, country/city, **phone verified by code**, optional email, spouse, dependants 0–12 with relationship declaration. |
| Admin console | Role-filtered nav with queue counts, edition scope switcher, global search, detail drawers with "request access (logged)", append-only audit, publish-readiness checks on pricing, quote preview, version history with reasons. |
| Admin safeguarding | Live counts, exceptions (late pickup, pick-up mismatch, duplicate scan), incident log with a timeline and a **required resolution note**, outcomes *Release to verified adult* / *Keep in care*, no force-success. |
| Design system | Role tokens only; four surfaces (public, portal, admin, device); event accent via `data-event`; typed-enum status badges; price-line states; consent card; day-attendance strip; capacity meter with sold/held/available; sensitive-data lock; `₦5,000` and `Mon 21 Dec 2026` formats; tabular numerals; WCAG 2.2 AA; 30% text expansion; no text in images. |
| Email | Handlebars-style merge fields (documented in the file header), logo must be a hosted absolute URL, always light, table layout with inline styles; FAQ now included. |

### 10.2 Conflicts and gaps I found

| # | Finding | Resolution |
|---|---|---|
| 1 | **Sign-up requires an email; the portal is phone-first** (profile shows verified phone, claim creates a profile "with this phone number", sponsor form takes phone or email). | Q5. Account = at least one verified contact. |
| 2 | **Sign-up collects name and gender but not DOB.** Age band, price and eligibility all need DOB. | DOB is collected in a mandatory "complete your profile" step before the first registration, not at sign-up. The API refuses registration until DOB exists. |
| 3 | Gender is Male/Female in the profile and sign-up. | Keep as designed; store as an enumerated field with room to extend. Confirm with NLWC if that matches the legacy forms. |
| 4 | **"Couples Retreat" vs "Couples Programme".** | Q19. |
| 5 | **Media consent unanswered → pass "Pending"** in the wizard, while the BRD says media never blocks registration. | Q20. Default: *No* never blocks; *unanswered* keeps the pass Pending, as designed. |
| 6 | **Check-in modes:** mock shows 5, spec shows 6 (adds Handover). | Build 6. |
| 7 | **`FRONTEND_DESIGN.md` is stale:** it still describes OTP sign-in, 14 portal screens and the old event page; the prototype now has separate login/signup, a calmer event page with a collapsible FAQ, new hero copy and a smaller featured-event card. | Update the doc in week 0 (it is the contract frontend engineers will read). |
| 8 | **Admin pricing demo data** shows Singlz amounts and "Rate plan 1 awaiting approval"; the dashboard shows **"Unique people 1,284 / 1,771 registrations"** and 412 Bootcamp registrations. All invented. | Treat as layout only; no seeds are copied into the app. |
| 9 | **Prototype loads Google Fonts from a CDN.** | Self-host Plus Jakarta Sans (400/500/600/700), Marcellus (event heroes only) and JetBrains Mono in the build: faster on low-end Android, works offline in the PWA, no third-party request for children's pages. |
| 10 | **Logos are PNG crops**, no SVG, no mark-only icon (DS-4). | Q22; blocks favicon, PWA icons and print templates. |
| 11 | Hero scene is a large animated inline SVG; the card uses `backdrop`-free styling but still ships ~14 KB of SVG paths and several CSS animations. | Lazy-load below the first paint; honour `prefers-reduced-motion` (already in CSS); budget in 6.2. |
| 12 | FAQ #10 "How can we give" has no account details. | Q17. |
| 13 | Event page copy ("camp 10–16, free, reviewed by our team") is hard-coded in the prototype. | Comes from edition data (5.11). |

### 10.3 Design debt to close (owner: design, by end of week 1 unless noted)

Registration desk (assisted on-site) · Reports and manifests (daily child manifest, camper list, occupancy) · Communications (templates and delivery log) · Forms & rules editor · Studio/forum rosters · empty, loading and error states for every screen · admin mobile layout · print templates for receipts, passes and the printed fallback manifest (week 3) · "Add dependant" and "Add collector" modals as standalone states · phone-OTP screens and the "I don't have an email" path (Q5) · account menu / post-login header state (since "My events" left the nav).

### 10.4 Artefact to build mapping

| Prototype artefact | Becomes |
|---|---|
| `tokens.css`, `tokens.json`, `components.css`, `theme.js` | `@nlwc/ui`: tokens as CSS custom properties (framework-neutral, as DESIGN_SYSTEM.md section 12 asks), generated typed token module, React components, theme switcher with `data-theme`, `data-surface`, `data-event` |
| Typed status vocabulary (DESIGN_SYSTEM.md 7.2) | TypeScript enums generated from the API's state-machine definitions; badge components accept the enum, not a string and a colour |
| Portal screens (15) | `nlwc-ems-web` routes (section 6) |
| Admin screens (12) | `nlwc-ems-admin` routes (section 7) |
| Check-in device mock | `/checkin` PWA |
| `email/registration-confirmation.html` | First notification template; the merge fields in its header become the template's typed variables |
| Hero SVG scene | Lazy decorative component, reduced-motion aware |
| Domain patterns (price line, consent card, day strip, capacity meter, sensitive lock, scan result panel) | Shared components with visual-regression snapshots, as DESIGN_SYSTEM.md section 12 requires |

---

## 11. Delivery plan

### 11.1 Workstreams and dependencies

```
 A Platform      repos · CI · envs · auth · RBAC · audit · observability ──────────┐
 B Catalog+People edition model · person · family · claims · consent ──► C Registration
 C Registration  wizard API · sponsor path · drafts · credentials ───────► E Admin console
 D Attendance    day check-in/out · custody · incidents · PWA ────────────► F Rehearsal
 E Admin console registrations · camp review · staff · audit · comms · reports
 F Hardening     security review · load · a11y · SOPs · drills · go/no-go
 G Pricing model (explicit FREE rules, quote API) ── runs alongside B/C; charging itself is Phase 2
```

### 11.2 Week-by-week (assumes 4 engineers, Q1)

| Week | Dates | Focus | Outputs |
|---|---|---|---|
| 0 | 31 Oct – 6 Nov | **M0**: decisions, scaffolding, ADRs, schema v0, contract v0, `@nlwc/ui` from prototype tokens; update FRONTEND_DESIGN.md; start design-debt list (10.3) | Repos, CI, staging, ADRs 1–8, first migrations |
| 1 | 9–13 Nov | Identity and accounts, edition catalog, person and family, RBAC skeleton | Sign up, log in, MFA for staff, edition CRUD |
| 2 | 16–20 Nov | Registration + consent + health, wizard UI, dashboard, profile | **M1**: end-to-end on staging |
| 3 | 23–27 Nov | Credentials/passes, notifications (confirmation email), claim flow, admin registrations, security review, content loaded | **M2**: public registration opens |
| 4 | 30 Nov – 4 Dec | Staff roles, camp review, comms templates, reports, audit UI, sponsor path | **M3** |
| 5 | 7–11 Dec | Check-in PWA, custody workflow, incidents, manifests, offline-safe queue | **M4** + code freeze |
| 6 | 14–18 Dec | Rehearsals, load test, restore drill, SOPs, bug fixes only | **M5** |
| 7 | 19–23 Dec | Go/no-go (19 Dec), final manifest printing, event support | Event |

Registration opens at M2 while the check-in side is still being built. This is safe because registration only collects data. The check-in side is on the critical path to M4, not to M2.

### 11.3 Scope cut line (what drops first if capacity is lower than assumed)

Cut in this order, keeping everything above the line:

**Above the line (must ship for 21 Dec):** accounts, profile, dependants, collectors, Bootcamp registration with consent and health, QR passes, confirmation email, admin registrations, staff roles with MFA, per-day check-in and checkout with custody verification, exceptions and incidents, attendance dashboard, printable daily manifest, audit log, backups.

**Cut first:** 1. Registration desk (use admin entry instead). 2. Camp review queue UI (handle in a spreadsheet and set status via admin). 3. Sponsor "register someone else" direct path (keep invite). 4. Communications campaigns. 5. Reports beyond attendance and registration counts. 6. Dark theme polish and the animated hero. 7. Phone OTP (email + password and Google only).

**Never cut:** custody verification, per-day records, MFA, audit, restore drill, security review before public launch.

### 11.4 Release gates for 21 Dec (all must be true)

1. Bible Bootcamp edition shows 21–23 Dec 2026; 17+ walk-ins register and are not blocked (BB-01, BB-02).
2. Every non-camping child has an expected row per day; checkout needs a verified collector; an unknown adult cannot check out a child and the escalation path works on a real phone (BB-03, BB-05, AT-08, AT-09).
3. Medical, collector and consent data visible only to permitted roles; verified by the authorisation test (AT-17).
4. Security review completed, no open critical or high findings; secrets scan clean; MFA enforced for all staff.
5. Backup restore tested within the past 7 days; event-day runbook and printed manifests ready.
6. Gate rehearsal completed on the real devices to be used, including offline and duplicate-scan cases.
7. Legal text (terms, privacy, guardian, media consent) approved and versioned (Q9).
8. Load test passed at 5× expected peak registrations and the expected gate scan rate.

### 11.5 After 21 Dec

| Phase | Scope | Gate |
|---|---|---|
| **2: Singlz** (Jan to Feb 2027, aligned to the Singlz date once known) | Orders, Paystack, inventory and holds, married-only chalet, spouse link and paid pairing, dependant pricing, pay later, reconciliation, accommodation admin, finance console | D-05 to D-10, D-24 answered; price plans approved; payment review (BRD acceptance 16) |
| **3: Couples** | Pair order, individual passes, no-accommodation template, entry-only scans | D-24 to D-26 answered; rate published |
| **4: Operations** | Offline scanning at scale, waitlists, WhatsApp, edition comparison, group sponsors | After Phase 2 |

Hypercare: daily triage 24–31 Dec; a retrospective and a "what we would change" review of safeguarding SOPs in the first week of January.

### 11.6 Team roles (Q1)

| Role | Owns |
|---|---|
| Tech lead / backend | Architecture, identity, registration, access, ADRs, release gates |
| Backend 2 | Catalog, pricing model, attendance and custody, outbox and jobs |
| Frontend 1 | `nlwc-ems-web`, `@nlwc/ui` |
| Frontend / PWA 2 | `nlwc-ems-admin`, `/checkin` PWA |
| QA / DevOps (shared, part-time acceptable) | CI, environments, test automation, rehearsal, observability |
| Designer (part-time) | Close design debt (10.3), SVG logo and icons, review each milestone against the prototype |
| Product / safeguarding liaison (NLWC) | Decisions in section 2, SOP, content, legal |

---

## 12. Risks

| # | Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|---|
| R1 | Team smaller than 4 | Medium | High | Apply the cut line early (11.3); decide at M0, not M3 |
| R2 | Bootcamp has fees after all (Q3) | Low–Med | High | Payments move into scope: add 2 backend weeks, drop registration desk and reports, consider opening registration later |
| R3 | Safeguarding SOP not signed off in time | Medium | High | Draft in week 1; Q8 owner named at M0; in-app flow cannot be bypassed |
| R4 | EGFM reuse becomes mandatory later | Low | Medium | Module boundaries and contracts make a .NET API swappable; no code coupling to EGFM |
| R5 | Poor connectivity at the venue | High | High | Offline-safe arrival modes, printed manifests, live-only custody with manual SOP, tested in M5 |
| R6 | Legal wording late | Medium | Medium | Wording is data loaded from admin; public launch blocked until approved |
| R7 | Scope creep from Singlz/Couples requests | High | Medium | Feature flags; roadmap in 11.5; "designed, not built" rule |
| R8 | Security findings late | Medium | High | Review at M2, not M5; automated scans from week 0 |
| R9 | Single points of failure on event day | Medium | High | Backups + restore drill, on-call, runbook, printed fallback, status page |
| R10 | Design drift between prototype and build | Medium | Low | `@nlwc/ui` built from `tokens.css`; design review at each milestone |

---

## 13. ADRs to write in week 0

1. Stack (TypeScript, NestJS, Next.js, Vite, Postgres) and the EGFM stance (Q2)
2. Monorepo vs three repos
3. Auth model: sessions, MFA, Google, phone OTP (Q5)
4. Where the check-in PWA lives (Q6)
5. Pricing model: versioned rate plans, explicit FREE, signed quotes
6. Offline policy for check-in (what may queue, what may not)
7. Audit and access-log design (append-only, hash chain)
8. Event content as data (no hard-coded event rules)
9. Hosting, backup, RPO/RTO (Q7)
10. Data retention and deletion (Q15)

---

## 14. What I'd do next, once you answer

1. Q1 to Q6 → I write ADRs 1–4 and 6 immediately and scaffold the monorepo (api, web, admin, shared) with CI.
2. Update FRONTEND_DESIGN.md (stale, see 10.2 #7) and the sign-in/sign-up prototype to match the answer to Q5, including the DOB step and phone-only path.
3. Draft the schema v0 migrations and the OpenAPI contract for identity, catalog, family and registration.
4. Turn the prototype's tokens and components into `@nlwc/ui`.
5. Produce the traceability sheet (BRD requirement → API endpoint → screen → test ID).
