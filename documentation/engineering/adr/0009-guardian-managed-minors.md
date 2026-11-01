# ADR-0009: Minors are guardian-managed up to 17; a teen's own login comes later
Status: Accepted (provisional) · 1 Nov 2026

**Context:** the BRD wants an independent person, registration and price for everyone 13 and over (PRO-04), but says a 13 to 17-year-old is still a minor who needs guardian consent and controls (section 6.3), and Scenario B has a guardian registering a 14-year-old for camp. Bootcamp camp is open to ages 10 to 16 (Q4), so a 0 to 12 limit on dependants left every camp-age teen unable to register.

**Decision:**
- A guardian can add a minor as a dependant at any age from 0 to 17. The person has no account; `GuardianAuthorization` records the guardian's authority.
- A dependant is still an independent person: their own registration, reference, age band, price and consents. Being a dependant does not merge them with the guardian.
- For a minor the guardian is the registrant and answers the terms, guardian-authorisation and medical-emergency consents. Media permission stays optional and a No never blocks.
- Age is judged on the edition's reference date. Someone who is 18 or over on that date must register themself (`adult_registers_themself`); a guardian cannot consent for them.
- A person under 18 with their own account still cannot register themself (`guardian_required`).

**Not built yet:** a teen claiming their own login, linked to the guardian who added them, with the guardian keeping consent and pick-up controls. It uses the existing `AccountClaim` table and should wait for real email (Q10). Until then a teen has no self-service access.

**Consequences:** the 12 to 13 and 17 to 18 transitions no longer move a person between kinds of record, only between rules. A child who turns 18 stays linked to their guardian's history but registers for future editions on their own.
