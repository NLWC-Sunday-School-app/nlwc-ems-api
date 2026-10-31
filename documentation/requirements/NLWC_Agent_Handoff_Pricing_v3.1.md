# NLWC Agent Handoff — Pricing Configuration Amendment

**Authoritative BRD:** `NLWC_Unified_Event_Management_System_BRD_v3.1.md` (supersedes v3.0)
**Companion reuse/gap analysis:** `NLWC_EGFM_Reuse_and_Gap_Analysis_v1.1.md`
**Historical system baseline:** `Registrar System – Business Requirements Document (EGFM).md`

## New confirmed business instruction

**ALL prices must be configurable by authorized administrators for every individual event edition.** Do not hard-code any price, fee, discount, zero/free rule, child rate, accommodation rate, spouse/chalet pairing fee, or paid Couples Programme registration amount. Annual/event editions can change amounts independently without deployment.

The **₦5,000** 7–12 hostel dependant rate and **₦0** 0–6 free rate are **examples of the current Singlz pricing supplied in 2026**, not permanent rates. Other 7–12 accommodation-category amounts remain unapproved. Unconfigured is not free.

## Implementation guardrails

1. Event template is separate from annual edition; each edition has independent rate plans and versions.
2. Differentiate draft, approved, published and retired prices with audit, effective times and authorized staff control.
3. Server is pricing authority; browser cannot submit arbitrary amounts.
4. Preserve quote/order/payment line-item price snapshots; changing next-year rates does not alter prior purchases or reports.
5. Allow reviewing/copying last-year rates into a **draft only**; never auto-publish.
6. Future-edition rates may be freely configured, subject to business approval.
7. Verify pricing tests and cross-edition invariants in BRD v3.1 Section 13.2 and functional requirements PRICE-01–04.

Follow all other requirements and open-decision gates of the BRD. The EGFM document is a reported source-system baseline, not authority to import EGFM prices, people data, credentials or business rules.
