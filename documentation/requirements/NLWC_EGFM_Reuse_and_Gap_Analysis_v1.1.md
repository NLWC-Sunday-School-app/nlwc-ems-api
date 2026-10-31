# EGFM Registrar → NLWC Unified Event Management System

## Reuse, gap analysis, modernization and migration blueprint

**Document ID:** NLWC-EGFM-GAP-001  
**Version:** 1.0  
**Date:** 31 October 2026  
**Status:** Draft for architecture discovery, stakeholder validation and codebase assessment  
**Target:** The New and Living Way Church (NLWC) unified event management platform  
**Companion document:** `NLWC_Unified_Event_Management_System_BRD_v3.0.md`

> **Critical evidence note:** “Existing EGFM” means **documented in the supplied _Registrar System – Business Requirements Document (EGFM)_**, which says it was derived from the working codebase. This review did **not** inspect repositories, database schemas, deployed services or live infrastructure. “Reuse” therefore describes **candidate reuse** pending technical due diligence; it is not a guarantee that EGFM code is portable, licensed for NLWC, secure, deployable or compatible with current dependencies.

## 1. Executive decision

**Recommendation: evolve a shared registrar foundation, but refactor high-risk seams before NLWC production use.** Retain the architecture shape (public portal + staff portal + mobile check-in PWA + backend + database), existing multi-event ideas, identity/family records, Paystack integration concepts, accommodation administration, ticket issuance and reporting patterns. Do **not** wholesale copy payment allocation, underage collection, account matching, event end archival or secrets management as-is.

NLWC owns several event *families*, each with multiple *editions* and independently configured capabilities:

- **Bible Bootcamp 2026:** 21–23 December 2026; daily child attendance and authorized checkout for ages 3–16 (especially non-campers); 17+ may attend daily programme; camp eligibility/fees need confirmation.
- **Singlz Summit:** online, walk-in or camping; singles 13+; chalets restricted to married participants; married applicants give spouse name and phone; spouse can be linked, registered and paid for; dependants 0–12, 0–6 free, 7–12 hostel ₦5,000 **for the current edition only**; future editions set their own prices; other 7–12 accommodation prices pending; no employed/unemployed fee tiers.
- **Couples Programme:** registration and payment, no accommodation/camping, with configurable per-couple vs per-person fee (not yet decided).

Shared person identity, registration, booking, payment, consent, credential and attendance **must be separate domain concepts**. Do not make “paid”, “camping”, “checked in” and “assigned room” different meanings of one mutable registration row.

## 2. Evidence and confidence

| Source | What it actually establishes | Confidence / limit |
|---|---|---|
| EGFM Registrar BRD (uploaded) | Describes an existing .NET 10/MySQL/Dapper/EF Core system; Next.js attendee portal; React admin; React PWA check-in; users, spouses, dependants; Paystack; SendGrid; lodging; ballot; room assignment; child sessions; archival | High for **documented design**; unverified directly in code or production |
| Bible Bootcamp 2025 screenshot inventory / BBMS BRD | Six historical sections, questions, consent, camp and parent forum content | Historical only; actual question-level Google Form branching unverified |
| NLWC conversations and earlier NLWC BRD v2.0 | Current requested capabilities, Bible Bootcamp dates, Singlz rules, sponsored registration and participation modes | Explicit user decisions; unresolved items preserved as TBD |
| Latest user addition | NLWC has a **Couples Programme** requiring registration and payment but **no camping** | Confirmed feature; fees, dates, pairing and entry validation still TBD |

## 3. Documented EGFM baseline architecture

| Layer | EGFM documented implementation | NLWC starting point | Decision |
|---|---|---|---|
| API | ASP.NET Core .NET 10, `api/v2`, service/core/data projects | Assess code hygiene, version support, API contracts | **Reuse/Refactor** |
| Data | MySQL, EF Core migrations, Dapper queries | Extend schema with explicit relational tables, constraints, status history and safe migrations | **Refactor** |
| Public attendee portal | Next.js, React, Ant Design/Jotai | Rebrand; simplify event templates; add proxy/claim workflows | **Reuse/Extend** |
| Administration | React/Vite, permissions, registration/payment/lodge pages | Church-wide, event-specific control; pricing, capacity, attendance, incident tools | **Reuse/Extend** |
| Check-in | React 18/Vite PWA, `html5-qrcode` | Daily attendance, child custody handover, camp check-in/out, scan modes | **Extend significantly** |
| Gateway | Paystack card + signed webhooks; cash/admin regularization | Server-authoritative orders, payment ledger, webhook, reconciliation, refunds | **Refactor critical path** |
| Notifications | SendGrid templates/campaigns | Separate beneficiary/sponsor messages, templates, consent, delivery logs | **Reuse/Extend** |
| Accommodation | Lodge, hostel groups, ballot, holds, room/bed assignment | Married chalet eligibility, family occupants, child rates, pair booking and room inventory | **Reuse concepts; refactor domain** |
| Family model | FamilyCode, GuardianId, Spouse and dependants | Relationship graph and separately claimable person identities | **Refactor** |
| Archive | Moves event rows into Historical tables and deletes active copies | Prefer immutable history in place, lifecycle closure, reversible exports, archive policies | **Replace strategy** |

## 4. Capability-by-capability gap matrix

**Disposition:** `R` = candidate direct reuse; `E` = extend; `F` = refactor; `N` = new module. **Priority:** `P0` = platform correctness or launch safety; `P1` = operationally necessary for supported programme; `P2` = later.

| ID | Area | EGFM documented baseline | NLWC required behavior / gap | Disposition | Priority |
|---|---|---|---|---|---|
| GAP-01 | One organization / many events | Multi-event with EventId-scoped registration, lodge and payment | NLWC-owned catalogue; event families and editions with independent feature switches | E | P0 |
| GAP-02 | Event lifecycle | Profiled → Published → CommenceRegistration → Ended | Separate visibility, registration-open, live-event and close/archive states; avoid contradictory published/registration semantics | F | P0 |
| GAP-03 | Event templates | Event configured by fields and `Lodgeable` | Template/policy for Bible Bootcamp, Singlz and Couples; no code fork per event | N/E | P0 |
| GAP-04 | Repeated event editions | Events persistent; live records archived on closure | Clone configuration only; preserve past attendees and financial/audit records | F | P0 |
| GAP-05 | Reusable person profile | Global Attendee; email/phone and church information | Shared Person plus separate Account, per-event Registration and per-event sensitive disclosures | F | P0 |
| GAP-06 | Date of birth | DOB, age categories, 12-hour age job | Calculate age at edition reference date and preserve pricing/eligibility snapshot | E/F | P0 |
| GAP-07 | Spouses | FamilyCode / gender-based mother/father; linked spouse | Verified two-person relationship; name+phone minimum for married Singlz applicant; claim/approve link | F | P1 |
| GAP-08 | Dependants | FamilyCode, GuardianId, child records | Dependants 0–12 by DOB; distinct child identity; 13+ own profile & registration; guardianship distinct from payment | F | P0 |
| GAP-09 | Register someone else | Principal manages family, admin bulk profiling | Arbitrary third-party direct registration, invite alternative, sponsor Pay Now/Pay Later | N/E | P0 |
| GAP-10 | Beneficiary claiming | EGFM documented signup and member service import | Secure claim flow, proof of contact ownership, avoid wrong auto-match/duplicates, self-correction | N | P0 |
| GAP-11 | Contactless proxy notifications | General registration/ticket email | Beneficiary notified upon registration and after verified payment; payer separately notified | E | P1 |
| GAP-12 | Authentication | Password/Google; separate member API | Independent NLWC signup (no EGFM dependency), safe adult/minor account and recovery model | F | P0 |
| GAP-13 | Participation modes | WalkInParticipant, OnlineParticipant, CampingParticipant | Same 3 modes per event; Couples disables camping; BBC can add hybrid physical/stream rule | R/E | P0 |
| GAP-14 | Registration state | Registration row combines payment, allocation, checked-in | Separate registration, entitlement, order, accommodation booking, occupancy and daily attendance state | F | P0 |
| GAP-15 | Price classification | Worker/Regular/Teenager/Children plus WorkStatus | Singlz flat per approved accommodation/person category within each edition, no employee distinction; **all prices are versioned, admin-configurable per edition; no automatic prior-year carry-forward** | F | P0 |
| GAP-16 | Payment amount authority | Portal calculates per-person amounts; backend checks totals of payment intent | Server computes and signs quote from versioned rates; cannot accept browser-set price as authoritative | F | P0 |
| GAP-17 | Payment orchestration | Paystack webhooks, PaymentRecord, regularization | Orders with items (person/couple/dependant/booking), payer vs beneficiary, lifecycle, receipt and ledger | F | P0 |
| GAP-18 | Safe transaction boundaries | Some business actions commit statement-by-statement | Atomic inventory holds, fulfillment, webhook application, ledger posting | F | P0 |
| GAP-19 | Paid but unallocated | EGFM may mark paid and clear lodge on allocation contention | Never imply paid booking has assigned bed; hold stock during payment and clear paid-unallocated exception queue with remedy | F | P0 |
| GAP-20 | Webhook integrity | Paystack signature and duplicate checks | Keep verification; strengthen immutable event ID, exactly-once fulfillment semantics, idempotency and reconciliation | E/F | P0 |
| GAP-21 | Cash/admin payment | Recorded/admin-attributed | Add permission separation, evidence, approval, full audit and reconciliation | E | P1 |
| GAP-22 | Refunds | Gateway refund outside system; admin reverts allocation | Explicit refund/chargeback cases and accounting state; do not erase paid history | F | P1 |
| GAP-23 | Accommodation types | Hostel, Chalet, ChaletPairing, SpecialDormitory, Hotel | Chalet, special dorm, hostel; future types, rate plans, role eligibility and temporal occupancy | E/F | P0 |
| GAP-24 | Chalet entitlement | EGFM pairing code allows second household; pairing price ₦0 | **Different NLWC rule:** only married access chalet; spouse details and paid pairing may be required; do NOT reuse zero-cost pairing semantics | F | P0 |
| GAP-25 | Marriage status | Profile marital status exists | Application validation, spouse link/claim, audit changes affecting booking | E/F | P1 |
| GAP-26 | Child accommodation pricing | Broad EGFM child category and event fees | Current-edition configured rules: 0–6 ₦0; 7–12 hostel ₦5,000; 7–12 other categories unresolved; 13+ edition-specific standard rate. **No rate is permanently hard-coded** | F | P0 |
| GAP-27 | Inventory | Lodge capacity and Available/Occupied/Reserved counters | Atomic holds per unit/bed/date; count each physical occupant even when accommodation is free | F | P0 |
| GAP-28 | Ballot queue | 5s worker, 24h reservation, 18d admin hold | Ballot/waitlist optional feature toggle; hold policy configurable per event and charge type | E/F | P1 |
| GAP-29 | Room assignment | At initial camp gate check-in, hostel group first available | Separate booking, room/bed allocation, room changes, camp arrival and departure | E/F | P1 |
| GAP-30 | QR tickets | PDF with registration GUID QR for paid attendees | Per-person revocable credentials for free/paid; admission vs day presence vs room arrival vs child pickup | E/F | P0 |
| GAP-31 | Day attendance | One `CheckedIn` flag per registration | Per-person **per-day** check-in and checkout records; BBC Dec 21, 22, 23 | N/F | P0 |
| GAP-32 | Child session handover | Session drop-off/pick-up with one code and registered guardians | Safe daily non-camper custody workflow, verified delegates, incident escalation, no code-only pickup | F | P0 |
| GAP-33 | Camper accountability | Camping gate check-in and room assignment | One stay admission/final departure plus daily roll-call and temporary movements | E | P0 |
| GAP-34 | Adult daily BBC | Event ages inferred from EGFM categories | Allow 17+ daily participation without child-only pickup flow | E | P0 |
| GAP-35 | Medical data | HealthCondition in attendee profile and screening answers in registration | Restricted event-scoped health/allergy/care notes, human assessment, no medical info in broad dashboards | F | P0 |
| GAP-36 | Consent | Not a detailed per-child event-versioned consent system in EGFM BRD | Separate guardian, event, medical/emergency, media, accommodation, terms acknowledgements | N | P0 |
| GAP-37 | Bible Bootcamp special processes | Not covered as specific modules | Studio requests/approvals, Parent Interactive Forum, optional museum/party | N | P1 |
| GAP-38 | Couples Programme | No dedicated couple booking model | Pair registration/payment, individual spouse identities, no accommodation, one/dual passes as defined | N/E | P1 |
| GAP-39 | Couples pricing | No confirmed model | Support per couple or per person; configurable, no guessed fee | N | P1 |
| GAP-40 | Notifications | SendGrid transactional/campaigns | Sponsor vs beneficiary, spouse linking, claim, consent reminders, booking and pickup alerts | E | P1 |
| GAP-41 | Admin RBAC | Privilege-driven endpoints | Scope privileges to NLWC event; split finance, camp, child safety, medical data and sponsorship | E/F | P0 |
| GAP-42 | Reports | Engagement, lodge, family, fees, payment, camper PDFs | Cross-event dashboard; daily custody ledger, camp manifests, paired bookings, revenue reconciliation | E | P1 |
| GAP-43 | Data retention | Moves to Historical tables, webhooks global | In-place archive with immutable ledger and time-bounded purpose-specific retention; lawful deletion paths | F | P0 |
| GAP-44 | Audit trail | Actor tracking for cash/check-in/admin actions | Versioned decisions, custody, claims, consents, refunds, reassignments, sensitive access | E/F | P0 |
| GAP-45 | Credentials and secrets | EGFM BRD warns credentials committed to appsettings | Rotate exposed credentials if any, remove from git, use secret manager, scan history, block deployment until verified | F | P0 |
| GAP-46 | JWT validation | EGFM BRD describes missing issuer/audience validation | Verify JWT issuer, audience, signing key, expiry, revocation; tighten auth/cookies/CSRF | F | P0 |
| GAP-47 | Internal EGFM member service | Profile imported from EGFM service | Decouple for NLWC; configurable import and explicit ownership of identities | F | P0 |
| GAP-48 | Background jobs | Fast in-process worker every 5 seconds | Durable queue, distributed lock/atomic claim, reliable retry, visibility across multiple replicas | F | P1 |
| GAP-49 | Dates / time | EGFM documented server-local timestamps | Store UTC; use Africa/Lagos for event rendering and eligibility date boundaries | F | P0 |
| GAP-50 | Offline operations | Check-in PWA and poor-connectivity errors | Safe offline attendance procedures with reconciliation; **no unsafe offline custody release** | E/F | P1 |
| GAP-51 | Audit and reconciliation exports | EGFM report for live/ended events | Immutable operational and finance exports by edition, with restricted staff access | E | P1 |
| GAP-52 | Event payment optionality | Paid check-in flows central to EGFM | Free, paid, sponsored, partially paid if later enabled; no unnecessary payment for BBC free editions | F | P0 |

## 5. Cross-system differences that must not be conflated

### 5.1 Chalet pairing is not portable without changing its semantics

In EGFM, a **paid chalet owner receives a pairing code** and a second household can join without its own accommodation payment; the pairing occupant does not consume primary capacity in the same way. This is **not** NLWC's confirmed Singlz requirement. NLWC restricts chalet access to married participants; the user specifically wants spouse details and an ability to **pay chalet pairing for a spouse**. That could be a per-spouse fee, a per-couple chalet rate, a second occupant supplement, or another church-approved model. **No exact price or payment formula has been supplied.** Enforce marriage eligibility and capture both individual occupants regardless of price. Never silently import EGFM's free paired-occupant rule.

### 5.2 “Children's session check-in” is not full-day safeguarding

EGFM starts ministry sessions and logs drop-off and pickup with a code. BBC needs one **independent attendance record per child per date** for all physical non-campers aged 3–16 on Dec 21/22/23 and validated handover; campers need separate overnight presence plus daily roll-call. A session code alone must not authorize release to an unknown adult. Require approved collector verification and a supervised exception path.

### 5.3 “One person per event” still holds even if payment is group-based

A sponsor can pay for multiple beneficiaries; a married couple can purchase one order; a parent can buy several dependants' accommodation. Yet **each attendee has their own event registration** (and safety/attendance record where needed). `Order` and `OrderItem` separate who **paid** from who **attends**.

### 5.4 13+ independence is a profile requirement, not adult consent

At Singlz, age 13+ needs their own person profile, event registration and full accommodation price. Ages 13–17 remain minors for safeguarding/guardian and lawful processing. Dependants aged 0–12 may be guardian managed but must still exist as distinct people for occupancy and safety.

### 5.5 Paid registration is not the same as a valid admission entitlement

For sponsored/provisional records, payment may arrive before the beneficiary claims their account or before required guardian consent. Send a **registration receipt/notice** on creation and **financial confirmation** on successful payment; a QR may be generated then, but its validation can remain restricted until all conditions are met. Avoid representing a paid but unclaimed participant as automatically admitted.

## 6. Recommended target architecture (conditional, not yet approved)

**Retain the four-client shape if assessment supports it:**

1. **Shared API / modular backend:** Person & Identity, Relationships, Event Catalog & Templates, Registration & Policy, Accommodation & Inventory, Pricing, Orders/Payments/Reconciliation, Credentials, Attendance & Safeguarding, Notification, Administration/Analytics.
2. **Public attendee portal:** profile, dependants, spouse, event listings, sponsor registrations, booking, payments, claims and QR.
3. **Admin portal:** organization/event configuration, pricing, inventory, approval, finance, notifications and reports.
4. **Mobile check-in PWA:** explicit scan modes (`EVENT_ENTRY`, `DAY_CHECKIN`, `DAY_CHECKOUT`, `CAMP_ARRIVAL`, `CAMP_DEPARTURE`, `SESSION`, `MINOR_HANDOVER`). Each mode invokes different authorization rules.

A modular monolith is a reasonable **architecture candidate** for the initial deployment (fewer distributed transactions), but codebase health, team expertise, expected load and hosting constraints should decide. There is **no obligation to preserve .NET** merely because EGFM used it; compare refactoring cost with porting cost before any rewrite decision.

### Minimum proposed data-boundary refactor

| Domain | Primary concepts / invariants |
|---|---|
| Identity | Person, Account, ContactMethod, Verification, PersonClaim; no automatic merging by name/email-only |
| Family | PersonRelationship, GuardianAuthorization, SpouseLink, DependantLink; separately verified or reviewed |
| Event | Organization, EventFamily, EventEdition, EventPolicyVersion, Session, RegistrationWindow |
| Registration | Registration per Person+Edition, ParticipationMode, EligibilityDecision, RegistrationStatus |
| Admission | Entitlement, Credential with signed/opaque token, revocation and scan history |
| Pricing | RatePlan, PriceRule, PriceQuote, QuoteItem; DOB-on-reference-date and rule-version snapshot |
| Money | Order, OrderItem, PaymentAttempt, PaymentTransaction, Refund, Settlement, Adjustment; payer independent of beneficiary |
| Accommodation | Venue, Category, Unit, BedSlot, InventoryDate, Booking, BookingOccupant, BookingHold, Allocation, Stay |
| Attendance | EventEntry, DayAttendance, SessionAttendance, CampRollCall, AuthorizedPickup, Handover, Incident |
| Consent & welfare | VersionedConsent, GuardianConsent, MediaDecision, HealthDisclosure (restricted), ReviewDecision |
| Admin | RoleGrant, EventScope, AuditEvent, NotificationTemplate, MessageDelivery, ExportJob |

**Database constraints:** unique active registration per `(person_id, event_edition_id)`; unique payment/provider reference; atomic capacity hold + confirm; no two active exclusive assignments for same bed/date; no overlapping unapproved custody release; no duplicate day check-in without explicit audited resolution; price in integer kobo.

## 7. Event-specific configuration matrix

| Rule | Bible Bootcamp 2026 | Singlz Summit | Couples Programme |
|---|---|---|---|
| Dates | **21–23 Dec 2026** | TBD | TBD |
| Main audience | Children 3–16; older than 16 also daily attendees | Singles 13+, married persons and dependants | Couples; precise eligible relationship requirements TBD |
| Attendance modes | Online / walk-in / camp as approved | Online / walk-in / camping | Physical or online as NLWC decides; **camping disabled** |
| Registration fees | 2026 free/paid policy TBD (2025 was free) | Flat pricing by approved accommodation type/age | **Paid registration**; per-couple or per-person rule TBD |
| Accommodation | 2026 child-camp details/eligibility TBD | Hostels, special dorms, married-only chalets | **None** |
| Child custody | Daily 3–16 physical child check-in/out; camper roll-call | Child welfare/handover based on event rules | Any accompanying children subject to event policy; not yet specified |
| Spouse requirements | Standard guardian/family links as applicable | Married entrant supplies spouse name/phone; link/create spouse | Proposal: couple pair link; whether both names mandatory TBD |
| Dependants | Guardian registers children | 0–6 free and 7–12 hostel ₦5,000 **for current edition only**; future-edition amounts are configurable; 7–12 other rate TBD | Whether dependants can attend TBD |
| Credential | Individual child/adult event QR as eligible | Per-person QR and accommodation entitlement | Per-couple order; one or two person passes TBD |
| Check-in | Per-day child collection and ordinary adult admissions | Event admission, overnight check-in and session attendance | Simple paid event admission; optional session check-in |

## 8. Proposed migration and implementation strategy

### Phase G0 — Codebase audit and source-of-truth inventory (required before estimates)

- Inventory EGFM repositories, branches, assemblies, migrations, data dependencies, package licenses, API routes and deployments; establish what the team legally/contractually may reuse.
- Build a mapping from BRD features to actual source files and tests. Record `VERIFIED_IN_CODE`, `DOCUMENTED_ONLY`, `MISSING`, `BROKEN` for every reused component.
- Run targeted security audit and secret scan. **If actual live credentials were committed, rotate/revoke them and purge inappropriate exposure before reuse.** Do not put secrets in prompts, logs or documents.
- Verify current .NET/tooling support, database migrations and ability to run local integration tests.
- Map EGFM-member-service imports and any EGFM-specific URLs, IDs, ministers/work-status categories, email templates and hard-coded data; remove coupling.

### Phase G1 — Unified identity, event and consent foundation

- Introduce NLWC organization scope (without unnecessary SaaS tenancy), event family/edition, profile/relationship/claim tables, verified contacts, consent versions, per-event policies and event-scoped roles.
- Migrate existing data **only if NLWC has a legitimate source and authorization**; do not copy EGFM private participant records into NLWC by default.
- Add event-reference-date age calculation and immutable eligibility/pricing snapshots.
- Reconcile migrations with existing schema using safe backfills and reversible deployment steps.

### Phase G2 — Payment and inventory integrity gate

- Separate order/price quote from registration; server computes all monetary lines; gateway callback confirms money, not the browser.
- Support combined family/sponsor orders, pair bookings and payable/zero-cost dependants.
- Add atomic reservations, expiration and refunds without deleting financial truth; durable worker reconciliation and idempotency tests.
- Implement a configurable fee engine and disable unapproved Singlz price combinations. Add paid-but-unallocated remediation workflow.

### Phase G3 — Bible Bootcamp 2026 safety and operations gate

- Implement 21, 22, 23 December child day records, authorized collection, camp stay/day roll-call, incident escalation, permissions, guardian notifications, medical restriction and room/approval where approved.
- Validate acceptance with supervised real-device rehearsal, lost-phone/duplicate-scan tests, clock/connection disruption, collection disputes and manual contingency.
- **This is a release blocker for physical children**, regardless of whether public registration launched earlier.

### Phase G4 — Singlz Summit and Couples event enablement

- Enable Singlz rate plans, married-only chalet eligibility, spouse link/claim and paid pairing as approved, dependants and category-specific pricing, accommodation management and reconciliations.
- Enable Couples no-camp template with pay-to-register group order, couple relationship handling and simple admission.
- Use admin-configured dates, venue, opening windows and policies rather than copying historical event defaults.

### Phase G5 — Operations, reporting and progressive hardening

- Cross-event reports, event audits, finance settlement reports, enhanced messaging, access reviews, backup/restore drills, attendance fallback, performance and resilience benchmarks.
- Sunset legacy archival and outdated service dependencies only after data parity and finance reconciliation.

## 9. Target-state acceptance tests (minimum)

| Test ID | Scenario | Expected result |
|---|---|---|
| AT-01 | Two concurrent purchasers compete for last hostel bed | No oversell; loser has clear unpaid/unallocated or paid-exception outcome with remedy; never misleading confirmed occupancy |
| AT-02 | Client modifies price of current-edition 7–12 hostel from configured ₦5,000 to ₦100 | Server rejects tampering; charges the approved edition-specific rate; future editions may legitimately use a different approved rate |
| AT-03 | 9-year-old chooses special dorm at Singlz | No price guessed; booking blocked or held pending approved pricing |
| AT-04 | Single 22-year-old requests chalet | Not offered/declined under Singlz married-only rule |
| AT-05 | Married attendee books chalet and pays for spouse | Spouse has individual registration; correct approved pairing order lines; verified spouse-link/claim; no EGFM implicit ₦0 pairing |
| AT-06 | Sponsor registers and pays for an adult friend | Friend gets registration notice, then verified payment notification and appropriate QR; sponsor cannot access friend's private profile |
| AT-07 | Sponsor chooses Pay Later | Pending order and any short-lived hold; no permanent occupancy; clear expiry/resume |
| AT-08 | Child age 8 attends Bible Bootcamp all three days | Three independent day check-ins and authorized checkouts, each with audit trail |
| AT-09 | QR is held by unauthorized child collector | Normal checkout refused; supervisor safety workflow invoked |
| AT-10 | Child camp arrival followed by day roll-call | One overnight stay record; distinct daily presence; no duplicate camp check-in |
| AT-11 | 18-year-old attends BBC daytime | Allowed ordinary event attendance; no automatic minor custody workflow |
| AT-12 | Couples event purchased as configured couple group | Financial order correct; each required couple profile/credential present; accommodation APIs not invoked |
| AT-13 | Paystack success webhook delivered 5 times | One charge fulfillment, one inventory allocation, no duplicate receipt/ticket access |
| AT-14 | Child media release declined | Publicity/recording requiring permission prevented; ordinary eligible registration remains possible |
| AT-15 | Event closed and reports requested | Past registrations, payments and attendance remain queryable with stable totals and protected access |
| AT-16 | New event edition created | Reuses config if selected, not prior people’s transactions, consents or medical answers |
| AT-17 | Staff member assigned Singlz role scans BBC data | Cross-event access denied unless explicitly authorized |
| AT-18 | Spouse relationship disputed | Changes audited, bookings safely reviewed, no unauthorized account takeover |
| AT-19 | Account of a 14-year-old claimed | Independent person/registration retained; required guardian controls not bypassed |
| AT-20 | Payment reversed after ticket generation | Financial history retained; entitlement/QR adjusted by policy; beneficiary notified |

## 10. Open decisions / questions for NLWC (not assumed)

**Blocking event/business policies:** Bible Bootcamp 2026 fee and camper eligibility; specific day pickup/exit times; camping venue/rooms and fitness approval; Singlz exact accommodation rates, especially chalet pairing and 7–12 non-hostel; 0–6 permissible room occupancy; chalet spouse-occupancy rules; Couples event date, fee basis, required spouse details and whether both partners attend; refund/transfer/cancellation rules.

**Security/implementation:** legitimate rights to reuse EGFM software; existing security exposure/rotated keys; NLWC authentication independence from EGFM member service; guardian verification and custody SOP; offline fallback; provider choices (Paystack, SendGrid, WhatsApp); exact user numbers and device/network provisioning.

**Proposed default until decisions:** keep unpriced accommodation selection disabled, no autonomous medical approval, no public posting of children, no child checkout without authorized collection, no automatic profile linking merely by name/contact, no unbounded accommodation reservation, no implicit ₦0 spouse pairing.

## 11. What to ask the development agent for next

1. Code inventory and feature-verification matrix, naming actual modules/endpoints/migrations for each GAP ID; no unsupported claim of code reuse.
2. Architectural decision record: **refactor EGFM vs selective module extraction vs new implementation**, with realistic costs and verification evidence.
3. NLWC PRD/UX, exact domain ERD and migration plan, API contracts, concurrency and payment state diagrams, access/consent matrix, test strategy, rollout and rollback plan.
4. Demo walkthroughs: BBC daily custody, Singlz paid spouse pairing and dependant pricing, proxy registration/claim/pay later, Couples no-camp pair checkout.
5. A signed-off decision register for all unknown fees, guardian safeguards and event logistics before public launch.

**End of gap analysis.**


## Addendum — Edition-specific pricing, 31 October 2026

**Confirmed revised NLWC business instruction:** All registration, accommodation, pairing, dependent, Couples Programme, discount/add-on and other applicable prices must be configurable independently **for each event edition** without code changes. The Singlz Summit 7–12 hostel ₦5,000 fee and 0–6 free rate supplied in 2026 represent the **current edition only**, not permanent system constants. A different amount may be approved for any future edition. A zero/free price must be explicitly configured and is different from an unpriced option. All active prices require event-scoped versioning, authorized publication, server-side quotes and financial snapshots; copying rates from a previous event creates an unapproved draft only. See the superseding **NLWC BRD v3.1 Section 13.2 and PRICE-01–PRICE-04**.
