# BUSINESS REQUIREMENTS DOCUMENT (BRD)

# NLWC Unified Event, Registration, Accommodation & Attendance Management System

**Document ID:** NLWC-UEMS-BRD-2026  
**Version:** 3.1 — Edition-specific, fully configurable pricing amendment  
**Document date:** 31 October 2026  
**Organization:** The New and Living Way Church (NLWC)  
**Product type:** Church-wide, multi-event, web-based event management platform  
**Initial event programmes:** Bible Bootcamp; Singlz Summit; Couples Programme  
**Target deployment:** Nigeria; event time zone `Africa/Lagos`  
**Status:** Draft for business validation, PRD, UX, architecture, technical specification and implementation planning  
**Supersedes:** *NLWC Unified Event Management System BRD v3.0*, *NLWC Unified Event Management System BRD v2.0*, and *Bible Bootcamp Management System (BBMS) — 2026 BRD v1.0*  

> **Receiving agent instructions:** Build the product **as one multi-event system**, not three disconnected applications. Use the documented **EGFM Registrar** as the existing-system reference and require a source-code audit before deciding which components can be reused. Preserve all confirmed rules in this document. Convert it into a traceable PRD, user flows, interactive wireframes, data model, architecture, threat model, development backlog, test scenarios and phased implementation plan. Clearly distinguish **CONFIRMED**, **PROPOSED**, and **PENDING DECISION**. Do not silently assume rates, age policies, legal wording, schedules or unverified original Google Forms branching. **All monetary prices, including zero/free prices, discounts, accommodation charges, pair/spouse charges, registration fees and add-on charges, are configurable per event edition and must never be hard-coded into application business logic.** The system must be suitable for real registration, payment, accommodation allocation and safeguarding of children.

---

## 1. Executive summary

NLWC organizes multiple events that may attract children, teenagers, single adults, married adults, couples and entire families. The existing Bible Bootcamp registration process is based on a multi-section Google Form. This is insufficient for recurring events, reusable identities, accommodation management, payment, bookings, event-day attendance, authorized child collection, third-party sponsorship and centralized oversight.

The church requires a **unified event management platform** that supports:

1. A church-owned event directory with reusable editions of events (e.g. Bible Bootcamp 2026, Bible Bootcamp 2027, Singlz Summit editions and Couples Programme editions).
2. One identity/profile for each participant **13 years and older** across events, with special guardian controls for minors aged 13–17.
3. Dependants aged **0–12** held as child person records connected to one or more authorized parents/guardians, without requiring a self-service login.
4. Individual event registrations per participant, including registrations made on someone's behalf.
5. Participation modes: **online**, **walk-in / non-camping physical attendance**, and **camping participant** (with event-specific restrictions and hybrid options where needed).
6. An accommodation catalogue and live inventory: chalets, special dorms, hostels and future types; **edition-specific configurable pricing**, capacities, availability, holds and assignments.
7. Accommodation selection and payments, including spouse/chalet pairing, **edition-configured** dependent pricing and sponsorship of another person's registration.
8. Secure payment confirmations, receipts and event-specific QR/barcode credentials.
9. Event-day attendance, including daily check-in/check-out for children, verified pickup/handover, and separate overnight accommodation arrival/departure records.
10. Staff dashboards, role-based administration, communication, incident handling, reports, finance reconciliation and audit logs.
11. Appropriate privacy, data protection, safeguarding and event-specific terms and permissions.
12. Paid **Couples Programme** registration and check-in without any camping/accommodation flow; a couple may have separate identities under a single group booking/order, subject to a fee/eligibility policy still to be confirmed.

**Architecture principle:** One organization, many event families, many event editions, one shared person registry, and separate registration/booking/payment/attendance records for each event.

### 1.1 Confirmed date

- **Bible Bootcamp 2026:** **21–23 December 2026**.
- **Singlz Summit event dates:** **Not supplied; must remain configurable/TBD**.
- **Couples Programme dates:** **Not supplied; must remain configurable/TBD**.
- Bible Bootcamp 2026 other-specific dates (e.g. Christmas Party/Bible Museum, camp arrival and departure time) are **not yet confirmed**; do not copy 2025 dates or times automatically.

### 1.2 Core business outcomes

- Reduce duplicate identities and repeat registration effort.
- Make it easy for a parent to register children and a sponsor to register/pay for other people.
- Safeguard children through controlled, logged daily arrival and departure.
- Prevent ineligible bookings and overbooking of accommodation.
- Track who paid, who is registered, who is physically present, where campers are accommodated and who is authorized to collect minors.
- Provide trustworthy operational and financial reports for every event and the church as a whole.

---

## 2. Definitions and conceptual boundaries

| Term | Meaning |
|---|---|
| Organization | NLWC, the entity that owns the platform and all church events. |
| Event family / template | Reusable event concept, e.g. Bible Bootcamp or Singlz Summit. |
| Event edition | A particular occurrence, e.g. Bible Bootcamp 2026 (21–23 Dec 2026). |
| Person | An individual human being with one stable, deduplicated identity record. |
| Account | Login credentials or passwordless identity used to access a profile. |
| Profile | Person's reusable identity and relevant preferences, not a particular event registration. |
| Dependant | A linked person, here generally aged 0–12 under Singlz Summit rules; no independent self-service account is required. |
| Guardian relationship | A verified/appropriately validated legal or care relationship to a minor. |
| Spouse relationship | Link between two distinct persons who have supplied/accepted a spouse association. |
| Registrant | Person who initiates and fills out a registration. May be different from attendee. |
| Participant / attendee | Person for whom an event registration exists. |
| Sponsor / payer | Person or organization paying for another participant or an accommodation booking. |
| Participation mode | Online, walk-in/day attendance or camping. |
| Accommodation category | Hostel, special dorm, chalet, etc. |
| Accommodation unit | Specific physical room, dorm, chalet, bed or inventory unit with capacity. |
| Booking | A time-bounded reservation for one or more people/units; has its own lifecycle. |
| Payment order | Financial obligation with one or more line items, payer and payment status. |
| Credential | Unique event-specific scannable QR/barcode; not sufficient proof of child pickup authorization. |
| Day attendance | A check-in and checkout/exit record on an event date. |
| Accommodation occupancy | Camp/hostel/chalet admission, unit assignment, nights stayed and final departure; separate from day attendance. |
| Consent record | Versioned, actor-attributed agreement or decision concerning terms, participation, health, media or guardian permission. |

**Critical modeling rule:** An account is not a registration; a registration is not a payment; a payment is not an accommodation assignment; a QR credential is not proof of child custody. These must be represented separately.

---

## 3. Scope and event model

### 3.1 Platform-level scope — shared by all events

- NLWC-branded website, church profile, contact/support details, event directory and event landing pages.
- Reusable person profiles containing date of birth (DOB) and verified contact channels.
- Profile links for spouses, guardians, dependants and authorized contacts.
- Secure signup, sign-in, contact verification, account claiming and recovery.
- Event edition creation, configurable rules, quotas, capacity, prices, registration periods and published content.
- Self registration, family registration and registration for others.
- Event-specific consent/eligibility forms, registration states and access credentials.
- Accommodation inventory, holds, assignments, payment collection, reconciliation, refunds/cancellations and audit trail.
- Check-in/check-out, QR lookup, staff devices, child pickup and incident escalation.
- Transactional notifications, attendee dashboards and administrative reporting.
- Data governance, access control, accessibility, support and monitoring.

### 3.2 Event-specific policy modules

**Bible Bootcamp:** children, guardians, daily attendance, camp suitability review, medical/welfare collection, studio broadcast, parent forum, optional special events and safeguarding.

**Singlz Summit:** marital-status-linked accommodation entitlement, spouse pairing, dependant age/price bands, single attendee classification, shared lodging inventory, accommodation fees and payment.

**Couples Programme:** paid registration only; separate linked participant registrations and shared order where appropriate; no accommodation, lodging reservations, camping payments or camp assignment screens.

Other future NLWC events must be created without writing new account, checkout, payment or inventory systems. **Accommodation is an event capability flag**, not mandatory for every event; Couples Programme is the first no-camping paid example. Event-specific forms and rules should be configurable; complex policies may use reviewed, versioned rule modules.

### 3.3 Out of scope unless approved

- A SaaS platform for unrelated churches/organizations. **This is initially a single-organization NLWC product.** Future multi-tenant SaaS should not be assumed.
- An automated medical diagnosis engine or autonomous fitness-to-camp approval.
- Mandatory recording or posting of images of children.
- Unsupervised staff communication with children.
- Automatically merging two person profiles merely because their names match.
- Specific payment processor or architecture technology choice: engineering should recommend these in the TechSpec.

---

## 4. EGFM Registrar: Existing-System Baseline and Adaptation Strategy

**Status: documented EGFM behavior, NOT a completed source-code audit.** The supplied *Registrar System – Business Requirements Document (EGFM)* describes a deployed-purpose, multi-event registrar. It provides a strong foundation for NLWC; it is **not** proof that the code can be copied legally, deployed as-is, or extended without correction. Confirm ownership/IP and code permissions before adaptation. Do not migrate EGFM attendee personal data into NLWC without separate authorization and lawful basis.

### 4.1 Existing EGFM architecture (documented)

| EGFM component | Existing technology and role | Initial NLWC disposition |
|---|---|---|
| `registrar.io` | .NET 10 Web API; MySQL; Dapper plus EF Core migrations | **Extend/refactor**, subject to code audit, security and migration testing |
| `registrarv2` | Next.js/React attendee portal | **Extend** for reusable NLWC profiles, proxy/self/family/event-specific UX |
| `registrarv2-admin` | Vite/React administration | **Extend** to policy configuration, finance, child safety, accommodation and reports |
| `registrarv2-checkin` | React/Vite QR PWA | **Refactor** for per-day checkin/checkout, collector verification and camp occupancy |
| EGFM member-management integration | External upstream member identity import | **Remove/replace or make optional**; NLWC must not rely on EGFM member service |

**Potentially reusable EGFM capabilities:** person profiles, DateOfBirth, family and dependant links, event editions, participation types (`WalkInParticipant`, `OnlineParticipant`, `CampingParticipant`), lodge types, room-group assignment, ballot/reservation queue, Paystack gateway/webhooks, offline cash recording, QR PDF tickets, admin privileges, check-in PWA, children-session handover, notifications and reports. See attached EGFM BRD §§4–13.

### 4.2 Changes required before accepting EGFM implementation for NLWC

1. **Model refactor:** separate Person/Account, Registration, AccommodationBooking/Occupant/Assignment, Order/Payment, Credential, DailyAttendance, CustodyHandover and Consent. EGFM's single registration combines many of these, including one-time `CheckedIn` and direct payment fields.
2. **New NLWC fee model:** **all prices are administered through edition-specific versioned rate plans**; Singlz uses the approved flat rates for each applicable category with **no worker/employed vs unemployed classification**. Historical rates from one edition must not automatically become rates for another. Keep any old EGFM worker/minister fields for legacy reporting only if genuinely needed; they cannot alter Singlz rates.
3. **Verified price source:** server generates and validates all quotes (EGFM BRD describes a frontend-calculated amount); payment webhooks verify signature, amount, currency, order and idempotency.
4. **Atomic inventory/financial state:** never present a paid confirmed camper as housed if a booking has no reserved/assigned capacity. EGFM documents `Paid` alongside `AllocationError` and missing lodge in a payment race. Retain accurate entitlement and refund/reallocation treatment.
5. **Spouse pairing semantics:** EGFM's zero-price pairing of another household is not automatically NLWC's married-spouse chalet pairing. Model separate person registrations, booking occupants and configurable price/entitlement; **final chalet price and occupancy policy pending**.
6. **Daily safeguarding:** EGFM has gate check-in and session-based child drop-off/pick-up; Bible Bootcamp needs separate per-day attendance and child release on **21, 22 and 23 December 2026**, as well as camper overnight accounting.
7. **Security:** EGFM's own BRD reports secrets committed to source control, weak JWT validation choices, hardcoded values and largely per-statement financial updates. Before reuse: rotate/secure secrets, implement strict token checks, MFA, auditing, proper transactions, negative authorization tests and role separation.
8. **Archived data:** EGFM's event closure copies and deletes live event tables. NLWC should retain queryable auditable historical records, avoid destructive live/history behavior as the only retention strategy, and explicitly implement lawful retention/deletion policies.
9. **Background workers:** replace unsafe implicit assumptions about 5-second single-process processing with transactional claims, retry, outbox and idempotency able to tolerate scaled API instances.
10. **Policy configuration:** ordinary event-specific dates, forms, availability, pricing, consent and attendance modes should be admin-editable, versioned and validated rather than buried inside conditionals or hard-coded event IDs.

### 4.3 Reuse decision and implementation gate

**Initial recommendation:** audit and extend the EGFM foundation if source rights, code structure, security remediation and test coverage permit. Otherwise rebuild sensitive services while reusing validated client components and non-sensitive patterns. A document describing functionality cannot establish a reliable percentage of code reuse. The separate **`NLWC_EGFM_Registrar_Gap_Analysis_v1.0.md`** contains the full `G-01` through `G-40` delta matrix, risks, audit checklist and recommended rollout.

**Required first agent deliverables:** source/IP rights confirmation, inventory of EGFM repositories and schema, dependency/build/test review, security and secrets remediation plan, migration plan, service-level reuse decision (`REUSE`/`EXTEND`/`REFACTOR`/`REPLACE`) and an end-to-end proof of the revised money/inventory/custody flows.

---

## 5. Users, permissions and governance

| Role | Major capabilities | Restrictions / conditions |
|---|---|---|
| Public visitor | See published events, pricing, FAQs and policy summaries | No private participant data |
| Participant (13+) | Maintain own account/profile and event registrations | 13–17 still subject to guardian permissions for minor activities and applicable processing |
| Parent/guardian | Manage linked dependants/minors; register, consent and authorize pickup | Access only their verified/authorized linked minors |
| Sponsor / proxy registrant | Register or pay for another person, view their own order/receipt and limited statuses | No access to another adult's sensitive profile, medical notes or ongoing account control |
| Church Super Admin | Manage NLWC configuration, staff, roles and event templates | Elevated access strongly authenticated and audited |
| Event Admin | Configure assigned event, view registrations, manage operations | Limited to assigned editions |
| Registration Officer | Review exceptions, duplicate flags, onsite registrations and claims | No unnecessary sensitive medical access |
| Accommodation Manager | Manage categories, rooms/beds, assignments, occupancy | Financial actions scoped; no health details by default |
| Finance Officer | Prices, payments, settlements, refunds, reconciliation | Minimum attendee data; no medical details |
| Child Check-in/Pickup Officer | Verify child attendance and approved collection | Can see only necessary identity, safety and pickup data |
| Camp Coordinator | Daily camp roster, arrival/departure and room allocation | May see limited safety flags; not unrestricted medical file |
| Medical/Welfare Officer | Restricted health disclosures, care needs, incidents and review | Confidential role with strong logging |
| Volunteer/Usher | Scan event QR and record appropriate presence | Cannot change payments, consents or pickup authority |
| Communications Officer | Manage templates and authorized event communications | No medical or private counselling narratives |
| Auditor/Leadership | Read approved reports, activity and audit trails | Aggregates by default, least-privilege access |

Role assignment should be **organization-wide or event-edition-scoped**, support multiple roles, and include revocation and audit evidence. No general admin should automatically receive all sensitive health data.

---

## 6. Shared person, account, family and dependent model

### 6.1 Profile data

A reusable **Person** record should capture, where needed and proportionate:

- Internal person ID; first/middle/last name or full legal name and preferred display name.
- **Date of birth** (required for age-based programmes); calculated age displayed from the configured relevant date.
- Phone, WhatsApp opt-in/channel capability, email where available, verification status and preferred communication channel.
- Optional gender only where required by event/logistics; accessible and consistent policy wording.
- Relevant contact country/location fields, optional emergency contacts and accessibility needs.
- Relationships to other people (guardian-child/dependant, spouse, emergency contact, authorized collector).
- Data completeness / verification flags, created-by details and privacy preferences.

Sensitive medical or counselling information should ordinarily be event-scoped and should **not** be copied into a globally accessible public profile without purpose and legal review.

### 6.2 DOB rules

- Collect actual DOB rather than a broad age bracket.
- Store DOB, compute chronological age based on the **event-edition age-reference date**, default proposed: edition start date. This is a proposed default requiring confirmation.
- Provide event-specific age brackets as configurable data.
- Display why a particular eligibility decision or fee applied, using the age snapshot/rule version at booking time.
- At transitions (6→7, 12→13, 16→17, 17→18), retain history and re-evaluate future editions rather than changing paid historical orders.

### 6.3 Accounts for participants 13+; guardians for minors

**Confirmed programme preference:** Individuals aged 13 and above have an independent person/profile record and pay normal accommodation prices when camping at Singlz Summit.

**Implementation distinction:** A 13-year-old may have a unique person/profile and even a permitted user account, but is still legally a minor. For under-18s, require suitable guardian consent, account controls, attendee safety measures and verification before minor-sensitive operations. Do not interpret “independent profile” as “no parent/guardian authorization.”

The account/claim workflow must accommodate a participant who has no email or smartphone, without creating a fake email address. A phone-based verification or guardian-assisted flow may be permitted where operationally appropriate.

### 6.4 Dependants

- The user profile has a **Dependants** section with Add, View, Edit, and event register actions.
- Singlz Summit dependants are **0–12**; use DOB, not a free-text age category.
- Ages **0–6** have a confirmed **free accommodation rule for the current Singlz edition**, implemented as an explicitly configured ₦0 rate, not an unconditional hard-coded global rule (scope and capacity remain subject to occupancy/safety constraints).
- Ages **7–12** have a **₦5,000 hostel accommodation rate for the current Singlz edition** based on the requester’s 2026-provided price. This is a configurable **edition rate**, not the price for future years.
- Ages **7–12 in special dorms/chalets/other lodging:** **PENDING**. Do not charge ₦5,000 for other categories or automatically apply full adult price without explicit pricing decision.
- At **13+**, the person becomes an independent attendee for registration and accommodation pricing, with their own event registration, even if their guardian sponsors the cost. The full tariff itself comes from the rate plan of the **specific edition**.
- A dependant may be connected to more than one authorized parent/guardian, with conflict handling and evidence for disputed custody/permissions. Do not assume only the payer has legal guardianship.
- Children remain separate person/attendance records; one parent booking does not replace safety tracking for each child.

### 6.5 Spouse relationship

- When marital status is **Married** for Singlz Summit, request spouse's **name and phone number at minimum**.
- Permit linking an existing person's profile after appropriate match/consent verification.
- Permit creating a **pending spouse person/registration**, optionally paying for the spouse, and delivering a claim invitation.
- The spouse maintains a separate person record and, if 13+, their own participant profile/registration; do not hide a spouse as an untracked plus-one.
- The linked spouse must be able to verify/accept the relationship link, resolve incorrect matches and update their own details.
- A spouse invitation or provisional profile does not automatically grant the initiating partner full control over the spouse's account.
- Support a change to marital status or spouse association with a review of chalet eligibility and active bookings.
- **PENDING:** whether married people can book hostels/special dorms, whether both spouses must attend to obtain a chalet, whether children are allowed in a chalet, and chalet occupancy policy.

---

## 7. Event catalogue and lifecycle

Each event template includes a name, description, branding, audience and configurable features. Every edition contains:

- Unique edition ID, title/theme, event start/end, venue(s), schedule, sessions and time zone.
- Published landing page and public event-specific FAQs.
- Registration open/close dates; early/late and on-site policies.
- Allowed participation modes: **online**, **walk-in participant**, **camping participant**; optional online + single-day physical combination.
- Eligibility criteria, DOB reference date, required forms/consents and attendee categories.
- Accommodation categories, eligible occupancy, price plans, availability windows and capacity.
- Payment settings, fee policies, booking cutoff, refund/cancellation rules.
- Attendance days, daily windows, session requirements, overnight rules, pickup policies and credential issuance.
- Staff assignments, communication templates, operational status and reporting.
- Status: DRAFT → PREVIEW → PUBLISHED / REGISTRATION_OPEN → REGISTRATION_CLOSED → LIVE → COMPLETED → ARCHIVED (with sensible skip/rollback controls).

An event admin should be able to clone a prior edition's structure into a new edition **without cloning attendee registrations, payment transactions or sensitive health records**.

---

## 8. Bible Bootcamp — confirmed and proposed business rules

### 8.1 Confirmed 2026 date and participation

- **Bible Bootcamp 2026 takes place 21–23 December 2026.**
- Children **aged 3–16** are the core child participants.
- **People older than 16 are allowed to attend the daily programme.** The application must not reject them simply for being above the child-programme age.
- All children physically attending the daily programme require **daily check-in and check-out controls**, notably **non-campers**. Campers also require daily safety/accountability attendance in addition to their accommodation/overnight roster.
- Physical attendees can be non-campers; physical attendance does **not** imply camping.
- A guardian should be able to register multiple children in one workflow, with a unique individual event registration/credential for each.

### 8.2 Legacy 2025 form baseline to carry forward thoughtfully

The supplied six-section 2025 Google Form captured:

1. **Registration:** guardian name, WhatsApp phone, child name, gender, attendance preference, studio participation and year-of-birth range.
2. **Camping and health:** wants to camp (Yes/No), symptoms/conditions (fatigue, dry cough, fever, loss of taste/smell, headache, sore throat, nausea, breathing difficulty, blocked/running nose, asthma, ulcer, sickle cell, none, other), and optional allergies/special prayer/counselling text.
3. **Camping information:** venue, arrival/departure, free camp, human fitness-to-camp review, parental consent and non-camper pickup instructions.
4. **Parental authorization:** activity/medical authorization, liability statement, media decision and guardian email.
5. **Age 3–9 media release:** age-specific publicity choice.
6. **Parent Interactive Forum:** Yes / Maybe / No attendance interest.

Legacy values include a 2025 venue at Faith Plaza, Igbogunu Bus Stop, Bariga, Lagos; the 2025 camp policy was free for 10–16-year-olds, and a 2025 studio cutoff of 8:30 a.m. All of these **except the new 2026 dates** need event-admin reconfirmation for 2026; they are **not automatically current policies**.

**Legacy defects to correct:** radio for multi-condition health data; checkbox for contradictory media choices; checkbox for mutually exclusive forum choices; year-of-birth bands inconsistent with age limits; conflicting child pickup time language (noon vs 10 p.m.); duplicated media questions. The exact original Google Form branch selectors were not verified, so use an explicit, tested 2026 rule definition.

### 8.3 Proposed Bible Bootcamp registration flow

1. Guardian signs in or uses verified assisted registration; selects 2026 edition.
2. Add/select child; system calculates age at event start.
3. Choose online, walk-in/day participant, camping interest, or relevant hybrid as configured.
4. Ask studio/broadcast interest if applicable; this is a **request**, not an approval.
5. Collect event-appropriate medical/allergy information and emergencies under restricted access; allow multiple conditions, “None” exclusive.
6. If camping is requested and permitted, create a **pending camping review**; do not award a bed automatically before eligibility/consent/capacity review.
7. Capture emergency contact, authorized collectors and any special safety directions for each child attending physically.
8. Capture separate event consent, emergency medical decisions and media release, with age-appropriate wording.
9. Record guardian Parent Interactive Forum interest if the event includes one.
10. Review, submit, receive registration reference and credentials/status according to policy.
11. Guardian may add another child without recreating guardian profile.

### 8.4 Age and camp rules

- The **3–16** group has the daily **child handover/check-out** rules.
- **17+** can attend the **daily programme**, subject to general event policy. Do not enroll them in a child-only activity or camp category without an explicit event-admin rule.
- Historical 2025 camping eligibility was **10–16** and free. Whether 2026 retains these age/price rules is **PENDING** confirmation.
- Proposed: if 2026 still has child camp capacity and fitness review, camp approval must depend on configured age, guardian permission, review, beds and welfare requirements.
- A non-camping child must not be marked as sleeping on-site. Adult/older daily attendees use ordinary presence tracking without automatic child-custody checkout rules unless specially configured.

### 8.5 Daily child check-in and checkout (must-have)

Daily flow for **each physical child aged 3–16**, including non-campers:

1. Registration and permission checked; guardian/authorized adult presents child at staffed entry.
2. Check-in officer verifies child identity, event/day, registration status, contact/pickup authorization and relevant safety alerts at a minimum-data level.
3. Scan event credential or look up manually; issue one attendance record for that **participant + event day + attendance segment**.
4. Record check-in timestamp, gate/location, staff member, check-in method and presenting guardian/authorized person where policy requires.
5. At exit, require authorization of collecting adult. QR alone is **not** proof of pickup authority.
6. Record checkout timestamp, collector, verification method, location, staff member and exception notes as necessary.
7. Prevent silent double checkout, unauthorized collection, invisible departures and edits without audit.
8. Escalate missing child, late pickup, collector mismatch, medical incident, disputed guardianship or checkout without valid permission to a designated supervisor.
9. Return/uphold clear statuses: NOT_ARRIVED, CHECKED_IN, CHECKED_OUT, LATE_PICKUP, EXCEPTION and appropriate review/override tracking.
10. Daily records are separate: checking out on Dec 21 must not close Dec 22 attendance. Display days **21, 22, 23 December 2026**.

**Campers:** Record an **accommodation check-in once on arrival**, **daily session/presence or roll-call** for safeguarding, and a **final camp checkout** on exit. Staff must never confuse a day-roll-call with permission to release a camping child to someone else. If a child temporarily leaves the site, capture a supervised departure/return and appropriate guardian authorization.

**PENDING:** precise daily arrival/pickup cutoffs, age 16 handling at child collection, whether older-than-16 attendees may camp, whether 2026 camp is free, and attendance rules for Christmas Party/Bible Museum.

### 8.6 Bible Bootcamp optional modules

- Studio/broadcast interest → coordinator review → approved roster; honour media preferences.
- Parent Interactive Forum attendance Yes/Maybe/No; report expected attendance by day/session.
- Session/day programme scheduling and session attendance.
- Incident, first-aid and welfare reporting with restricted visibility.
- Child camp fitness-to-camp review by authorized humans; no software-only medical decision.
- Onsite registration desk and exceptions dashboard.

---

## 9. Singlz Summit — business rules

### 9.1 Audience and participation

- **Singlz Summit includes children, singles and married participants**, including people who attend physically and people who camp.
- Participation statuses: **online**, **walk-in participant**, **camping participant**.
- **Bonafide single:** age **13+** for programme/pricing classification. Individuals younger than 13 fall into child/dependant bands, not the paying single 13+ category.
- A person 13+ has an individual profile/participant registration and, if camping, pays the full applicable accommodation tariff.
- **No employed/unemployed categories or price discrimination** for Singlz. Accommodation rates are flat within each applicable accommodation category and eligibility group **for a given edition**, but the amount may change between editions and must be configurable by authorized administrators.
- Marital status and spouse relationship are relevant to chalet eligibility, not general user rights or employability.

### 9.2 Accommodation access

| Participant situation | Chalet | Special dorm | Hostel | Pricing |
|---|---|---|---|---|
| Married, spouse details provided | Eligible **subject to verified event policy, available inventory and booking conditions** | Availability/policy to confirm | Availability/policy to confirm | Configured rates; chalet pairing supported |
| Single, age 13+ | **Not eligible** | Eligible | Eligible | Full flat accommodation rate |
| Dependant, 0–6 | Depends on responsible adult's booking and age-specific occupancy/safety policy — **PENDING** | PENDING occupancy policy | PENDING occupancy policy | **Free accommodation** as confirmed, subject to registration and lawful capacity |
| Dependant, 7–12 | **PENDING** | **PENDING** | Permitted subject to guardian/safety arrangements | **₦5,000 in hostel for current edition only**; other categories **PENDING** |
| Participant age 13–17 | Not eligible unless qualifying married per event policy (non-standard; requires manual legal/policy review) | Eligible as single where appropriate | Eligible as single where appropriate | **Full rate**; guardian authorization applies |

**Important:** Eligibility to a category is different from eligibility to a particular shared room/bed. Allocations may also require approved sex/age/guardian/household separation policies, safeguarding and room capacity checks. Do not fabricate those policies.

### 9.3 Current-edition dependant pricing example — not permanent prices

| Age on configured event reference date | Relevant status | Current-edition hostel example | Other accommodation charge |
|---|---|---|---|
| 0–6 | Dependant | ₦0, **configured free-rate rule for this edition** | **Unresolved occupancy/price application**; current-edition free age principle must be reconciled with category policy |
| 7–12 | Dependant | **₦5,000 for the current edition only** | **Pending** — no automatic rate |
| 13+ | Independent participant | Edition-configured **full hostel rate** | Edition-configured **full category rate** |

**Caution:** The requester confirmed the 0–6 free rule and ₦5,000 7–12 hostel price for the current Singlz Summit pricing context only. **Do not set either as a universal default for future event editions.** Actual numbers of people occupying accommodation still count toward safety limits, even when the configured rate is ₦0. The application must distinguish an explicit approved zero-price rule from a missing/unconfigured price.

### 9.4 Married/chalet pairing flow

1. Married participant registers or edits Singlz Summit registration and indicates **Married**.
2. System requests spouse's **full name and phone** (at minimum) and optionally email/DOB where appropriate and consented.
3. Search possible matching spouse records using privacy-respecting, non-revealing matching (e.g. verified phone confirmation). Do not disclose whether a third person's profile exists to an unauthorized searcher.
4. If spouse already has a profile, send/require spouse verification to **link** the relationship; do not create a duplicate.
5. If spouse is not on the system, create a **provisional spouse person/registration** and send a secure claim link/notification.
6. Applicant selects an available chalet category/unit under a rate plan defined by the organizer. A chalet booking has both partners as expected occupants or designated pairing participants as policy requires.
7. Permit one spouse to pay for **their own and the spouse's chalet pairing** in a combined order; show exactly what is being covered, avoid duplicate charges, and preserve per-person confirmation.
8. Payment success is verified server-side; the booking becomes confirmed only if capacity, spouse and applicable consent conditions are met. If the pair is incomplete, keep a clearly labelled pending status rather than assigning a final room without policy approval.
9. Each spouse receives their own registration confirmation and, when eligible, individual access credential; both must be independently identifiable at arrival.
10. Both have distinct physical accommodation check-in/out records, even when one person paid the combined order.
11. Changes of spouse link, marriage status, occupancy or cancellation trigger an explicit re-check of chalet entitlement and potential repricing/relocation/refund workflow.

**PENDING:** precise meaning and price of “chalet pairing”; whether it is a couple bundle, two beds, a whole chalet, a surcharge, or an allocation fee; whether both need to be paid before confirmation; physical identity verification requirements; whether an absent spouse invalidates a chalet stay.

### 9.5 Single participant flow

1. 13+ individual selects Singlz Summit edition and attendance mode.
2. If camping, presents only allowed room categories (special dorm, hostel and any future explicitly approved single-eligible options), with **no chalet**.
3. Shows current total, rate breakdown, permitted dependants where applicable and payment choices.
4. An adult/guardian/sponsor may pay on the participant's behalf without acquiring ownership of their profile.
5. After confirmed registration/payment and eligibility checks, issue event credential and relevant booking details.

### 9.6 Children/dependants at Singlz Summit

- A linked guardian may add a child 0–12 to their account/profile, giving the dependant a stable person ID.
- Each attending dependant receives **an individual event registration**, attendance record and appropriate credential if useful for operations.
- Determine rate strictly from DOB + event reference date + selected accommodation category + published rate version.
- A child 7–12 selecting hostel must receive a line item priced using the **selected edition’s approved 7–12 hostel rate** (₦5,000 for the currently specified Singlz edition); no employment-based questions/rates.
- For a child 7–12 in non-hostel lodging, block checkout or mark `RATE_PENDING` until an administrator publishes an approved rule. **Do not substitute a guessed figure**.
- A child 0–6 attracts a ₦0 accommodation charge but consumes occupancy capacity where permitted.
- For 13+ children, use individual independent registration/pricing, with guardian permission for those under 18 as applicable.
- **PENDING:** child check-in/guardian pickup and overnight safeguarding procedures for Singlz Summit; propose using the shared child safety system wherever children attend physically, subject to event-specific permissions.

---

## 10. Couples Programme — Paid Registration, No Camping

### 10.1 Confirmed scope and non-assumptions

- **CONFIRMED:** NLWC also runs a **Couples Programme**. It requires event registration and payment; **it does not require camping or accommodation**.
- **PENDING:** event title/theme, date, venue, attendance mode (physical/online/hybrid), exact couple/admission eligibility, permitted solo attendance if any, registration fee, whether charge is **per couple or per person**, transfer, cancellation/refund and maximum venue capacity.
- The label *Couples Programme* does **not** by itself prove that a legal marriage verification is required for that event. Unlike **Singlz Summit chalet access**, marriage-only access to the Couples Programme is not confirmed. Make it a versioned event rule once NLWC decides.
- **PROPOSED:** allow two individuals to be associated with one **CoupleRegistrationGroup** or linked booking; each has their own `Person` and `Registration` for identity, eligibility, credential and attendance, while one `Order` can pay for one or both. The event's price plan determines whether one line item covers the pair or each attendee is billed separately.
- **Strict exclusion:** no hostel, chalet, dormitory, lodging reservation, camper check-in, bed assignment or accommodation pricing appears anywhere in Couples Programme registration or administration. The platform must support a **paid non-accommodation** event without workaround dummy lodges.

### 10.2 Participant journeys

**Option 1 — Existing person with spouse/partner linked:**
1. Open the published Couples Programme edition and read the event description, programme eligibility and published price.
2. Select eligible participants from linked profiles where authorized; verify/refresh necessary participant/contact details.
3. Set the applicable couple-group relationship only with adequate participant confirmation; protect the other person's account.
4. Confirm event-specific terms, required guardian permissions if any minor is eligible, and any consent records.
5. Generate a server-priced order with an itemized per-couple or per-person basis as NLWC has configured; choose Pay Now/Pay Later **only if enabled** for that event.
6. On confirmed payment (or approved pay-later handling), issue each eligible participant a separate event credential and deliver appropriate payment/registration notices.
7. On the event date, perform ordinary event check-in. Do not create accommodation occupancy events.

**Option 2 — Register spouse/partner who is not on the platform:**
1. Authorized registrant supplies the other person's minimum required identity/contact information and sends an invite or enters a direct registration for another person.
2. System creates a pending, unclaimed person/proxy registration without claiming account ownership or assuming consent on their behalf.
3. Registering person may pay the full order (if permitted); beneficiary gets registration and payment notices at the verified/contactable address and can claim/correct their own profile.
4. The other person must complete any individual consent and eligibility tasks required before event admission. Payment alone does not override those tasks.

**Option 3 — Register and pay for an unrelated couple:** sponsor uses the cross-platform register-on-behalf workflow to create or select **two distinct beneficiaries** and pays one authorized group order. Sponsor is recorded as payer/registrant, not as either beneficiary. Additional privacy/consent checks apply where necessary.

### 10.3 Functional requirements and rejection rules

| ID | Priority | Business requirement | Minimum acceptance condition |
|---|---|---|---|
| CP-01 | M | A Couples Programme edition is a paid registration event with accommodation disabled | Neither the frontend nor backend allows camping or lodge bookings for it |
| CP-02 | M | One event can price a couple together or people individually | Server-priced fee basis is configurable, versioned and shown on checkout; no fee charged before approval |
| CP-03 | M | Participant records remain individual even if one order covers two people | Both people get separate registration IDs and personal credentials |
| CP-04 | M | An organizer can register/pay on behalf of either or both people | Beneficiary/payer/registrant distinctions and notifications are preserved |
| CP-05 | M | Payment success, failed/retried payments and refunds do not generate duplicate tickets | One order fulfillment with auditable receipts and entitlement state |
| CP-06 | M | Check-in is ordinary admission/session attendance, not camp arrival | No accommodation check-in state or room assignment generated |
| CP-07 | M | Event-specific eligibility/consent rules are adjustable | Unknown marital/age rule is not hardcoded merely from the event name |
| CP-08 | S | Optional couple/group-level attendance and finance reporting | Reports show couples registered/paid plus individual people admitted without double-counting revenue |

### 10.4 Financial and reporting distinctions

- Reports must present **orders**, **couple/group registrations**, **individual participants**, **paid entitlements** and **people checked in** as distinct measures. For example, one paid couple order may represent two attendees and must not be counted as two payments.
- One person cancelling or becoming ineligible must follow the event's approved policy for the other person's admission and partial/full refunds; **policy not yet supplied**.
- This programme is a reference implementation for future NLWC **paid, non-residential events** and must validate that the platform does not require accommodation to collect registration fees.

---

## 11. Registration for another person (proxy registration/sponsorship)

### 11.1 Design decision

Support **both** pathways. The recommended **default**, especially for sponsored attendees, is **register now on their behalf and pay now or later**. The invite-only pathway remains available as a lightweight alternative.

The most important distinctions:

- **Registrant:** Person providing information and initiating registration.
- **Beneficiary:** Person who will attend.
- **Payer:** Person financing some/all charges.
- **Owner of attendee account:** The attendee after verification/claiming, not necessarily the payer.
- **Legal guardian:** The person authorized to provide minor-specific consent; sponsorship does not create guardianship.

### 11.2 Path A — Invitation-first

1. Existing user opens an event and selects **Invite Someone**.
2. Supplies an email or verified reachable phone (allow optional message; no unauthorized data exposure).
3. System sends an event-specific invitation containing secure expiring claim/registration link.
4. Invitee creates/claims their own account and completes their event registration.
5. Sponsor may elect to pay a defined amount/order if supported, but **invitation alone creates no completed registration and no guaranteed accommodation**.
6. Track states `INVITED`, `OPENED` (if supported), `ACCEPTED`, `EXPIRED`, `DECLINED`, `REMINDER_SENT`; protect against harassment/spam.

**Best for:** people who prefer to enter their own details. **Limitation:** weak conversion if the invitee does not act.

### 11.3 Path B — Register-for-someone (recommended)

1. Sponsor selects **Register Someone Else** on the target event.
2. Enters the **minimum compulsory information** needed for an initial provisional registration: full name, accurate DOB where eligibility/pricing requires it, a reachable verified/confirmable contact (email or phone), intended participation mode and event-specific minimum data. Marital/guardian details are collected where relevant. Do not solicit sensitive health answers about a third-party adult without appropriate authorization.
3. System looks for likely duplicate registrations without disclosing another person's profile details; offer safe linking/claiming, not automatic merges.
4. Sponsor chooses **Pay Now** or **Pay Later**, and selects accommodation where rules allow. Free registrations can have no monetary order.
5. System creates a unique **beneficiary event registration** with `PROVISIONAL_PENDING_CLAIM` or an equivalent explicit status; it also creates a separate financial order if required.
6. Beneficiary immediately receives an event registration notification with their name, edition, registration reference, sponsor identity where appropriate, pending tasks, and secure **Claim / Complete Profile** link (subject to a legitimate contact channel). Sponsor gets confirmation of the action.
7. If Pay Now, payment is processed and verified using trusted server-to-server gateway callbacks and reconciliation. The beneficiary receives a **payment confirmation** and QR/barcode credential once the edition's issuance conditions are met.
8. If Pay Later, sponsor/beneficiary can open the secure payment link and pay before an admin-configured deadline, subject to inventory holds and price validity. Do not imply an indefinite bed reservation.
9. When beneficiary claims account, they verify contact ownership, approve/correct shared details, provide missing event information and make required own/guardian consent decisions.
10. Once eligibility, payment (if applicable), required consent and booking state permit, the credential becomes **ACTIVE**; prior issued codes may remain restricted as `PENDING_COMPLETION` until requirements are satisfied.
11. Sponsor can view their own payment order and a minimal “completed/pending” outcome, but not the beneficiary's new login, medical details or confidential profile information.

**Business promise:** Registration email **on registration**, and verified payment confirmation plus event credential **on successful payment when eligible**. Receiving a QR does **not** waive identity verification, guardian consent or entry rules. For free registrations, a credential may be issued on successful confirmation subject to safeguarding requirements.

### 11.4 Proxy edge cases

- Sponsor pays for multiple people in one cart; each gets an individual event registration/credential and one or more allocated payment line items.
- Recipient has no email: use approved phone/OTP or assisted notification path; prevent unrecoverable fake accounts.
- Recipient already has an account: securely claim/link without revealing confidential details.
- Recipient refuses or corrects the details; policy defines refunds and cancellations, preserves audit trail.
- Sponsor pays but recipient fails to claim or give required consent: payment does not by itself enable access; state remains pending.
- Sponsor and recipient share a phone/email (common for family members): establish identity and linked guardian flows, not insecure account merges.
- Provisional registrations expire or are anonymized under approved retention policy if unclaimed; avoid hoarding inventory.
- One person can register their spouse and dependants in a single purchase but each person has an individual event participation record.
- All financial and allocation adjustments are auditable and idempotent.

---

## 12. Accommodation management

### 12.1 Inventory hierarchy

Model the following independently:

`Venue → Building/Zone → Accommodation Category → Accommodation Unit (e.g. chalet/room/dorm) → Bed/Capacity Slot (if applicable)`.

Accommodation units have code/name, location, capacity, occupancy constraints, availability dates, amenities (as needed), out-of-service status, assigned staff, operational notes and maintenance blocks. Availability is **edition-specific**, even where physical rooms are reused across events.

Categories initially include:
- **Chalets** — only married participant access under Singlz Summit, with a verified/configured spouse-pairing policy.
- **Special Dorms** — available to eligible singles and others if event policy permits.
- **Hostels** — available to eligible participants, including the confirmed 7–12 dependant rate.
- Extensible future categories (do not hard-code only three accommodation types).

### 12.2 Booking rules

- Inventory can be offered as whole unit, room, bed or occupancy slot depending on rate plan; the event defines allocation style.
- Every booking identifies the event edition, lead registrant, all occupants, chosen category/unit and each applicable date/night.
- Rate plans may be per person, per bed, per room/chalet or per pairing; show the unit of charge clearly.
- Calculate by participant age and type, eligibility, category, date/night (if applicable), published rate version and voucher policy where enabled.
- Keep unpaid holds time-limited; release automatically on expiry or failed payment. Hold length is configurable and must be designed for payment webhook delays.
- Never overbook on concurrent requests: use transactional inventory locks/constraints and idempotent creation.
- On successful payment, revalidate hold/capacity, payment and eligibility before confirming booking.
- Support room assignment by authorized staff, reallocations, waitlists, capacity blocking, cancellation, no-shows and reconciliation.
- Support household/pair bookings and individual occupancy records; a room booking must never erase individual guest traceability.
- A paid booking must produce a receipt and an entitlement to an accommodation category, but a **specific unit/bed assignment may be pending** until arranged.
- Provide separate **booking status**, **payment status**, **allocation status** and **occupancy status**.

### 12.3 Accommodation arrival and departure

- At arrival, verify registered camper, booking entitlement, payment, guardian/consent requirements and assignment; record accommodation check-in.
- Permit a front-desk staff member to move a participant to an approved unit without changing original financial evidence; log reasons.
- Record nights and occupancy changes, absent expected occupants, late arrivals, emergency movements and final checkout.
- Record separate daily attendance / session check-ins where the event requires them.
- Produce occupancy manifest by unit and day. Sensitive medical information is not included in broad occupancy reports.

---

## 13. Payments, invoicing, rates and finance

### 13.1 Core principles

- **Bible Bootcamp 2026 payments are not assumed:** prior-year physical attendance and camping were free, but that policy must be reconfirmed for this edition.
- **Singlz Summit** supports paid camping/accommodation. Prices are flat per valid category/rate plan within each edition, **not employed/unemployed based**. Each edition can charge different amounts.
- **Current-edition example confirmed by requester in 2026:** dependant age 7–12 in hostel = **₦5,000**; 0–6 = **₦0/free**; 13+ = full appropriate category rate (amount TBD). These values are **configurable prices for that edition only**, not application constants and not the automatic prices for 2027 onward.
- Chalet pairing price, all base room rates, taxes/processor fees and cancellation/refund policy are **PENDING**; authorized admins must configure and approve the applicable edition’s rate plan before checkout opens. Future editions begin with an unapproved draft or an expressly reviewed copy, never an automatically active prior-year price.
- Store money in minor currency units (kobo) or equivalent safe decimal design; never floating-point for financial totals.

### 13.2 Edition-specific pricing configuration — mandatory platform capability (CONFIRMED)

**Business policy:** Every monetary amount in the NLWC system **MUST be configurable without changing, recompiling or redeploying code**. A specific figure supplied for one year or event edition is historical/current-edition configuration, **not a permanent business rule**. Price-setting authority is limited to authorized NLWC finance/event administrators and subject to approval, audit and publication controls.

1. **Scope of configuration:** The price engine must cover paid or free general event registration; accommodation by category, lodge, room/bed, participant age/eligibility band and pricing unit; married-spouse/chalet pairing; dependant rates; Couples Programme per-person or per-couple rates; optional programmes, ticket classes or add-ons; approved discounts, concessions, group sponsorship, service/processing fees and taxes where applicable. Unsupported or unapproved pricing dimensions remain disabled rather than invented.
2. **Separation of event and edition:** Define event templates (e.g. Singlz Summit) and individual editions (e.g. the edition configured in 2026 and the 2027 edition). An edition owns its own rate plan, rules, validity dates, currency and publication status. Editing 2027 must not alter 2026 bookings, payments, receipts, reports or rate snapshots.
3. **Administrator experience:** Finance/event admins must be able to create draft rate plans, add/edit rates and age bands, select applicable accommodation categories, set explicit free (₦0) prices, review a pricing matrix, preview example quotes, submit for approval, publish, schedule effective dates, retire rates and produce change history. Define who may approve prices separately from who may draft them; avoid unreviewed financial overrides.
4. **Missing rates versus free rates:** `UNCONFIGURED` and `FREE/₦0` are different. An accommodation combination with no approved rate cannot silently be treated as free, nor priced using another category, previous year's rate or EGFM defaults.
5. **Rules, not constants:** Maintain configurable amount and eligibility/rate-band rules. The system calculates the attendee's age from DOB at the edition's defined reference date. The current 0–6 free, 7–12 ₦5,000 hostel, and 13+ full-rate Singlz policies are rule entries for the currently approved edition, not hard-coded `if/else` prices. Marital eligibility can remain an event-specific rule without defining a monetary amount.
6. **Flat-rate meaning:** Singlz's “flat” prices mean no employed/unemployed pricing differentiation **within the edition's approved category/rate plan**; they do **not** mean identical prices for all accommodation types, ages, people or years.
7. **Currency/units:** Amounts are in NGN and stored in integer kobo or a precise equivalent. Rate plans must specify pricing unit (per person, per couple, per bed, per room/chalet, per night or fixed admission, as NLWC approves), any duration and inclusions; never infer these.
8. **Price snapshots and temporal integrity:** Server-side quotes record edition, rate-plan/rule version, participants, line items, price basis, valid-until timestamp, currency, discounts and final amount. Confirmed orders/payment records preserve the accepted amounts even if an admin later revises the published price. Expired or changed quotes must be recalculated and explicitly reaccepted; any allowed repricing/refund adjustment is separately auditable.
9. **Rate-plan lifecycle and changes:** Support `DRAFT`, `PENDING_APPROVAL`, `APPROVED`, `PUBLISHED`, `RETIRED` or an equivalent auditable workflow. Changes after sales commence create a **new version** with a documented effective date, not silent edits to settled orders. Prevent accidental overlaps or ambiguous applicable rules.
10. **Publication readiness:** A paid option cannot be sold until there is exactly one applicable approved published price rule (including an explicit free rule, if relevant), a defined rate unit and relevant booking/cancellation policy. If pricing is incomplete, mark the option `PRICE_PENDING` and disable checkout with an explanatory message.
11. **Cross-edition reuse:** Admins may **copy** a previous edition’s price matrix into a **new draft for review**, but cannot automatically activate copied rates. New rates can differ for any year; approvals and published effective dates must be edition scoped.
12. **Reports and audit:** Compare planned, quoted and collected amounts by edition/rate-plan version; include who changed/published a rate, when and why. Financial reconciliation must not retroactively recalculate earlier revenue using newer edition rates.

**Illustrative configuration, based only on the current information supplied:**

| Event edition | Fee component | Eligibility | Approved/current amount | Future-edition handling |
|---|---|---|---|---|
| Singlz Summit current edition (pricing supplied in 2026; event dates TBD) | Hostel accommodation | Dependant aged 0–6 | ₦0/free, explicitly configured | Must be reviewed and configured for next edition |
| Singlz Summit current edition (pricing supplied in 2026; event dates TBD) | Hostel accommodation | Dependant aged 7–12 | ₦5,000, explicitly configured | May change; must be reviewed and configured for next edition |
| Singlz Summit current edition | Special dorm/chalet/other | Dependant aged 7–12 | **PENDING** | Cannot charge until NLWC approves edition-specific rate/policy |
| Singlz Summit current edition | Standard hostel, special dorm, chalet, spouse pairing | Eligible full-rate participant or pair | **PENDING** | Configure each permitted amount and price basis per edition |
| Bible Bootcamp 2026 | General entry and camping, if offered | Eligible registrant | **PENDING confirmation** (2025 was free) | Explicit free or paid rate for each edition |
| Couples Programme edition | Registration | Per person or per couple (TBD) | **PENDING** | Admin selects billing unit and approved price for each edition |

**Pricing acceptance criteria:**

- Admin publishes Singlz Hostel 7–12 = ₦5,000 for one edition; a valid 9-year-old's server-side quote is ₦5,000, excluding any separately approved charges.
- Admin creates the next edition and changes this age-band hostel rate to a **test amount**, for example ₦7,000. A new edition's quote reflects that configured test rate without a code change, while existing previous-edition orders, tickets, refunds and reports keep their original amounts. The test amount is **not** an NLWC-approved future price.
- An edition without a configured 7–12 special-dorm rate cannot charge ₦5,000, ₦0 or an adult amount by inference. Checkout is blocked until an approved rate is published.
- A published explicit ₦0 age-band rule results in a valid free line item and still reserves capacity; an absent rule causes `PRICE_PENDING`.
- A finance officer cannot silently overwrite a published rate used by settled orders; a new approved version takes effect prospectively.
- Singlz attendees with the same age/category and lodging price plan receive the same configured base tariff regardless of employment status.
- A Couples Programme with accommodation disabled may charge a newly configured fee without generating a camping booking.

### 13.3 Checkout and payment behavior

1. System builds itemized cart with participant, category, nights/unit, individual rate, family/pair discounts (if approved), amount due and any externally charged fees.
2. Use server-side pricing; record rate-plan version and quote expiry. Never trust an amount sent by a browser.
3. Create a unique order and associate payment attempts, payer and beneficiary(ies).
4. Support payment immediately, or pay later subject to a published deadline and limited inventory holding rules.
5. Use approved Nigeria-compatible gateway provider, with secure hosted payment page/tokenization where possible; do not store raw card details.
6. Gateway webhook/callback is verified server-side (signature, order amount, currency, identity, idempotency); prevent duplicate fulfillment.
7. Send separate receipt to payer and confirmation to each relevant beneficiary (in line with their verified contact permissions).
8. Fulfill booking/credential only when paid and otherwise eligible. For failed/abandoned attempts, preserve accurate states and release holds after configured period.
9. Support manual/offline payment only via finance-approved audited workflow with evidence and dual review for risky overrides.
10. Support partial payments/installments **only if stakeholders enable them**; do not assume general availability.
11. Refunds, cancellations, transfers, chargebacks and overpayments must have explicit lifecycle, approval, ledger entries and notifications.

### 13.4 Reporting

Finance dashboard: gross orders, discounts, amount paid, outstanding, refunds, reversals, unclaimed sponsored orders, reconciliation mismatches, gateway settlement, counts by accommodation category, per-event edition and payer/beneficiary breakdown (with least privilege).

---

## 14. Event credentials, QR/barcodes and access

- Generate **unique, signed/unguessable, revocable event-edition credentials** per participant/registration (not one shared credential per family or payment).
- Prefer a QR code; support barcode representation/printable pass if operations require it.
- Never embed sensitive DOB, medical information, home address or full raw payment credentials in the code; use opaque token/identifier.
- Credential states: PENDING_ISSUANCE, ACTIVE, SUSPENDED, REVOKED, EXPIRED (or equivalent).
- For sponsored registrations: notify at creation; send payment confirmation and code after successful payment, with any consent/claim prerequisites clearly stated. If a code is issued early, scanning must show restricted access until requirements are met.
- A free participant receives code after registration confirmation and mandatory permissions, where appropriate.
- Staff scan with role-based application: event admission, day check-in, accommodation check-in and child pickup are **different actions** requiring different checks.
- Support lost-phone/print replacement, single-use token rotation where feasible, revocation, duplicate-scan detection and audited manual lookup.

---

## 15. Unified attendance and safeguarding

### 15.1 Attendance types

Keep separately:

1. **Event admission/entry:** participant is permitted entry at a given event location.
2. **Daily programme attendance:** each date/segment checked in and out (for relevant attendees).
3. **Session attendance:** optional workshop, studio or forum presence.
4. **Accommodation occupancy:** camper arrived, assigned accommodation, occupied nights, and final departed.
5. **Minor custody handover:** verified authorized adult brought or collected child.

### 15.2 Daily child safeguarding

- Each child physically present aged **3–16 at Bible Bootcamp** must have an accountable per-day record. Daily check-out is **mandatory for non-campers**; campers also require secure overnight accountability and logged movements as defined by event policy.
- For Singlz Summit and other events, define child daily handover and camp safeguarding policies using the same shared machinery; do not leave child safety solely to an optional QR scan.
- Capture authorized pickup persons with name/contact/relationship and authorization evidence/verification method; allow revocation and time-bounded delegation by guardian.
- At checkout, verify adult identity and authority using approved process. A scan by the child or possession of a QR alone is insufficient.
- Flag and escalate unauthorized pickup attempts, missing child, late pickup, unexpected camp departure, emergency custody changes and duplicate scans.
- Supervisor override requires reason, authorized role and immutable audit record; dangerous overrides must follow defined safeguarding escalation rather than a simple “force success” button.
- Reports include expected/present/checked-out/overdue counts and detailed incident queue, visible only to appropriate staff.

### 15.3 Walk-in attendees older than 16

- Bible Bootcamp must accept adults/older youths **17+** as daily programme participants, subject to any separate registration limits.
- Capture ordinary attendance scans and event admissions, not child-specific guardian pickup permissions by default.
- Do not assign adult profiles to child camp rules solely because they attend Bible Bootcamp.

### 15.4 Offline/low-connectivity

An event can have poor connectivity. Proposed: queued offline check-in for **low-risk entry/presence** with explicit conflict handling and device credentials. **Do not permit an unchecked offline child-custody override** or unsafe double checkout; staff must have a documented manual safeguarding fallback and reconciliation process.

---

## 16. Privacy, terms, consent and data governance

### 16.1 Should users accept terms and data protection notices?

**Yes, use a layered model rather than repeating legal text unnecessarily.**

- **At account signup / first use:** present platform Terms of Use and a clear, accessible Privacy Notice explaining controller identity (NLWC), purposes, relevant lawful bases, data recipients/payment processors, retention principles, individual rights and contact route. Record acceptance of Terms where applicable. Do not treat a Privacy Notice as a universal substitute for consent where consent is actually legally required.
- **At event registration:** present event-specific terms (registration, attendance, camp, accommodation, eligibility, pricing, cancellation, adult/child safety, late pickup). Require appropriate acknowledgement, versioned per event edition.
- **For minors:** obtain appropriate parent/legal guardian authorization and age/relationship verification, with separate activity and accommodation permission as relevant. A 13–17-year-old's profile does not waive these safeguards.
- **For sensitive health/welfare processing:** provide an explicit and appropriately lawful, proportionate collection and consent mechanism as determined by legal review; restrict access.
- **For publicity/media:** offer affirmative **I consent / I do not consent**, default unanswered; this is separate from indispensable participation terms and must be enforceable in photo/video workflows.
- **For marketing/newsletters or optional promotional communications:** use separate, non-pre-ticked choices when consent is required; do not bundle them into event confirmation messages.
- **For invitations/proxy registrations:** collect only limited third-party details initially; disclose to the beneficiary who registered them and why, and provide a clear claim, correction or objection process.
- **For communications:** distinguish transactional event confirmation/operational alerts from marketing and religious outreach preferences.

### 16.2 Legal baseline (not legal advice)

The **Nigeria Data Protection Act 2023** and Nigeria Data Protection Commission (NDPC) guidance govern personal data processing in Nigeria. The NDPC explains safeguards for child personal data and parental/guardian consent where consent is relied upon; the Child's Rights framework treats those below 18 as children. These requirements are not satisfied by simply putting a generic “I agree” box on every form. Have a Nigerian privacy/legal adviser verify legal bases, minor consent, processor agreements, cross-border transfers, breach response, and current applicable rules before launch.

Official background sources:
- NDPC — Nigeria Data Protection Act 2023: https://ndpc.gov.ng/download/nigeria-data-protection-act-2023
- NDPC — Questions and answers: https://ndpc.gov.ng/faqs/
- NDPC — Annual Report 2024, chapter discussing child data safeguards: https://ndpc.gov.ng/wp-content/uploads/2025/01/NDPC-Annual-Report-2024.pdf
- NDPC — NDP Act General Application and Implementation Directive 2025: https://ndpc.gov.ng/wp-content/uploads/2025/07/NDP-ACT-GAID-2025-MARCH-20TH.pdf

**Required legal work product:** approved Terms of Use, Privacy Notice, separate event terms/waiver as relevant, child/guardian consent, emergency-medical authorization, media release language, payment/refund policy, retention schedule, incident response plan and data processor agreements. Do not republish the old 2025 liability language without review.

### 16.3 Security and privacy requirements

- Data minimization; per-purpose lawful basis; event-specific retention and deletion/archival policy.
- Versioned consent records: actor, dependent/beneficiary, exact text/version, response, timestamp, verification method and revocation/change history.
- Strong authentication for staff and admins, preferably MFA; secure session management, rate limiting, abuse prevention, encryption in transit and at rest.
- Least-privilege access to health details, household relationships, payments and safeguarding incidents; log sensitive access and exports.
- Strict isolation of unrelated persons' profiles from sponsors and other attendees.
- Secure file uploads if added; validate types and access, malware scan if supported.
- Payment gateway integration via trusted server; never store payment card PAN/CVV.
- Human-reviewable privacy requests/corrections and subject-rights workflow consistent with applicable rules.
- Defined data breach notification and incident procedures; backup, tested recovery and change management.
- No public attendee directory exposing minors, spouse links or household details.

---

## 17. Communication and attendee portal

### 17.1 Participant portal

- Browse/register for future events; view My Events, registration statuses and credentials.
- Update own profile (DOB edits controlled/audited after paid booking), verified contacts and privacy preferences.
- Manage Dependants and spouse link; authorize, register and manage minors within permissions.
- Register/assist someone else and view **sponsorship orders** without seeing their sensitive profile.
- View accommodation request, payment due/paid, receipts and published arrival instructions.
- Claim invitation or provisional registration; correct data; resolve missing consent/verification.
- Request cancellation, refund or assistance according to event policy.

### 17.2 Notifications

Template-driven messages by email and optional WhatsApp/SMS according to infrastructure and user permissions:

- Signup verification, welcome and account recovery.
- Event registration initiated/submitted, including registration on behalf.
- Recipient invited/claimed, profile completion outstanding, guardian permission outstanding.
- Pay-later reminder, hold expiry, payment success/failure and receipt.
- Accommodation pending/confirmed/waitlisted/assigned/reassigned/cancelled.
- Singlz Summit spouse link/pairing awaiting acceptance; pairing confirmed or invalidated.
- Bible Bootcamp camp review decision, studio decision, event reminders and schedules.
- Child daily check-in/checkout confirmation and safety escalation to approved guardians (channel policy and network permitting).
- Event cancellation/important venue changes and post-event notice.

Log template version, recipient, purpose, sending outcome, delivery status and retry count. Notifications are operational aids; they must not be the sole source of safeguarding truth.

---

## 18. Operational dashboards and reporting

### 18.1 Church-wide dashboard

- Total event editions and publication status.
- Registered people (deduplicated) vs registrations (per event).
- Registration trends, participation mode distribution, children/dependants and age groups.
- Accommodation utilization, sold/held/available inventory and waiting lists.
- Paid/unpaid/outstanding/refunded amounts by edition; sponsor vs self payment.
- Check-in/out and outstanding welfare exceptions (without disclosing confidential information broadly).

### 18.2 Bible Bootcamp dashboard

- Total registered children 3–16 and older 17+ daily participants.
- Daily expected, present and checked-out children **by Dec 21/22/23, 2026**.
- Non-camper checkout still pending, overdue/late pickup and collector verification flags.
- Camp applications under review, approved, waitlisted and declined.
- Child camp accommodation/overnight counts, studio requests and Parent Interactive Forum expectations.
- Medical/welfare review counts only for authorized staff.

### 18.3 Singlz Summit dashboard

- Online, walk-in and camp participant counts.
- Single vs married and age-group distribution (only as operationally needed).
- Spouse links pending, verified pairs, chalet demand/assignment and pairing orders.
- Hostel/special dorm/chalet availability and occupancy.
- 0–6 free dependant count, 7–12 hostel paid count, 13+ fully priced bookings.
- Rate-pending cases (e.g. 7–12 non-hostel) and blocked checkouts.
- Payment/booking reconciliations and accommodation-arrival manifests.

Reports export only under authorized access, with columns minimized and personal data disclosure audited.

---

## 19. Functional requirements register

Priority definitions: **M** = must-have for relevant launch; **S** = should-have; **C** = could-have. Business approval can rephase features; no unsafe shortcuts for minor safeguarding, payment integrity or user privacy.

| ID | Priority | Requirement | Minimum acceptance test |
|---|---|---|---|
| ORG-01 | M | NLWC as organization and configurable event catalogue | Can create Bible Bootcamp, Singlz Summit and Couples Programme independently |
| EVT-01 | M | Event template and independently configured yearly edition | 2026 dates can be set without changing 2025 history |
| EVT-02 | M | Custom participation modes, registration windows and age policies | Event can offer only the permitted subset of online, walk-in and camping; Couples Programme exposes no camping |
| EVT-03 | M | Configurable event forms, terms and consent | Rule/form versions and responses stored per edition |
| PRO-01 | M | Reusable person records with DOB | Age computed on configured reference date; edits audited |
| PRO-02 | M | Accounts and contact verification | No account takeover by duplicate email/phone claim |
| PRO-03 | M | Dependants 0–12 linked to authorized adult(s) | Individual dependant identity and event registration retained |
| PRO-04 | M | 13+ independent participant records | Full-price classification; minors still require appropriate guardian controls |
| PRO-05 | M | Married spouse profile/link by name and phone minimum | Can link verified existing spouse or create pending spouse record |
| REG-01 | M | Self, family and proxy registration | Registrant and beneficiary IDs are separately recorded |
| REG-02 | M | Invite-only and register-on-behalf flows | Recipient can claim/correct without sponsor controlling account |
| REG-03 | M | Pay now / pay later for sponsored bookings | Distinct provisional, unpaid and paid statuses available |
| REG-04 | M | Accurate event- and participant-level references | No duplicate active participant enrollment on retry |
| REG-05 | M | Free and paid registration support | Free edition can confirm without creating paid charge |
| REG-06 | M | QR/barcode issuance, revocation and scanning | Each participant has independent code; no sensitive data encoded |
| BB-01 | M | Bible Bootcamp 21–23 Dec 2026 | Edition schedule displays all three dates correctly |
| BB-02 | M | Bible Bootcamp 3–16 child / 17+ daily adult/youth participation | 17+ not erroneously blocked from daily programme |
| BB-03 | M | Daily minor check-in/check-out | Every non-camping child has per-day arrival and authorized checkout |
| BB-04 | M | Camper overnight vs daily accountability records | Camp entry/exit does not overwrite daily attendance |
| BB-05 | M | Guardian/authorized collector verification | Unknown adult cannot perform routine child checkout |
| BB-06 | M | Medical, camp and media forms with access control | Multiple conditions possible; consent choices noncontradictory |
| BB-07 | S | Camp medical/human review and studio/parent forum | Explicit review states and coordinator rosters |
| SS-01 | M | Singlz Summit event-specific age/marital eligibility | 13+ single rate; singles cannot choose chalet |
| SS-02 | M | No employed/unemployed pricing | No employment factor in checkout/rate determination |
| SS-03 | M | Chalet married-only with spouse details | Marriage/spouse requirement enforced and tracked |
| SS-04 | M | Spouse pairing and pay-for-spouse | Two occupant registrations and associated itemized payments |
| SS-05 | M | Current-edition 0–6 free accommodation rule | Edition has explicit approved ₦0 rule; occupancy still counted; future editions configurable |
| SS-06 | M | Current-edition 7–12 dependant hostel ₦5,000 | Server quote equals edition-configured ₦5,000 for current edition before unrelated approved charges; amount editable for later editions |
| SS-07 | M | Non-hostel 7–12 unpriced until approved | Checkout blocked with transparent pending-rate explanation |
| SS-08 | M | 13+ full category price | No dependant discount incorrectly applied at age 13 |
| CP-01 | M | Paid Couples Programme with no accommodation | Can pay without ever choosing a lodge/camp/room |
| CP-02 | M | Couple/group order plus two participant registrations | One group order may pay for two independently identifiable participants |
| CP-03 | M | Edition-configurable price amount, billing basis and eligibility | Per-person/per-couple and amount are edition settings; none assumed until approved |
| CP-04 | M | Proxy/sponsor enrollment and claims | Sponsor can pay for both, and each beneficiary retains own claim rights |
| CP-05 | M | Admission for paid couple event | Personal QR check-in without camp occupancy |
| INV-01 | M | Room/bed/chalet inventory and capacity | Concurrent bookings cannot sell same capacity twice |
| INV-02 | M | Inventory hold, expiration and assignment | Unpaid holds expire; allocated guests individually identified |
| INV-03 | M | Accommodation arrival/departure | Individual occupancy audit separate from daily admission |
| PRICE-01 | M | Admin-managed edition-specific published price plans | No monetary amount hard-coded; all event fees, discounts, free rates, lodging and pairing prices versioned per edition |
| PRICE-02 | M | Versioned server-side price quotes with monetary snapshots | Changing next-year pricing never changes prior-year paid orders, receipts or reports |
| PRICE-03 | M | Distinguish approved free from missing price | ₦0 requires explicit published rule; unconfigured amount blocks checkout |
| PRICE-04 | M | Pricing publication approval and audit trail | Only permitted users create/publish rates; changes to active rates are versioned and effective-dated |
| PAY-01 | M | Secure gateway initiation and payment verification | Duplicate webhooks never create duplicate fulfillment |
| PAY-02 | M | Itemized billing, payer vs attendee and receipt | Sponsor can pay for spouse or unrelated attendee |
| PAY-03 | M | Cancellation/refund/reconciliation audit | Correct financial states after payment reversal |
| COM-01 | M | Registration and payment notifications | Beneficiary receives registration then payment/credential messages |
| COM-02 | M | Claim link, retries and expiry | Unclaimed user can securely complete account later |
| OPS-01 | M | Event-scoped staff RBAC | Check-in staff cannot view finance/medical files |
| OPS-02 | M | Incident / pickup exception register | Safe escalation and audit, not silent force checkout |
| RPT-01 | M | Event, finance, attendance and occupancy reports | Numbers reconcile to source records; permission gates enforced |
| SEC-01 | M | Privacy notice, terms, guardian and media consent | Version/time/actor evidence retained; media opt-out honored |
| SEC-02 | M | Security, audit, backups and recovery | Admins have MFA; sensitive access and edits traceable |
| UX-01 | M | Responsive accessible registration and staff interface | Works on common mobile screens and devices at gates |
| UX-02 | S | Save draft, resume and multi-person cart | Sponsor can return to unfinished registrations |
| OPS-03 | S | Offline low-risk arrival scanning with safe reconciliation | No silent offline custody release |
| COM-03 | S | WhatsApp integration with approved templates | Delivery and opt-out/consent rules observed |
| RPT-02 | S | Event comparison and historical growth analytics | Distinguishes unique people from edition registrations |

---

## 20. High-level entity/data model

Suggested entities, not a final normalized schema:

1. `Organization`, `OrganizationSetting`, `StaffRole`, `RolePermission`, `UserRoleAssignment`.
2. `Person`, `Account`, `VerifiedContact`, `ContactVerification`, `ProfileCompletion`, `AccountClaimToken`.
3. `PersonRelationship` (type SPOUSE/GUARDIAN/DEPENDANT/EMERGENCY_CONTACT/AUTHORIZED_COLLECTOR), `RelationshipVerification`.
4. `EventTemplate`, `EventEdition`, `Venue`, `EventLocation`, `EventSession`, `EventDay`, `EventPolicyVersion`, `FormDefinition`, `FormResponse`.
5. `Registration` (participant and event), `RegistrationActor` (who registered), `AttendanceMode`, `RegistrationStatusHistory`, `Invitation`, `ProxyRegistration`, `SponsorLink`.
6. `ConsentRecord`, `GuardianAuthorization`, `MediaPreference`, `HealthDisclosure`, `CarePlan`, `IncidentReport`.
7. `AccommodationCategory`, `AccommodationUnit`, `BedSlot`, `InventoryCalendar`, `RatePlan` (scoped to EventEdition), `RateRule` (age/type/unit/eligibility criteria), `RatePlanVersion`, `RateApproval`, `PriceQuote`, `PriceQuoteLine`, `CapacityHold`. Separate approved explicit zero-price entries from missing rates.
8. `AccommodationBooking`, `BookingOccupant`, `UnitAssignment`, `OccupancyRecord`, `BookingStatusHistory`, `WaitlistEntry`, `SpousePairing`.
9. `Cart`, `Order`, `OrderItem`, `OrderAllocation` (payer/beneficiaries), `PaymentAttempt`, `PaymentTransaction`, `Refund`, `LedgerEntry`, `SettlementReconciliation`.
10. `EventCredential`, `CredentialScan`, `DailyAttendance`, `SessionAttendance`, `AuthorizedPickup`, `CustodyHandover`, `AttendanceException`.
11. `NotificationTemplate`, `Notification`, `NotificationDelivery`, `AuditLog`, `SupportTicket`, `DataAccessRequest`, `RetentionJob`.

**Data integrity rules:**

- Each human should have one stable Person ID, with cautious/manual duplicate resolution.
- Each person can have at most one ordinary active registration per event edition unless an approved event supports separate distinct ticket types; attendance records are separate from registrations.
- A payer can cover multiple beneficiaries; a beneficiary can receive one or more payments/adjustments for an order according to policy.
- A dependant and spouse are each Persons in their own right, not JSON blobs attached to a parent.
- All age-eligibility/rate decisions reference a rule version and event age-reference date.
- A confirmed booking must be backed by valid allocation entitlement and available capacity.
- Safeguarding events append immutable actor/time records, rather than overwriting past checkins.

---

## 21. Key lifecycle state machines

These should be separate bounded contexts in the PRD/TechSpec:

**Registration:** `DRAFT → PROVISIONAL_PENDING_CLAIM → SUBMITTED → CONFIRMED → CHECKED_IN/COMPLETED` with `NEEDS_ACTION`, `CANCELLED`, `REJECTED` or `EXPIRED` as relevant. Do not use a single status field to represent every subsystem.

**Payment:** `NOT_REQUIRED | UNPAID → PENDING → PAID`; `FAILED`, `PARTIALLY_PAID` only if enabled, `REFUND_PENDING → REFUNDED`, `CHARGEBACK`.

**Booking:** `QUOTED → HELD → REQUESTED/PENDING_PAYMENT → CONFIRMED → ALLOCATED → OCCUPIED → CHECKED_OUT`; `WAITLISTED`, `CANCELLED`, `EXPIRED`, `NO_SHOW`.

**Spouse pairing:** `NOT_REQUIRED | PENDING_DETAILS → LINK_PENDING_CONFIRMATION → LINKED/VERIFIED → PAIRING_PENDING_PAYMENT → PAIRING_CONFIRMED`; exception/unlink states with reassessment.

**Minor day attendance:** `EXPECTED → CHECKED_IN → CHECKED_OUT`; `ABSENT`, `LATE_PICKUP`, `EXCEPTION/ESCALATED`. Each day is a distinct entity.

**Camp occupancy:** `EXPECTED → ARRIVED → UNIT_ASSIGNED/OCCUPIED → DEPARTED`; `MOVED`, `TEMPORARILY_OUT`, `NO_SHOW`, `EXCEPTION` as policy requires.

**Account claim:** `NOT_INVITED → CLAIM_INVITED → CLAIM_VERIFIED → PROFILE_COMPLETED`; `EXPIRED`, `DECLINED`, `DISPUTED`.

Statuses and valid transitions must be formalized in the TechSpec with authorization, notification triggers and compensation/reversal behavior.

---

## 22. End-to-end journeys / acceptance scenarios

### Scenario A — Bible Bootcamp non-camping child (age 7)

Guardian registers child for 21–23 December 2026, selects daily physical participation, supplies contact and approved collectors, completes guardian/media permissions. Registration is confirmed. Child receives a unique event reference/credential. On **each date** Dec 21, 22 and 23, staff record arrival and separately verify authorized adult at exit. The dashboard never treats a Dec 21 checkout as checkout for Dec 22. If a different adult comes, normal checkout is blocked and escalated until authorization is confirmed.

### Scenario B — Bible Bootcamp teenager (age 14) requests camp

Guardian registers teen, requests camping if 2026 camp policy permits, and provides safety/medical details. System records camp request pending human review. It does not assign overnight accommodation merely because the applicant is 14. After guardian permission, approval and capacity, arrival logs establish overnight occupancy. Daily roll-call/checkin and final camp departure are separately tracked.

### Scenario C — Bible Bootcamp attendee age 19

Adult/older person attends the **daily programme**, registers for allowed physical walk-in mode, receives event credential and has daily attendance tracked. They are not rejected by the 3–16 child age band and do not enter child-custody checkout logic by default.

### Scenario D — Singlz single, age 20, hostel

Participant chooses camping; chalet is unavailable; hostel and special dorm appear if available, with approved full flat category prices. Employment status does not affect price. After payment webhook verification, attendee receives confirmation/code and booking entitlement. Accommodation staff assign unit and record arrival/departure separately from sessions.

### Scenario E — Singlz married person books for spouse

Married attendee provides spouse name and phone. Existing spouse is verified/linked, or provisional spouse profile created. Buyer chooses permitted chalet pairing, sees itemized cost for covered people, pays one valid order. Spouse receives independent notification, can claim/verify profile and required consents, and obtains their own credential after eligibility. Check-in identifies **both** occupants separately.

### Scenario F — Singlz dependant age 5

Guardian adds a 5-year-old dependant with DOB. Selected permitted accommodation has ₦0 child price and includes child in occupancy count. Child has their own registration and safety tracking, not an invisible extra occupant.

### Scenario G — Singlz dependant age 9 in hostel

Guardian books hostel with 9-year-old dependant for the current Singlz Summit edition with an approved **₦5,000** 7–12 hostel rate. The price engine uses the **edition’s configured amount** (₦5,000 for this edition), regardless of employment. It shows beneficiary and rate version, honors capacity, and never copies that amount automatically into future editions.

### Scenario H — Singlz dependant age 9 in special dorm

System finds no approved 7–12 special dorm rate for the selected edition. It must **not guess ₦5,000, ₦0 nor adult price**. It displays that the option is pending rate confirmation or disables booking and provides an alternate permitted option.

### Scenario I — Singlz age 13 sponsored by parent

13-year-old is an independent participant record for Singlz pricing and pays the full approved category rate, but uses guardian authorization/safeguards as a minor. Sponsor may pay and receive a receipt without bypassing guardian terms.

### Scenario J — Register and pay for an adult friend

Sponsor inputs minimal compulsory event info, chooses accommodation, pays, and sees their own order. Friend receives registration email, and after verified payment receives confirmation/QR; they later securely claim account and complete personal details. Until required checks are satisfied, QR may be issued in restricted state rather than granting entry. Sponsor must not read friend's private profile after claim.

### Scenario K — Pay later, stock expires

Sponsor registers another person and selects Pay Later, temporarily holding a hostel bed if policy permits. Hold expires at configured time. Beneficiary remains notified/registered provisionally but does not retain a sold-out bed. Later payment checks live inventory and transparently asks beneficiary to choose a replacement if necessary.

### Scenario L — Double payment callback

Gateway calls payment-success webhook twice. System records **one** paid transaction and issues **one** booking/credential fulfillment, logging the duplicate safely.

### Scenario N — Paid Couples Programme (no camping)

Two existing profiles are grouped under a Couples Programme registration. The server generates a charge in the configured per-person/per-couple format, a single sponsor can settle the order, and each participant receives an individual credential. At admission, both scan independently. No lodge, berth or camper stay is created. **Exact fee/eligibility must be approved before enabling checkout.**

### Scenario O — Couples registration by a sponsor

A person registers and pays for an unrelated couple. Both beneficiaries receive the relevant notifications and secure claim instructions; neither beneficiary's account belongs to the sponsor. Count one financial order and two attendees, not two orders; record individual consent tasks.

### Scenario M — Consent for a 15-year-old with own account

Minor creates their own profile and can see event details, but guardian authorization is still required where applicable. A sponsor's payment cannot be treated as guardian consent. Media refusal is respected without automatically rejecting ordinary attendance unless a particular recorded activity truly requires its own permission.

---

## 23. UX and product surfaces

**Public web:** NLWC branded home, event listing, event landing page, audience/age information, accommodation/pricing preview, FAQs, help, login/register.

**User area:** Dashboard, My Profile, Dependants, Spouse/Family Links, My Events, Register for Someone, Invitations/Claims, My Payments/Receipts, My Accommodation, My QR Codes, Notifications, Privacy/Consents and Support.

**Registration wizard:** Event selection → beneficiary/participants → DOB-based rules → participation mode → accommodation and party/guardian details → appropriate event questionnaire → permissions → payment (if due) → review/confirm → credentials/next steps. Forms must show progress, save/resume and clear error/help copy.

**Admin area:** Organization configuration, Event editions, Forms & Rules, Registration desk, People/duplicate review, Camping review, Accommodation inventory, Pricing, Orders & Finance, Spouse pairing exceptions, QR/Attendance, Child Safeguarding & Incidents, Communication center, Reports, Staff access, Audit and Policy versions.

**Check-in device:** Large scan control, clear match/no-match/needs-review states, photo/identity verification if specifically approved, accessible manual lookup, current event/day selection, and child-collector verification with confirmation steps.

Avoid nested endless forms: offer role-relevant screens and progressive disclosure. Use English initially; localization support desirable. Accessible keyboard, readable mobile fonts, clear contrast and resilient poor-network experiences are must-haves.

---

## 24. Nonfunctional requirements and operational principles

- **Availability:** production uptime target to be agreed; high resilience during registration and event days.
- **Performance:** responsive on low-end Android phones and congested mobile networks; specific latency/load targets defined after sizing.
- **Scalability:** multiple concurrent events/editions, household signups, sponsor group orders, staff scanning, concurrent inventory booking.
- **Security:** MFA for admins, role-scoped access, encryption, secure secrets, signed webhooks, brute-force defense, secure logging and dependency updates.
- **Privacy:** minor/sensitive data safeguards, data minimization, consent history, retrieval rights and approved retention schedule.
- **Reliability:** transactional integrity for bookings/payments; idempotency; retry and reconciliation; consistent source-of-truth states.
- **Auditability:** who did what, when, against which event/participant, with prior/new values where suitable; immutable/high-assurance audit store for money and safeguarding changes.
- **Observability:** dashboards, alerts for payment mismatches, high check-in failures, booking over-allocation, missing child exceptions and integration outage.
- **Maintainability:** modular separation of identity, events, registration, pricing/inventory, orders/payments, attendance and consent.
- **Backup/DR:** tested backups, recovery procedures, and event-day contingency plans.
- **Testing:** unit, integration, security, end-to-end, concurrency and in-person gate drills before rollout.

Engineering should propose measurable SLOs, infrastructure, deployment pipeline, support/incident process, capacity estimate and total-cost model. Do not prescribe a programming language/framework merely because the legacy registration used Google Forms.

---

## 25. Phase-wise delivery proposal

### Phase 0 — Decisions and product foundation

- Verify confirmed policies for 2026, approve legal wording and event-specific configuration.
- Finalize unified people/guardian/spouse relationships and person-claim rules.
- Map roles, safeguarding SOPs, inventory categories/rates and payment partner.
- Produce wireframes, formal PRD, architecture, schema, security model and test plan.

### Phase 1 — Shared platform + Bible Bootcamp operational readiness

- NLWC organization, event catalogue, profiles, guardians/dependants and event registrations.
- Bible Bootcamp **21–23 Dec 2026**, including 17+ daily attendees.
- Child 3–16 daily check-in/out, authorized pickup, exception escalation, QR and attendance dashboard.
- Guardian permissions, notifications, safeguarding and any confirmed 2026 camp review process.
- Staff administration and operational reporting.

**Reason for priority:** Bible Bootcamp's calendar is confirmed and child attendance controls are safety-critical. Development must target usable event-day operations rather than shipping numerous unfinished features.

### Phase 2 — Singlz Summit accommodation and commerce

- Accommodation categories/units/bed slots, capacity and assignment.
- Married-only chalet policies, spouse link/verification and pairing.
- Configure for the current edition: 0–6 ₦0, hostel 7–12 ₦5,000, 13+ edition-specific full-rate tariffs, with other pending combinations blocked. **All amounts and price bands remain edition-configurable; do not hard-code or carry over automatically.**
- Payments, sponsored registrations, pay-now/pay-later, receipt/QR fulfillment, reconciliation and occupancy check-in/out.
- Deploy ahead of the **as-yet unconfirmed Singlz Summit dates**.

### Phase 3 — Couples Programme and other paid non-residential events

- Reuse person/couple/beneficiary linking and sponsored-order capabilities.
- Add paid, accommodation-disabled event forms, configurable per-person/per-couple fee basis and ordinary admission-only QR scans.
- Enable fee collection only when NLWC publishes an approved rate/eligibility/cancellation policy.
- Demonstrate that no camp/room/booking side effects occur when registration payment is made for a non-residential event.

### Phase 4 — Broader operational capability

- Advanced session management, transport/meals if needed, deeper analytics, group/corporate sponsors, waitlists, messaging automation, offline scanning and edition comparisons.

**Sequencing note:** The platform must be designed for all modules from day one; not every module must necessarily be built before the first event. Singlz dates and operational priorities may justify pulling selected Phase 2 capabilities earlier.

---

## 26. Open decisions, assumptions and explicit non-assumptions

| ID | Priority | Clarification to obtain from NLWC | Interim handling |
|---|---|---|---|
| D-01 | Critical | Confirm Bible Bootcamp 2026 venue, full daily schedule, camp arrival/departure and parent event dates | Only 21–23 Dec 2026 confirmed |
| D-02 | Critical | Bible Bootcamp 2026 camping ages, availability and fees | Legacy 10–16/free marked historical; policy disabled until confirmed |
| D-03 | Critical | Pickup time policy for non-camping children and treatment of 16-year-olds | Require explicit event day/age schedule; no copied conflicting 2025 noon/10 p.m. |
| D-04 | High | Are all 17+ allowed to attend all day sessions, and what is their camp/studio eligibility? | Daily attendance allowed; extra eligibility unconfirmed |
| D-05 | Critical | Dates, venues and registration opening of Singlz Summit edition | Configurable/TBD |
| D-06 | Critical | Current-edition accommodation rates and owner-approved versioned price configuration for each event edition | Cannot process paid checkout until rates approved and published for that edition |
| D-07 | Critical | What exactly is “chalet pairing” priced per? Must both spouses be physically present? | Model flexible pairing, defer final rule |
| D-08 | Critical | 7–12 rate in special dorm/chalet/non-hostel categories for current edition | Block unpriced combination; ₦5,000 applies only to current-edition hostel and may change next year |
| D-09 | High | For current Singlz edition, does 0–6 free apply to all categories and may children occupy them? | Explicit current-edition ₦0 rule where approved; category/capacity policy pending; future editions require renewed configuration |
| D-10 | High | Can married attendees use hostels/special dorms? Can children stay in chalets? | Do not assume; admin-controlled eligibility |
| D-11 | High | How to validate spouse link and married status without invasive collection? | Secure recipient verification and audited disputes |
| D-12 | High | Registration vs accommodation fee, taxes, transaction charges and refunds | Clearly itemize; no invented fees |
| D-13 | High | Deadline/hold duration for Pay Later; partial payments allowed? | Configurable pay-later, installment off by default |
| D-14 | Critical | Guardian authorization, minor independent account treatment, pickup SOP | Legal/safeguarding review required |
| D-15 | High | Authorized collector verification method, late-pickup escalation and lost-child process | Safe manual SOP before launch |
| D-16 | High | Preferred payment gateway, email, WhatsApp/SMS vendors and budget | Architecture vendor-neutral pending approval |
| D-17 | High | Requirement for identity/photo on QR passes and check-in | Minimal data; no photo required by default |
| D-18 | High | Data retention, legal counsel/privacy lead, staff medical access | Formal policy before production |
| D-19 | Medium | Studio/media/Parent Interactive Forum details for BBC 2026 | Modular and configurable; legacy content not hard-coded |
| D-20 | Medium | Is limited onsite guest registration allowed without an account? | Proposed assisted verified workflow |
| D-21 | High | When does beneficiary get an *active* QR if sponsor pays before beneficiary claims? | Send upon payment; active access subject to required eligibility/consent |
| D-22 | Medium | Should online participants receive event QR or a stream access token instead? | Different credential/entitlement by participation mode |
| D-23 | High | Who approves disputes, cancellations and reallocation of paid chalet pairing? | Event admin/finance coordinated audited workflow |
| D-24 | Critical | Couples Programme fee: price amount and billing basis (per couple or per participant)? | No charging until approved and published |
| D-25 | High | Couples Programme eligibility: married only, engaged or other qualifying pairs; any solo attendee? | Event form/rule configurable; no assumed restriction |
| D-26 | High | Couples Programme dates, venue, attendance mode and check-in policy | Not supplied; edition TBD |
| D-27 | Critical | What IP/source-code reuse rights exist for EGFM Registrar? | Code reuse prohibited until rights verified |
| D-28 | Critical | Have EGFM committed secrets been rotated and payment/auth vulnerabilities audited? | No production connection until remediation and penetration/security testing |
| D-29 | High | Which existing EGFM modules are technically reusable after code/migration review? | Reuse recommendation remains provisional, not an asserted implementation plan |
| D-30 | High | Whether historical EGFM source data is to be migrated | **No EGFM attendee data migration by default**; requires explicit separate approval and lawful basis |
| D-31 | Critical | Who drafts, approves and publishes rate changes and when may published prices take effect? | Require separately authorized edition-scoped rate approval and prospective versions; no unreviewed override |
| D-32 | High | Should admin start a new edition with an empty price plan or an unapproved copy of last year’s rates? | Offer reviewed draft copy only; never activate historical rates automatically |

Unresolved items must be visible in delivery backlog and admin configuration. **Do not silently fill them with defaults that charge money or compromise child safety.**

---

## 27. Completion/acceptance criteria for first production edition

A launch is operationally acceptable only when:

1. NLWC can publish an event edition without duplicating the whole application.
2. One person can participate in multiple editions with one stable identity and distinct registrations.
3. Guardians can register multiple children with distinct credentials.
4. Bible Bootcamp 2026 shows **21–23 December 2026**, and accepts **17+ daily attendees** while enforcing 3–16 child safeguarding controls.
5. Check-in staff can account for every non-camping child each day and perform auditable authorized checkout; camping children are not lost inside a single generic “checked in” flag.
6. Medical/welfare data and minor custody information are restricted to appropriate users.
7. For Singlz, single 13+ participants cannot purchase chalets; a married registrant can supply spouse name/phone and securely link/create/register/pay for spouse.
8. For the **current specified Singlz edition**, age 0–6 has an approved ₦0 rate, 7–12 hostel has an approved **₦5,000** rate, and 13+ pays the edition-configured full rate; all figures and bands are configurable for future editions and unknown category pricing does not become an arbitrary charge.
9. Sponsors can register another person with minimal necessary data, pay now or later, and beneficiary gets registration notification, verified payment confirmation and QR according to issuance rules.
10. Payment and inventory are race-safe and reconcile; no duplicated booking from repeated callbacks; occupancy is never overallocated.
11. Staff can handle exceptional cases — duplicate/claim disputes, consent pending, waitlist, refunded payment, missing child, unauthorized collector — without silent overrides.
12. NLWC administrators can reconcile attendance, camp occupancy, registrations and payments through event-specific dashboards.
13. Terms, privacy notice, guardian permissions and published price/cancellation policies receive proper legal/operations approval and are captured with versioned evidence.
14. Registration, payment, attendance and staff permissions are tested end-to-end, including high-load and low-connectivity event-day drills.
15. NLWC can configure a Couples Programme as **paid and no-camping**; a configured group/order covers two independently identifiable people with individual admission credentials and no accommodation assignment.
16. A signed-off EGFM code-reuse audit has addressed source permissions, committed-secret remediation, idempotent transactions, event-specific prices, historical retention and child attendance before any inherited components reach production.

---

## 28. Required output from the next agent

The receiving agent should deliver, in sequence, using **both this BRD and the accompanying EGFM gap analysis**:

1. **Decision log:** list confirmed decisions, assumptions and open questions, prioritizing critical blockers.
2. **Product Requirements Document (PRD):** module-by-module user stories, validated rules, field specifications, validation, error/exception states, and full acceptance criteria with references to this BRD.
3. **UX/UI specification:** information architecture, responsive screen inventory, page flows and prototypes for attendees/guardians/sponsors/administrators/finance/staff.
4. **Formal workflow diagrams:** self-registration, proxy registration, claimant activation, spouse pairing, minor guardian consent, accommodation purchase, pay later, booking and refund, event admission, child pickup, camper occupancy.
5. **Data architecture:** entity-relationship diagram, tables, constraints, unique indexes, relationship/claim model, versioned rates, inventory reservation design and audit strategy.
6. **Technical Specification:** API boundary/contracts, pricing/rule engine, payment webhook/idempotency, notification queues, QR verification, RBAC, safeguards for minors, storage, integrations, security and infrastructure.
7. **MVP and phased backlog:** dependency-aware workstreams, priorities, timeline relative to 21–23 Dec 2026, estimates, risks and role assignments.
8. **Test plan:** unit/integration/e2e, safeguarding, eligibility and price boundary tests, inventory concurrency, repeated payment callbacks, security/authorization and event-day rehearsal.
9. **EGFM reuse audit and gap closure:** compare the actual four EGFM repositories against this BRD and `NLWC_EGFM_Registrar_Gap_Analysis_v1.0.md`. Produce code-level reuse decisions, data migration and secret rotation plans; do not migrate EGFM attendee records by default.
9. **Operations playbooks:** event-day check-in/out, lost child or collector dispute, medical emergency, communications outage, payment exception, accommodation reallocation and post-event closeout.
10. **Traceability matrix:** every major business rule here links to design, implementation ticket and at least one acceptance test.

**Do not deliver only attractive registration forms. This is an operational system spanning identity, families, events, bookings, payments and safeguarding.**

---

## Appendix A — Business-rule quick reference

| Topic | Definitive instruction |
|---|---|
| Platform | One NLWC multi-event product; Bible Bootcamp, Singlz Summit and Couples Programme are distinct event types/templates |
| Bible Bootcamp 2026 | 21–23 December 2026 |
| Bible Bootcamp children | Ages 3–16; daily attendance and authorized checkout for physical children, particularly non-campers |
| Bible Bootcamp 17+ | Allowed at daily programme; separate child-only camp eligibility not implied |
| Singlz participation | Online / Walk-in / Camping |
| Singlz single | 13+; full approved accommodation tariff; chalets not offered |
| Singlz married | Spouse name/phone required; spouse link/create-and-pay supported |
| Couples Programme | Paid registration with no camping; couple grouping and fee unit require policy approval |
| Chalet | Married-only access; pairing and occupancy details need approved rate policy |
| Special dorms/hostels | Singles eligible; other categories' access configurable |
| Dependants | 0–12 linked to adult; DOB determines band |
| Dependant 0–6 | Free accommodation price for current Singlz edition (explicitly configured ₦0; future years configurable; subject to legitimate capacity/category policy) |
| Dependant 7–12 hostel | ₦5,000 confirmed for current Singlz edition only; price configurable next year |
| Dependant 7–12 other accommodation | PENDING — cannot guess a rate |
| Participant 13+ | Independent person/event registration; full **edition-configured** accommodation price, with guardian protection if minor |
| Price classification | Singlz: no employed/unemployed differentiation within edition; amounts may change year to year |
| Proxy registration | Invite or direct registration; direct allows Pay Now / Pay Later |
| Notifications | Beneficiary registration notice on creation; payment confirmation and credential after verified payment as eligible |
| Consent | Platform/privacy + event terms + separate age/guardian/medical/media choices as applicable |
| Distinct tracking | Day attendance, minor checkout, camp occupancy, booking, payment and registration are different records |

## Appendix B — Source provenance and confidence

- **Confirmed current business decisions:** user-provided updated multi-event scope, participation categories, Bible Bootcamp 2026 dates, age and daily-attendance rules, Singlz Summit current-edition accommodation and dependant prices/eligibility, **all fees/rates fully configurable per event edition (the ₦5,000 rate is not a future-year constant)**, spouse pairing, proxy registration and payment/notification aspirations, and Couples Programme paid registration without camping.
- **Confirmed historical form structure:** images of the six-section 2025 Google Forms editor, previously documented in the Bible Bootcamp BRD v1.0. The exact original answer-triggered form branching was not verified.
- **Documented EGFM Registrar baseline:** the attached original *Registrar System – Business Requirements Document (EGFM)* (sections 1–15) states current EGFM behavior, technical stack, specific implementation issues and constraints. Its descriptions have not been independently verified against source code.
- **Crosswalk:** *NLWC EGFM Registrar Gap Analysis v1.0* provides `G-01`–`G-40` reuse/gap classifications and audit gates. The implementation agent should keep this crosswalk in PRD traceability.
- **Couples Programme:** confirmed by requester as registration with payment and **no camping**. Exact fee, couple eligibility and schedule are not provided and remain open decisions.
- **Proposed enhancements:** unified identity/claims, relationship verification, event rules engine, separate booking/payment/attendance life cycles, safekeeping controls, notifications, reporting and phased rollout.
- **Pending decisions:** clearly marked in Section 24 and individual business rules; legal documents require professional review.

## Appendix C — NLWC-to-EGFM design and acceptance traceability

| NLWC capability | EGFM documented baseline | NLWC delta | New BRD IDs / gap IDs |
|---|---|---|---|
| Shared event catalog | Existing Event status/EventId | Event templates and independent forms/fees; paid non-lodging support | ORG-01, EVT-01–03; G-01, G-10 |
| DOB/person/family | Attendee + FamilyCode/dependants | Event-age-reference logic; independent 13+ participants; guardian, spouse verification | PRO-01–05; G-03–06 |
| Sponsored/proxy enrollments | Family payment and admin bulk-create | Third-party claims, payer vs beneficiary, Pay Now/Later and notifications | REG-01–03, COM-01–02; G-07–09 |
| Bible Bootcamp operations | QR gate check-in + fixed children's sessions | Daily 21–23 Dec child custody and independent overnight occupancy | BB-01–06; G-28–32 |
| Singlz accommodation | Existing lodge types/chalet pairing | Married chalet; 13+ single rate; dependant price rules; no work fee | SS-01–08; G-14–20 |
| Couples Programme | No dedicated couple paid admission | Paid/no lodging, two people in a group, configured fee unit | CP-01–08; G-13 |
| Payment/booking | Paystack, cash, ballot and holds | Server-validated rate, atomic fulfill, refunds/reconciliation | PAY-01–03, INV-01–03; G-21–26 |
| Audit/security/history | Per-endpoint privileges, archived copies | Secrets rotation, MFA, non-destructive history, scope and least privilege | SEC-01–02, OPS-01–02; G-35, G-37–40 |

**Technical quality gate:** The original EGFM BRD itself acknowledges committed live-looking secrets, insufficient transaction boundaries, hard-coded event behavior and operational workers with single-process assumptions. The receiving agent shall track these as **blocking technical debt**, not harmless differences. If the actual repositories differ from the BRD, document the evidence and revise the adaptation plan; never alter confirmed NLWC policies to fit old code.

---

**End of BRD v3.1.**


---

## Appendix D — Change log

| Version | Change | Status |
|---|---|---|
| 3.1 | Clarified that **all pricing is configurable per event edition**, including free/₦0 rates, the current-edition Singlz ₦5,000 dependant hostel example, other accommodation rates, registration charges, spouse/chalet pairing, Couples Programme fees, discounts and approved add-ons. Added versioned rate approvals, price snapshots, cross-edition integrity and pricing acceptance tests. | Confirmed business direction supplied 31 October 2026 |
| 3.0 | Superseding EGFM-adapted unified NLWC BRD with Bible Bootcamp, Singlz Summit, Couples Programme and church-wide shared modules. | Previous baseline |
