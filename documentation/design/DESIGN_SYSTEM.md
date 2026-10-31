# NLWC EMS Design System v1.0

**Applies to:** NLWC Unified Event Management System (BRD v3.1)
**Surfaces:** Public site · Attendee portal · Admin console · Check-in PWA
**Files:** `tokens.css` (source of truth) · `tokens.json` (mirror) · `index.html` (living style guide, open in a browser) · `assets/` (logos)

Status labels follow the BRD: **CONFIRMED** (taken from NLWC assets/requirements), **DERIVED** (measured from the sites/logo), **PROPOSED** (my recommendation, needs NLWC sign-off).

---

## 1. Inputs and what they told me

| Source | Finding | Used for |
|---|---|---|
| Logo (3 variants) | Tree-on-mountain-above-river mark. River gradient runs crimson `#B3260E` → flame `#E8541A` → amber `#F28216`. Wordmark is a Roman-capital serif in `~#D03A16`. Black variant keeps the river in colour. | Brand ramp, gradient, logo rules (DERIVED) |
| nlwc.church | WordPress/Elementor. **Plus Jakarta Sans**, accent `#FF7C18`, near-black `#060606` sections, grey `#8F8F8F`/`#EFEFEF` text, white logo on dark header. Warm, devotional, scripture-led tone. | Type family, dark header, accent, tone (CONFIRMED from markup) |
| tyrannus.nlwc.church | Next.js + Chakra UI, **Poppins**, amber `#EE9938` with brown `#7B4C16`/`#BA7F3C`. Course cards, testimonial quotes, newsletter, WhatsApp float. Footer already links Singlz' Summit and Couples' Garden. | Confirms amber family and card patterns; I did not adopt Poppins or Chakra defaults (see §3) |
| BRD v3.1 + gap analysis + handoff | Money, minors and capacity. Status must be separate per concern, ₦0 ≠ unconfigured, QR ≠ custody, accessible on low-end phones at gates, English first, localisation desirable. | Everything in §6–§9 |

**Honest limits:** the websites expose no design tokens, so colours/fonts are read from page source, not from a brand guide. If NLWC has a brand guideline PDF it overrides §2. The event accents in §2.4 and the display font in §3 are proposals.

## 2. Colour

Full values in `tokens.css`. Roles, not hexes, are what code should use.

### 2.1 Brand ramp (ember)
`50 #FDEBE3 · 100 #FBD3C0 · 300 #F4894F · 400 #FF7C18 · 500 #E8651A · 600 #C2300F · 700 #B3260E · 800 #8E1B0A · 900 #5E1206`

- **Primary action = ember-600** (white text 5.64:1; hover ember-700 6.56:1).
- **ember-400 `#FF7C18` is the website accent and fails with white text (2.58:1).** Use it for rules, icons, highlights and as a fill with *dark ink* text (7.52:1). Never as a white-text button.
- **Flame gradient** (logo river) is reserved for: header underline, hero base rule, progress completion, empty-state illustration. It is never a button or text fill.

### 2.2 Neutrals (warm stone)
Warm greys so UI doesn't fight the ember. Ink `#17120F` on `#FAF7F4` page. Secondary text minimum is stone-500 `#6B635C` (5.52:1 on page). Do not use stone-400 for text on light surfaces.

### 2.3 Semantic
| Role | Light fg / bg | Dark fg / bg | Meaning in this product |
|---|---|---|---|
| success | `#14532D` / `#DCF5E6` | `#4ADE80` / `#0F2A1B` | Paid, checked in, linked/verified, credential active |
| warning | `#7A4A00` / `#FDF0D0` | `#FBBF24` / `#2E2208` | Pending claim, hold expiring, rate pending, restricted credential, late pickup |
| danger | `#8E1B12` / `#FBE4EA` | `#FF8A9E` / `#321019` | Collector mismatch, failed payment, revoked, do-not-release |
| info | `#154A85` / `#E3EEFB` | `#7DB4F5` / `#0E2036` | Neutral notices, explanations of why a rule applied |

**Danger is rose-crimson `#B0123C`, deliberately not the brand red-orange.** The brand is itself red/orange, so an error styled in brand red reads as "on brand", not "stop". Pair every semantic colour with an icon and a word (WCAG 1.4.1).

### 2.4 Event accents (PROPOSED)
Bible Bootcamp = ember (church core) · Singlz Summit = plum `#7A2E5B` · Couples Retreat = deep teal `#1F6F78`.
Applied only through `--event-accent`: event chip, hero base, selected-state outline on that event's pages. Never on buttons (primary stays ember so "the main action" is learnable across events), never on status. The `data-event` attribute swaps it, so adding a fourth event needs no new component.

### 2.5 Contrast (measured)
All text pairs ≥ 4.5:1, large text/UI ≥ 3:1. Spot checks: primary button 5.64 · body on page 15+ · muted text 5.52 · danger-fg on danger-bg 7.6 · device-mode ok-fg on dark 11.1.

## 3. Typography

| Token | Family | Use |
|---|---|---|
| `--font-ui` | **Plus Jakarta Sans** 400/500/600/700 | All UI. Matches nlwc.church; this is the continuity link with the main site. |
| `--font-display` | **Marcellus** (PROPOSED) | Public event hero titles only, uppercase with `.04em` tracking. Echoes the logo's Roman-capital wordmark. Never in forms, tables or admin. |
| `--font-mono` | JetBrains Mono | Registration refs, credential IDs, rate-plan versions, audit IDs. |

Why not Poppins (Tyrannus)? Two geometric sans families would split the brand; Jakarta is already on the church's primary site, and it renders well on low-end Android. Tyrannus can adopt this later with no loss.

Scale (rem): 0.75 · 0.875 · **1** · 1.125 · 1.25 · 1.5 · 1.875 · 2.25 · 3. Body 16px minimum on every surface (BRD: readable mobile fonts). Line height 1.55 body, 1.2 headings.
**Money, counts and times use tabular numerals** (`font-variant-numeric: tabular-nums`) so columns align and totals don't jitter.
Currency format: `₦5,000` (symbol, thousands comma, no decimals unless kobo is non-zero). Dates: `Mon 21 Dec 2026`, times 24h `15:42` in `Africa/Lagos`, with the zone stated once per page.

## 4. Space, shape, elevation, motion
- 4px grid: 4 · 8 · 12 · 16 · 20 · 24 · 32 · 40 · 48 · 64.
- Radius: 6 (chips, inputs inside dense tables) · 10 (buttons, inputs) · 16 (cards, panels) · pill (badges).
- Elevation is subtle: three shadows tinted with ink, not black. Prefer border + surface contrast to shadow.
- Motion: 120ms/200ms, ease-out. Motion never conveys state alone. `prefers-reduced-motion` is honoured globally in `tokens.css`. No decorative animation on the check-in device.

## 5. Theming and surfaces

| Surface | Theme | Target size | Density | Notes |
|---|---|---|---|---|
| Public site | light, dark follows OS | 44px | Roomy | Dark header with white logo and flame underline (mirrors nlwc.church). |
| Attendee portal | light, dark follows OS | 44px | Comfortable | Mobile-first; wizard with save/resume. |
| Admin console | light, dark follows OS | 44px (36 in dense tables with 44 hit-area) | Dense | Left nav + event switcher; event scope always visible. |
| Check-in PWA | **always dark** (`data-surface="device"`) | **56px** | Spacious | High-contrast for sun, gloves and crowds; mode always visible; sound/haptic on result. |

Dark mode swaps the primary to `#FF9A4D` with dark text, since ember-600 is too heavy on near-black.

## 6. Components
Built on semantic tokens only, never hex. States required for every interactive component: default, hover, focus-visible (3px blue ring `--focus-ring`, intentionally not brand-coloured so it stays visible on ember), active, disabled, loading, error.

**Foundation set:** Button (primary / brand / secondary / ghost / danger; one primary or brand per view; **brand** = ember-400 `#FF7C18` fill with ink text `#0F0D0C`, 7.5:1, for dark headers and public event heroes where it matches nlwc.church; disabled must explain why) · Input, Select, DOB picker, Phone input (with explicit "No phone" path per BRD §6.3) · Checkbox/Radio (never pre-selected for consent) · Badge/Status chip · Alert · Card · Table (sortable, sticky header, row actions, right-aligned numerals) · Stepper · Tabs · Modal/Sheet · Toast · Empty state · Skeleton · Pagination · Event switcher · Scan result panel.

**Form rules**
- Label above field, always visible (no placeholder-as-label). Hint below, error replaces hint and begins with an icon.
- Show *why* a derived value applies: "Age 9 at event start → band 7–12" (BRD §6.2).
- Multi-select where multiple answers are valid (health conditions; "None" is exclusive). Radio only for mutually exclusive choices. This fixes the legacy Google Form defects listed in BRD §8.2.
- Consent choices are two explicit buttons/radios, default **unanswered**, never a single ticked box.
- Save-draft is always visible on multi-step forms.

## 7. Domain patterns (the part that is specific to this product)

### 7.1 Price line, "free" is not "unpriced" (PRICE-03)
Three distinct renderings, never interchangeable:
| State | Rendering | Checkout |
|---|---|---|
| Approved amount | `₦5,000` | Enabled |
| Approved ₦0 rule | `₦0 · Free` in success colour | Enabled; occupancy still counted |
| No approved rate | `⏱ Price pending approval` in warning colour | **Blocked** with an explanation and an alternative |

A missing rate must never render as ₦0, a dash or blank. Quotes show rate-plan version (mono, caption) and a visible validity countdown. Totals are computed server-side; the UI never edits an amount.

### 7.2 Separate status axes
A person's event record shows **four independent badges**: Registration · Payment · Booking · Occupancy. A fifth line shows Credential state. Never collapse into one "Confirmed" badge. Example: *Pending claim* + *Paid by Kelechi* + *Confirmed, unit TBD* + *Expected* + *Credential restricted*.

Badge vocabulary comes straight from the BRD state machines (§21):
- Registration: Draft · Pending claim · Submitted · Confirmed · Needs action · Cancelled · Expired
- Payment: Not required · Unpaid · Pending · Paid · Failed · Refund pending · Refunded · Chargeback
- Booking: Quoted · Held · Pending payment · Confirmed · Allocated · Occupied · Checked out · Waitlisted · No-show
- Credential: Pending · Active · Restricted · Suspended · Revoked · Expired
- Day attendance: Expected · Checked in · Checked out · Late pickup · Exception

### 7.3 Registrant / payer / beneficiary
Any order table has separate **Beneficiary** and **Payer** columns. Sponsors see only a minimal "completed/pending" outcome for beneficiaries, never medical or profile data. A provisional person shows a *Pending claim* badge until claimed.

### 7.4 Consent card
Title names the person and purpose; versioned text id in mono; neutral until answered; declining media never blocks registration (BRD §16).

### 7.5 Day-attendance strip
One card per event day (21, 22, 23 Dec) with its own status. Checking out on the 21st is visually unrelated to the 22nd. Camp occupancy is a separate strip beneath, so a camper's daily roll-call and overnight stay never merge.

### 7.6 Capacity and holds
Inventory meters show *sold · held · available* as three segments (never one number). Holds show a countdown and an explicit "we can't reserve longer" note, no promise of an indefinite bed (BRD §12.2).

### 7.7 Sensitive-data affordances
Medical/welfare fields get a lock icon and "Restricted: visible to Medical/Welfare" label. Any reveal is a deliberate click that is logged, and the UI says so ("This view is recorded"). Broad dashboards and exports never show these fields.

### 7.8 Audit and override
Every override requires a reason field and shows the audit line (who, when, role). No "force success" button anywhere near custody (BRD §15.2); the escalation path is the primary action when a check fails.

## 8. Check-in device spec

- Mode bar fixed at top: **Entry · Check-in · Check-out · Camp · Session · Handover**. The active mode is shown in the header and on every result, because each mode runs different checks.
- Result panels fill the width and use colour **+ icon + word** (✓ Checked in / ⚠ Verify collector / ✕ Do not release). Optional sound/haptic differs per result.
- Child check-out is a **two-step**: scan → verify collector against the authorised list → confirm. The panel lists authorised adults by name and relationship. A QR scan alone never releases a child; "Not on list → supervisor" is always as large as the confirm button.
- Show minimum data: name, age, day, safety flag, authorised collectors. No DOB, medical detail, payment or address.
- Connectivity pill always visible. Offline queue is allowed for low-risk entry and presence scans only; the custody actions **disable** offline with an explanation (BRD §15.4).
- Manual lookup is a first-class button, not hidden. Duplicate scan shows who/when/where of the first scan.
- 56px targets, 17px base size, no hover-dependent UI, no gestures required.

## 9. Content and tone
Voice from nlwc.church: warm, welcoming, plainly spoken, faith-aware but not preachy inside transactional flows. Rules:
- Say what happened and what to do next. "We couldn't confirm your payment. Your bed is held for 14 minutes."
- Be exact about money and minors; no softening. No exclamation marks in payment, safeguarding or error copy.
- Explain eligibility outcomes ("Chalets are available to married participants. Add your spouse's details to continue.") without exposing another person's data.
- Do not imply a sponsor controls a beneficiary's account; do not imply payment grants entry (BRD §11.3).
- Names of events as NLWC writes them: Bible Bootcamp, Singlz Summit, Couples Retreat.

## 10. Accessibility (must-have per UX-01)
WCAG 2.2 AA minimum.
- Contrast per §2.5; focus ring 3px, ≥3:1 against adjacent colours.
- Colour never alone; every status has icon + text.
- Targets ≥ 44px (56px on device); spacing so adjacent targets don't mis-tap.
- Full keyboard operation; logical order; visible focus; skip link on public/portal.
- Form errors are announced (`aria-describedby`, `role="alert"` on submit failure) and linked to fields.
- Respect `prefers-reduced-motion` and `prefers-color-scheme`.
- Low-end Android: no layout shift from webfonts (`font-display: swap`, system fallback stack), images lazy-loaded, ≤1 web-font family on critical path for check-in (load Jakarta 400/600/700 only).
- Text must reflow at 200% zoom; avoid fixed-height containers for text.
- Localisation-ready: no text in images, allow 30% expansion, dates/currency through Intl.

## 11. Logo usage
- **Coloured:** light surfaces. **White:** dark headers, the device, email footers on dark. **Black:** receipts, printed tickets, QR passes, monochrome print.
- Clear space = height of the "T" in THE on all sides. Minimum width 120px (screen) / 30mm (print).
- Do not recolour, stretch, add effects, or place the coloured logo on ember or photography without a solid overlay.
- Provided files in `assets/` are cropped copies at 860×380 of the originals in `documentation/artifact/` (originals are 4501² with large padding). Request SVG from the designer for production; the PNGs are for reference and prototypes.
- The favicon/app icon should be the mark alone (tree + mountain + river), not the wordmark. This needs a mark-only export (PROPOSED).

## 12. Implementation guidance
- Consume `tokens.css` directly or generate platform tokens from `tokens.json`. Components reference **role tokens** (`--surface`, `--text`, `--primary`, `--ok-bg`), never the raw ramps, so dark/device themes work for free.
- The BRD notes the EGFM stack (Next.js portal, Vite/React admin, React PWA) as a candidate, not a decision. These tokens are framework-neutral CSS custom properties and will work with Tailwind (map in `theme.extend`), Ant Design (ConfigProvider token mapping to `--primary`, radius and font) or Chakra. Do not adopt a library's default blue.
- Keep one component library across the three React clients so status badges, price lines and consent cards cannot drift. These are the components where a visual inconsistency becomes a safeguarding or money bug.
- Every status badge component should take a typed enum (the BRD state machines), not a free string and a colour.
- Add a visual-regression test for the §7 patterns, plus automated axe checks.

## 13. Open design decisions (need NLWC input)
| # | Decision | Default until answered |
|---|---|---|
| DS-1 | Is there an official brand guide or Pantone/CMYK/RGB for the logo colours? | Values here are sampled from the logo and site |
| DS-2 | Approve event accents (plum, teal) or provide official event branding | Proposed accents in §2.4 |
| DS-3 | Approve Marcellus for public event heroes, or keep sans only | Sans only if rejected; nothing else changes |
| DS-4 | SVG logo and mark-only icon from the designer | PNG crops used for prototypes |
| DS-5 | Will Tyrannus move onto this system (Poppins → Jakarta, Chakra theme map)? | Out of scope for v1; tokens compatible |
| DS-6 | Is a photo/ID display wanted on the check-in result? (BRD D-17) | None; minimum data only |
| DS-7 | Languages beyond English (Yoruba, Igbo, Hausa, Pidgin)? | English only; layouts allow expansion |

## 14. Not yet covered (next iteration)
Screen-level wireframes for the registration wizard, admin pricing matrix and rate-plan approval flow, capacity meter and manifest views, and print templates for tickets and receipts. These should be designed after the PRD fixes screen inventory, using the components and patterns above.
