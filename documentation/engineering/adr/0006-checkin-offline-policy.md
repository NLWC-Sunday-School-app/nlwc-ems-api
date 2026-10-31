# ADR-0006: Check-in offline policy
Status: Accepted · 2026-10-31

**May queue offline (IndexedDB, device timestamp, conflict report on sync):** `EVENT_ENTRY`, `DAY_CHECKIN`, `CAMP_ARRIVAL`.
**Require live connection:** `DAY_CHECKOUT`, `MINOR_HANDOVER`. Offline, the screen says so and points to the printed manual SOP.
**Never:** a child checkout succeeds on QR alone. It needs a registered `collector_id` and officer `identity_verified: true`, otherwise `ESCALATE`.
