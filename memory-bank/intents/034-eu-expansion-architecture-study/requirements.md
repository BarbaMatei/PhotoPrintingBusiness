---
intent: 034-eu-expansion-architecture-study
phase: inception
status: inception-complete
created: 2026-06-05T12:57:50Z
updated: 2026-06-05T13:14:12Z
---

# Requirements: EU Expansion — Architecture Study (Research-Only Intent)

> **Special intent.** This is a **research-only** intent (roadmap Phase 5 preparation).
> Its bolts are **spike-bolts** that write **no production code** — they produce
> knowledge: per-track findings, an options paper, an owner decision recorded as an
> ADR, and new implementation brief(s) fed back into the inception agent.
>
> **The loop:** `research brief → inception → research intent (spike bolts) → findings
> + options paper → owner decides → ADR → implementation brief(s) → inception →
> implementation intent(s)`.
>
> Source feed: `docs/planning/eu-expansion-research-brief-2026-06-05.md`

## Intent Overview

The business ambition is to enter **all EU country markets**, not just Romania. This
intent answers the questions that block sensible implementation work — using a
**multi-agent research method** — and turns the owner's decision into actionable
implementation requirements. It writes no production code.

**Owner decisions taken at Checkpoint 1 (2026-06-05)** materially scope the study:

1. **Target markets — compare both tiers.** Produce numbers for a neighbors-first tier
   (HU/BG, possibly PL) **and** a big-market tier (DE/FR/IT/ES), side by side.
2. **Brand — one brand EU-wide.** Site-architecture options are evaluated under a single
   brand; per-country ccTLD brand sites become the documented rejected-by-default option.
3. **Fulfillment — ship everything from Romania.** This **settles the dominant question
   up front**: the software architecture stays **single fulfillment origin**. T1 shifts
   from a 3-way model comparison to *validating* RO-centric cross-border economics and
   finding where it breaks; local print partners are researched only as a costed
   **fallback/sensitivity**, not a primary path.
4. **Currency — local currencies.** Multi-currency (presentment vs settlement) is now a
   **first-class requirement** rippling through T5/T6 and the order/invoice model (T7).

**Consequence for synthesis:** because fulfillment is fixed, the options paper's 2–3
coherent bundles vary primarily along the **site-architecture + i18n** axes (all sharing
the RO-ship / one-brand / multi-currency baseline). One bundle should stress-test the
partner-fallback sensitivity so that path is not foreclosed.

## Business Goals

| Goal | Success Metric | Priority |
|------|----------------|----------|
| Validate RO-centric EU fulfillment on evidence | Real per-parcel cost/time numbers for RO→HU/BG and RO→DE/FR/IT/ES/PL; Sameday coverage limits identified; partner fallback costed | Must |
| Decide site/URL architecture (one brand) on evidence | 2–3 coherent candidate architectures costed (one-off + recurring), tied to intent 033's env triad | Must |
| Choose i18n approach (frontend + backend) | Angular 21 i18n + .NET localization options evaluated against the chosen site architecture | Must |
| Make multi-currency decisions concrete | Presentment vs settlement model chosen; order/invoice model impact sized | Must |
| Size the localization + currency retrofit honestly | T7 seam audit: file/occurrence counts per area + named top-10 heaviest spots | Must |
| Produce owner decision + ADR + implementation brief(s) | ADR recorded only after explicit owner decision; D4 brief(s) ready to hand to inception | Must |

## Scope

### In Scope (knowledge only)
- Research tracks T1–T7 (see FRs), executed via multi-agent fan-out + adversarial verification + synthesis.
- An options paper composing 2–3 **coherent bundles** (site architecture + i18n that fit together on the fixed RO-ship / one-brand / multi-currency baseline), each costed with explicit trade-offs.
- A ⛔ human owner-decision checkpoint and the resulting ADR.
- Implementation brief(s) (D4) authored as inception feeds for the real work.

### Out of Scope (hard constraints)
- ❌ **No production code.** Zero production-code changes anywhere in this intent.
- ❌ **No translations.** Phase 5 prepares architecture; it does not translate content.
- ❌ **No deployment.** Deployment is roadmap Phase 6, deliberately last — this intent must not presuppose or pressure it.
- ❌ **No re-litigating settled owner decisions.** Fulfillment (RO-ship), brand (one EU-wide), and multi-currency are decided; research validates and sizes them, it does not reopen them (partner fallback is a sensitivity only).
- Throwaway prototypes (e.g. a ~20-line Angular i18n build experiment) are allowed per spike rules but are archived or deleted, **never merged**.

---

## Functional Requirements

### FR-1: Multi-agent research method
- **Description**: The research MUST be executed as a fan-out of parallel, clean-context research agents — one per track — followed by a dedicated synthesis stage. High-stakes regulatory/tax/legal claims get a second, independent agent attempting to refute or confirm them before they enter the options paper.
- **Acceptance Criteria**: Each track (T1–T7) produces its own findings doc; every regulatory/tax/legal claim carries a source, a date, and an adversarial-verification verdict; synthesis is its own stage consuming all track findings.
- **Priority**: Must
- **Related Stories**: (T1–T7 stories + synthesis story)

### FR-2: T1 — Fulfillment & logistics findings *(RO-ship validation)*
- **Description**: Validate RO-centric cross-border fulfillment. Produce realistic carrier cost/time for a photo-print parcel RO→DE/FR/IT/ES/PL/HU/BG; identify where Sameday's coverage ends and what carriers cover the rest. Research local print-partner networks as a costed **fallback/sensitivity** only. Competitive scan of how EU photo-print players structure fulfillment.
- **Acceptance Criteria**: `docs/analysis/eu-expansion/track-1-fulfillment.md` with **actual numbers** (cost/time per parcel per corridor) for both market tiers — not hand-waving; Sameday coverage boundary stated; partner fallback costed and flagged with the market-size threshold at which it should be revisited.
- **Priority**: Must

### FR-3: T2 — Site & URL architecture findings *(one brand)*
- **Description**: Under a single EU-wide brand, compare multi-locale single site vs subdomains vs path prefixes (`/de/`) — and document per-country ccTLD sites as the rejected-by-default option. Cover SEO consequences (hreflang, domain-authority splitting), legal-page/content management per jurisdiction, and hosting/deployment topology per option **referencing intent 033's Staging/Production triad** (state how each option multiplies the environment count, or doesn't).
- **Acceptance Criteria**: `docs/analysis/eu-expansion/track-2-site-architecture.md`; each option states its env-triad multiplier and SEO trade-offs; ccTLD-per-country documented as rejected-by-default with reasons.
- **Priority**: Must

### FR-4: T3 — Frontend internationalization findings
- **Description**: Angular's built-in i18n (compile-time, one build per locale) vs runtime libraries (Transloco and peers) **for Angular 21 specifically** — maturity, bundle impact, developer workflow, and how each interacts with the T2 options. Currency/number/date formatting (multi-currency per owner decision). RTL not required.
- **Acceptance Criteria**: `docs/analysis/eu-expansion/track-3-frontend-i18n.md`; recommendation per T2 option; bundle-impact and build-strategy implications stated; a throwaway Angular 21 i18n build experiment may be cited (archived/deleted, never merged).
- **Priority**: Must

### FR-5: T4 — Backend localization findings
- **Description**: .NET resource-based localization for validation/error messages (ProblemDetails), transactional emails (existing Razor templates), invoice PDFs, enum/display strings. Culture resolution per request (header / user preference / site-of-origin) and how it flows into background jobs that send emails *later*.
- **Acceptance Criteria**: `docs/analysis/eu-expansion/track-4-backend-localization.md`; **explicitly flags the deferred-culture trap** — culture must be stored on the job/entity, not read from ambient request context at send time.
- **Priority**: Must

### FR-6: T5 — Tax, invoicing & compliance findings *(multi-currency, both tiers)*
- **Description**: EU OSS (One-Stop-Shop) VAT for B2C distance selling — registration, thresholds, per-country VAT rate application, reporting cadence — concretely for this codebase's `VatCalculator` (bolt 038, currently RO-only). E-invoicing per country: what (if anything) is mandated for B2C sellers in the target markets in 2026 (ANAF e-Factura / bolt 039 is RO-only). Multi-currency (EUR + PLN/HUF/CZK/BGN): pricing, display, settlement. Per-country consumer-law deltas affecting checkout copy (withdrawal-rights wording) — without a legal rabbit hole.
- **Acceptance Criteria**: `docs/analysis/eu-expansion/track-5-tax-compliance.md`; OSS rules current to 2026 (reject pre-2021/pre-OSS sources); every tax claim sourced + dated + adversarially verified; explicit statement of what changes for `VatCalculator` for both market tiers.
- **Priority**: Must

### FR-7: T6 — Payments & checkout findings *(multi-currency)*
- **Description**: Stripe's EU coverage incl. local methods (iDEAL, Bancontact, Przelewy24, etc.) and what enabling them implies for the existing Stripe integration (bolt 016). EuPlatesc: keep RO-only or retire outside RO. Multi-currency: presentment vs settlement currency and its impact on order/invoice models.
- **Acceptance Criteria**: `docs/analysis/eu-expansion/track-6-payments.md`; concrete presentment-vs-settlement recommendation; EuPlatesc disposition stated; per-tier local-payment-method list.
- **Priority**: Must

### FR-8: T7 — Codebase seam audit *(repo-bound, no web research)*
- **Description**: Audit where Romanian/RON/`ro-RO` is hardcoded today across Angular templates/components, backend messages, email templates, invoice PDF strings, legal pages, SEO/meta tags. Identify ANAF/Sameday/EuPlatesc coupling points that become per-country variation seams, and the currency hardcoding made critical by the local-currency decision.
- **Acceptance Criteria**: `docs/analysis/eu-expansion/track-7-seam-audit.md`; **file/occurrence counts per area** + the **top-10 heaviest retrofit spots**; notes which upcoming wave bolts (058, 067, 069) add to the bill; currency-hardcoding sized as its own area.
- **Priority**: Must

### FR-9: Synthesis → Options paper (D2)
- **Description**: A dedicated synthesis stage consumes all track findings and composes 2–3 **coherent bundles** (site architecture + i18n on the fixed RO-ship / one-brand / multi-currency baseline), each costed (one-off effort + recurring operational cost), each stating what it forecloses and what it keeps open. One bundle stress-tests the partner-fallback sensitivity. Recommendation is **separated from** the owner-decision list.
- **Acceptance Criteria**: `docs/analysis/eu-expansion-architecture-study.md`; bundles are coherent (never a menu of independent picks); each costed; explicit "owner must decide" list separate from the recommendation.
- **Priority**: Must

### FR-10: Owner decision → ADR (D3)
- **Description**: ⛔ HUMAN checkpoint. The owner reads D2, decides (possibly after follow-up questions). The ADR records the chosen bundle and the rejected options with reasons.
- **Acceptance Criteria**: New ADR appended to `memory-bank/standards/decision-index.md`, created **only** after the owner's explicit decision (never auto-decided).
- **Priority**: Must

### FR-11: Implementation brief(s) (D4)
- **Description**: Translate the ADR into concrete readiness requirements (seam preparation only, no translations), authored in the same style as the source brief — explicitly as the inception feed for the implementation intent(s). Split into multiple briefs if the decision splits the work.
- **Acceptance Criteria**: `docs/planning/i18n-readiness-brief-<date>.md` (+ more if needed); complete enough to hand directly to the inception agent with no additional context.
- **Priority**: Must

---

## Non-Functional Requirements

### Sourcing & Currency of Information
| Requirement | Standard | Notes |
|-------------|----------|-------|
| Official sources for regulatory claims | europa.eu, national tax authorities, carrier rate cards | No blogs/forums for tax/legal/regulatory facts |
| Every claim dated | Date on each regulatory/tax/cost claim | Tax rules change |
| Information currency | Reject pre-2021 / pre-OSS VAT sources | It is 2026 |

### Verification
| Requirement | Standard | Notes |
|-------------|----------|-------|
| Adversarial verification of high-stakes claims | Independent second agent confirms/refutes | Applies to VAT thresholds, OSS scope, e-invoicing mandates before they enter D2 |

### Method
| Requirement | Standard | Notes |
|-------------|----------|-------|
| Parallel multi-agent research | One researcher per track, clean context | Quality (unbiased depth), not just speed |
| Dedicated synthesis stage | Separate from track research | Produces coherent bundles |

### Process
| Requirement | Standard | Notes |
|-------------|----------|-------|
| Time-boxing | Spike-bolt rules; boxes proposed at bolt-plan | Stop when box expires; document what was learned; don't extend without approval |
| Costing | One-off effort + recurring operational cost per option | Bundles never uncosted |
| Quantitative fulfillment | Real per-parcel cost/time per corridor | Not hand-waving |
| Quantitative seam audit | File/occurrence counts + top-10 spots | Honest sizing |

### Compliance / Output Integrity
| Requirement | Standard | Notes |
|-------------|----------|-------|
| Zero production code | No production-code changes anywhere | Knowledge-only intent |
| Owner-decides | D3 ADR waits for explicit human decision | Synthesis recommends; human decides |
| No deployment pressure | Must not presuppose/pressure deployment | Roadmap Phase 6 is last |

---

## Constraints

### Technical Constraints
**Project-wide standards**: loaded from memory-bank standards by Construction Agent.

**Intent-specific constraints**:
- Research method MUST be multi-agent fan-out + adversarial verification + dedicated synthesis stage — not one sequential reader. The method requirement travels with the research stories.
- T7 (codebase seam audit) is repo-bound: no web research.
- Spike bolts MUST be time-boxed.
- Throwaway prototypes archived or deleted, never merged.

### Business Constraints
- Owner decides; research informs. D3 waits for the human.
- Settled owner decisions (RO-ship, one brand, multi-currency) are not reopened.
- Must not presuppose or pressure deployment (Phase 6, last).

---

## Assumptions

| Assumption | Risk if Invalid | Mitigation |
|------------|-----------------|------------|
| Ship-from-RO remains viable at the studied market scale | Architecture assumes single origin; partner model later forces rework | T1 costs the partner fallback and flags the market-size threshold to revisit |
| It is 2026; current EU VAT/OSS rules apply | Stale tax claims poison the decision | Reject pre-2021/pre-OSS sources; adversarially verify |
| Intent 033 env triad is the deployment baseline T2 builds on | T2 env-multiplier analysis is wrong | T2 references 033 explicitly |
| Multi-currency is presentment + (likely) EUR settlement via Stripe | Order/invoice model retrofit mis-sized | T6 confirms presentment-vs-settlement; T7 sizes the model impact |

---

## Open Questions

| Question | Owner | Due Date | Resolution |
|----------|-------|----------|------------|
| Target-market priority | Owner | Checkpoint 1 | ✅ Resolved: compare both tiers (HU/BG + DE/FR/IT/ES) |
| Brand strategy | Owner | Checkpoint 1 | ✅ Resolved: one brand EU-wide |
| Fulfillment appetite | Owner | Checkpoint 1 | ✅ Resolved: ship everything from Romania (partner = fallback/sensitivity) |
| Currency preference | Owner | Checkpoint 1 | ✅ Resolved: local currencies (PLN/HUF/CZK/BGN + EUR) |
| At what market scale should the partner-fulfillment model be revisited? | Research (T1) | Synthesis | Pending — T1 to propose a threshold |
| Settlement currency & FX handling (Stripe presentment vs settlement) | Research (T6) | Synthesis | Pending — T6 to recommend |
