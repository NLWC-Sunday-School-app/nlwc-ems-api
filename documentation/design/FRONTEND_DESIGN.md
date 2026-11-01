# NLWC EMS Frontend Designs: Attendee Portal and Admin Console

**Prototypes (open in a browser, no build needed):**
- [portal/index.html](portal/index.html): public / attendee web app, 15 screens (login and sign-up are separate pages)
- [admin/index.html](admin/index.html): admin console, 12 screens, switchable by role
- Shared: [tokens.css](tokens.css), [components.css](components.css), and the rules in [DESIGN_SYSTEM.md](DESIGN_SYSTEM.md)

The check-in PWA is the third client, in its own repo `nlwc-ems-checkin` (ADR-0004). Its screens are in the design system page (`index.html`, "Check-in device"). Six modes: Entry, Check-in, Check-out, Camp, Session, Handover.

> **Updated 31 Oct 2026** to match the prototype and the engineering decisions: email + password and Google sign-in (no OTP, ADR-0003), Bootcamp 2026 is free (no payment step), camp is ages 10–16 only. Phase 2 screens (Singlz, payments, accommodation, pairing) are designed but not in the 21 Dec build (ADR-0007).

## 0. About the "90% like the Registrar" brief

The only Registrar material I could see is what the gap analysis says about it: four clients (`registrarv2` Next.js attendee portal, `registrarv2-admin` Vite/React console, `registrarv2-checkin` PWA, `registrar.io` API), and capabilities such as event editions, participation types, lodge selection, ballot/holds, Paystack, QR tickets and reports. **I haven't seen the Registrar's actual screens or its BRD.** So I mirrored its **structure and flows** (portal, admin, check-in; profile → family → register → pay → ticket; lodge/room admin; payments; reports) and did not copy any visual style.

To make the two feel like one family with the Registrar, please send screenshots of its attendee and admin apps. I'd then align navigation labels and screen order, which is cheap to do now.

The ~10% that is deliberately different comes straight from the gap analysis:

| Registrar behaviour | NLWC design |
|---|---|
| One registration row mixes paid, room and checked-in | Four separate status axes on every person |
| Portal calculates the amount | Price shown is a server quote with version, expiry and "blocked" states |
| Chalet pairing code, second household free | Married-only chalet, spouse is a named occupant, price pending |
| Worker/regular/teen price tiers | None. Age-band and category rates per edition |
| One `CheckedIn` flag | Per-day check-in/out plus a separate camp stay |
| Session code releases child | Collector verified against named list; no "force" |
| Member-service import | Own profile with phone-only path and claim flow |
| Event close deletes live rows | Admin shows history in place |

## 1. Attendee portal

**Principles:** mobile-first (low-end Android, bottom tab bar under 720px), email + password or Google sign-in (no codes), date of birth collected in a "complete your profile" step before the first registration, one profile for every event, tasks before browsing, honesty about prices that aren't confirmed.

**Navigation:** Events · Profile · Payments. The dashboard is reached from the account menu and after login ("My events" was removed). Sub-pages: Passes, Privacy.

| # | Screen | Purpose | Key BRD refs |
|---|---|---|---|
| 1 | Home / events | Catalogue from live editions; status chips (open, TBA) | ORG-01, EVT-01 |
| 2 | Event page | Dates, modes, days, FAQ, price block (amount / Free / pending) | BB-01, PRICE-03 |
| 3 | Log in, Sign up, Forgot password | Two separate pages, Google button on both, email required, layered terms, separate marketing opt-in. Sign-up does not reveal whether an email already has an account | PRO-02, SEC-01 |
| 4 | Dashboard | Needs-your-attention tasks, my registrations with 4 status axes | REG-01, UX-02 |
| 5 | Profile & family | Individuals only: own details (view/edit, incl. email; phone is optional contact only), spouse and link status, dependants (minors 0–17) with guardians. No event, price or booking information on this page | PRO-01–05, §6 |
| 6 | Bootcamp wizard | Multi-child; 17+ not blocked; camp = request; restricted health with exclusive "None"; collectors; per-child media consent; forum | BB-01–06 |
| 7 | Singlz wizard + live cart | Mode, marital status → spouse, accommodation by eligibility, per-person price lines, hold timer, pay now/later | SS-01–08, INV-02 |
| 8 | Register someone else | Recommended direct path vs invite; minimal data; non-revealing duplicate warning; result with statuses | REG-02, REG-03 |
| 9 | Couples | Pair on one order, individual passes, no accommodation step, checkout blocked until fee published | CP-01–05 |
| 10 | Claim | Beneficiary verifies by emailed link, checks details, own consents, pass activates | COM-02 |
| 11–12 | Payments, receipt | Payer vs beneficiary, snapshot amounts, sponsor sees only completed/pending | PAY-02 |
| 13 | Passes | Per-person QR, restricted state with reason, daily record, lost phone | REG-06 |
| 14 | Privacy | Optional comms, consent history, data requests | SEC-01 |

**Behaviours the prototype demonstrates**
- Price states: `₦5,000`, `₦0 · Free`, `⏱ Price pending approval`. In the Singlz cart the pending chalet price **disables Pay** and says why.
- Singles never see a chalet card; 7–12 in special dorm is listed but blocked with "rate pending" (Scenario H).
- Bootcamp 2026 is free: the event page shows "Free" from an explicit published `FREE` rule, with no payment step (the Paid registration capability is off). Camp is a request for ages 10–16, reviewed by the team.
- Sponsor flow ends on "Pending claim + Paid + Confirmed unit TBD + Pass restricted": payment doesn't equal entry.
- Wizard keeps a step list, Back, and Save draft; consent defaults to unanswered.

## 2. Admin console

**Principles:** event scope always visible in the top bar; navigation filtered by role (try the role switcher); queues with counts in the sidebar; every dangerous action has a reason and an audit trail; sensitive fields show a lock, not data.

**Navigation (grouped):** Overview · Events (Editions, Pricing, Registrations, People & claims, Spouse pairing) · Operations (Camp review, Accommodation, Safeguarding & check-in) · Money (Orders & finance) · Governance (Staff & roles, Audit).

| Screen | Purpose | Roles | BRD refs |
|---|---|---|---|
| Dashboard | Unique people vs registrations, exception queue, per-edition cards | all | §18.1 |
| Event editions | Lifecycle stepper, capability toggles (Accommodation off for Couples), clone without people data | Super, Event | EVT-01–03, §7 |
| Pricing & rate plans | Matrix by band × category with Amount / Free / Unconfigured / Not offered cells; draft → approval → publish; drafter ≠ approver; publish-readiness gaps; preview quote; version history; copy-as-draft | Super, Event (draft), Finance (approve) | PRICE-01–04, §13.2 |
| Registrations | Four-axis table, filters, detail drawer with restricted health and history | Super, Event | REG-01, OPS-01 |
| People & claims | Duplicate review side-by-side; no merge button; owner verification | Super, Event | PRO-02, G-10 |
| Spouse pairing | Awaiting / disputed links, impact review | Super, Event, Accom | SS-03/04, AT-18 |
| Camp review | Human decision queue; blocked until camp rule is published | Super, Event, Safety | BB-07, D-02 |
| Accommodation | Capacity meters (sold/held/free), unit grid, paid-but-unallocated queue | Super, Event, Accom | INV-01–03, GAP-19 |
| Safeguarding & check-in | Day tabs 21/22/23, expected/on site/out/overdue, exceptions, incident with no "force success" | Super, Event, Safety | BB-03–05, OPS-02 |
| Orders & finance | Orders vs people vs outstanding, payment mismatches, duplicate webhook log, manual payment, refunds | Super, Finance | PAY-01–03 |
| Staff & roles | Role × capability matrix, edition scope, MFA, expiry | Super | OPS-01, SEC-02 |
| Audit log | Append-only, filters for pricing, custody, sensitive access, money | Super, Finance | SEC-02 |

## 3. Not yet designed (next)
*Needed for the 21 Dec build:* phone-free "I don't have an email" path is **out** (staff enter these people in admin); account menu / post-login header state; "Add dependant" and "Add collector" modals; empty/error/loading states; printed fallback manifest. *Cut by ADR-0007:* Registration desk, camp review queue UI, campaigns, extended reports.

*Original list:*
Communications (templates + delivery log), Reports and manifests (occupancy by unit/day, camper PDF), Studio/forum rosters, Forms & rules editor, Registration desk (assisted/on-site), Admin mobile layout, Portal "Add dependant" and "Add collector" modals, empty/error/loading states per screen, print templates for receipts and passes. I'd do the Registration desk and Reports next since both are operational on event day.

## 4. Caveats
- Prototype data is invented for illustration (names, counts, references). Amounts shown are only the BRD's current Singlz examples (₦0 for 0–6, ₦5,000 for 7–12 hostel); every other amount is shown as pending or "₦ …" on purpose.
- Sign-in, payment, QR and role filtering are visual mock-ups; no backend exists.
- Pages load Google Fonts; offline they fall back to system fonts.
- I viewed screenshots of the admin pricing and portal Singlz screens; I haven't clicked through every screen or tested at phone width or with a screen reader.
