# EU Expansion — Research Brief (Inception Feed)

> **What this document is.** The requirements feed for a *special* intent: a
> **research-only intent** (roadmap Phase 5 preparation). Hand this document to the
> specsmd inception agent to create the intent. Unlike normal intents, its bolts write
> **no production code** — they produce knowledge: research findings, an options paper,
> an owner decision recorded as an ADR, and **new requirement briefs that are themselves
> fed back into the inception agent** to create the real implementation intent(s).
>
> **The loop, explicitly:**
> `this brief → inception → research intent (spike bolts) → findings + options paper →
> owner decides → ADR → implementation brief(s) → inception → implementation intent(s)`
>
> **Bolt type:** `spike-bolt` (`.specsmd/aidlc/templates/construction/bolt-types/spike-bolt.md`)
> — explicitly designed for this: time-boxed, knowledge-out, human checkpoint between
> explore and document stages.

---

## 1. Context and motivation

The business ambition is to enter **all EU country markets**, not just Romania. Roadmap
Phase 5 (see `docs/analysis/ai-workflow-review-2026-06-05.md` §6) prepares the
*architecture* for multiple languages and countries — it does **not** implement
translations, and nothing gets deployed (deployment is Phase 6, deliberately last).

Two large questions currently block any sensible implementation work, and guessing on
either would be expensive:

1. **The fulfillment question (business-dominant).** When a customer in Germany orders
   prints, who physically prints and ships them? Cross-border shipping from Romania,
   local print partners per country, or a hybrid? This constrains the software
   architecture more than any technical preference does.
2. **The site-architecture question.** One multi-language site, or multiple per-country
   sites on different URLs? Deliberately undecided by the owner; ripples through URLs,
   SEO, Angular build strategy, deployment topology (interacts with intent 033's
   Staging/Production tiers), content, and per-country configuration.

This intent exists to answer both with evidence, and to turn the owner's decision into
actionable implementation requirements.

## 2. Research method — REQUIRED: a multi-agent research job

The research must NOT be one agent reading the internet sequentially. Execute it as a
**fan-out of parallel, clean-context research agents** — one per track below — followed
by a synthesis stage. This matters for quality, not just speed: each researcher goes deep
on one dimension without the others' conclusions biasing it.

Method requirements:

- **One researcher per track (§3), run in parallel.** Each produces its own findings
  document with sources.
- **Adversarial verification of high-stakes claims.** Every regulatory/tax/legal claim
  (VAT thresholds, OSS scope, e-invoicing mandates) gets a second, independent agent
  attempting to refute or confirm it against official sources before it enters the
  options paper. Tax rules change; a stale claim here poisons the decision.
- **Sourcing standard:** official sources for anything regulatory (europa.eu, national
  tax authorities, carrier rate cards); every claim dated; information currency matters —
  it is 2026, reject sources describing pre-2021 VAT rules (pre-OSS).
- **The codebase seam audit (T7) runs against this repository**, not the web.
- **Synthesis is its own stage**, consuming all track findings and producing the options
  paper (§4 D2). The synthesizer must compose 2–3 *coherent bundles* (a fulfillment model
  + site architecture + i18n approach that fit together), not per-dimension menu picks.
- Throwaway prototypes are allowed per spike rules (e.g. a 20-line Angular i18n build
  experiment) but are archived or deleted, never merged.

## 3. Research tracks (the parallel fan-out)

### T1 — Fulfillment & logistics *(the dominant track)*
- Cross-border shipping RO→EU: carriers, realistic costs and delivery times for a
  photo-print parcel to DE/FR/IT/ES/PL/HU/BG; where Sameday's coverage ends.
- Local print-partner model: do white-label photo-print fulfillment networks exist in the
  EU? Pricing models, integration APIs, quality control implications.
- Competitive scan: how do existing EU photo-print players (e.g. national leaders,
  pan-EU brands) structure fulfillment and country presence?
- Output: per-model cost/time/complexity comparison + scenarios per target-market tier.

### T2 — Site & URL architecture
- Multi-locale single site vs per-country sites: ccTLDs vs subdomains vs path prefixes
  (`/de/`); SEO consequences (hreflang, domain authority splitting); legal-page and
  content management per jurisdiction.
- Hosting/deployment topology per option — must reference intent 033's environment triad
  (Staging/Production) and state how each option multiplies (or doesn't) the environment
  count.

### T3 — Frontend internationalization
- Angular's built-in i18n (compile-time, one build per locale) vs runtime libraries
  (Transloco and peers) for **Angular 21 specifically** — maturity, bundle impact,
  developer workflow, and how each interacts with the T2 options.
- Currency/number/date formatting; RTL not required (EU scope).

### T4 — Backend localization
- .NET resource-based localization for: validation/error messages (ProblemDetails),
  transactional emails (Razor templates exist), invoice PDFs, enum/display strings.
- Culture resolution per request (header? user preference? site-of-origin?) and how it
  flows into background jobs that send emails *later* (the culture must be stored, not
  ambient — flag this trap explicitly).

### T5 — Tax, invoicing & compliance
- **EU OSS (One-Stop-Shop) VAT** for B2C distance selling: registration, thresholds,
  per-country VAT rates application, reporting cadence — what it means concretely for
  this codebase's `VatCalculator` (bolt 038) which currently assumes Romanian VAT.
- E-invoicing per country: ANAF e-Factura (bolt 039) is Romania-only; what (if anything)
  is mandated for B2C sellers in the other target markets in 2026.
- Currency: EUR vs local currencies (PLN, HUF, CZK…) — pricing, display, settlement.
- GDPR is already handled EU-wide; note any per-country consumer-law deltas that affect
  checkout copy (withdrawal rights wording) without going down a legal rabbit hole.

### T6 — Payments & checkout
- Stripe's EU coverage incl. local methods (iDEAL, Bancontact, Przelewy24, etc.) — what
  enabling them implies for the existing Stripe integration (bolt 016).
- EuPlatesc remains Romania-only: keep, or retire outside RO?
- Multi-currency: presentment vs settlement currency; impact on order/invoice models.

### T7 — Codebase seam audit *(repo-bound, no web research)*
- Where is Romanian hardcoded today? UI copy in Angular templates/components, backend
  messages, email templates, invoice PDF strings, legal pages, SEO/meta tags.
- Where is RON/currency hardcoded? Where is `ro-RO` culture assumed?
- ANAF/Sameday/EuPlatesc coupling points that become per-country variation seams.
- Output: honest sizing — counts per area, the top-10 heaviest retrofit spots, and which
  upcoming wave bolts (058, 067, 069) will add to the bill.

## 4. Deliverables

| # | Deliverable | Location | Notes |
|---|------------|----------|-------|
| D1 | Per-track findings docs (7) | `docs/analysis/eu-expansion/track-<n>-<slug>.md` | Sourced + dated claims; verification verdicts inline |
| D2 | **Options paper** | `docs/analysis/eu-expansion-architecture-study.md` | 2–3 coherent candidate architectures; per-option: what it costs (one-off effort + recurring), what it forecloses, what it keeps open; explicit recommendation **separated from** the owner-decision list |
| D3 | **Owner decision → ADR** | `memory-bank/standards/decision-index.md` (new ADR) | ⛔ HUMAN checkpoint: the owner reads D2, decides (possibly after follow-up questions). The ADR records the chosen bundle and the rejected options with reasons |
| D4 | **Implementation brief(s)** | `docs/planning/i18n-readiness-brief-<date>.md` (+ more if the decision splits work) | Written in the same style as THIS document — explicitly authored as the inception feed for the implementation intent(s). Must translate the ADR into concrete readiness requirements (seam preparation only, no translations) |

## 5. Acceptance criteria for the research intent

- [ ] Every regulatory/tax claim in D1/D2 carries a source and a date; high-stakes claims
      show their adversarial-verification verdict.
- [ ] D2's options are coherent bundles (fulfillment + site architecture + i18n approach),
      each costed (effort estimate + recurring operational cost) — never a menu of
      independent picks.
- [ ] The fulfillment track produces actual numbers (cost/time per parcel per corridor),
      not hand-waving.
- [ ] T7 sizes the retrofit with file/occurrence counts and names the top-10 spots.
- [ ] The "owner must decide" list is explicit and separate from the recommendation.
- [ ] D3 ADR exists only after the owner's explicit decision (never auto-decided).
- [ ] D4 brief(s) are complete enough to hand directly to the inception agent with no
      additional context.
- [ ] Zero production-code changes anywhere in this intent.

## 6. Constraints

- **No production code, no translations, no deployment** — knowledge only (spike rules).
- **Time-boxed** per spike-bolt requirements; propose the boxes at bolt-plan time.
- **The owner decides; the research informs.** The synthesis recommends, but D3 waits for
  the human.
- Scenario-handling for unknown business inputs (§7): where an owner input is missing,
  research proceeds with explicit scenarios rather than blocking or assuming.
- Must not presuppose or pressure deployment (roadmap: deployment is Phase 6, last).

## 7. Owner inputs (answer if known; otherwise research runs scenarios)

1. **Target-market priority**: which countries first? (Scenario default: a neighbors-first
   tier — HU/BG — vs a big-market tier — DE/FR — compared side by side.)
2. **Brand strategy**: one brand EU-wide, or per-country brands? (Affects T2 heavily.)
3. **Fulfillment appetite**: is partnering with local print shops acceptable, or is
   everything shipped from the Romanian operation for the foreseeable future?
4. **Currency preference**: EUR-only outside Romania, or local currencies?

## 8. Suggested decomposition (hint for the inception agent — not binding)

- Intent: `034-eu-expansion-architecture-study` (next free number at authoring time).
- Unit 1 — *research tracks*: stories T1–T6 as parallel-friendly spike work, T7 as a
  repo-bound story; **spike-bolt(s)**. Note for the bolt-parallel-planner: T1–T6 are
  mutually independent and conflict-free (docs-only) — ideal wave-parallel candidates.
- Unit 2 — *synthesis & decision*: the options paper, the ⛔ owner checkpoint, the ADR;
  spike-bolt (its Stage-2 "document" + human checkpoint maps exactly onto D2→D3).
- Unit 3 — *implementation briefs*: author D4 from the ADR; simple bolt (docs only).
- Hard internal ordering: Unit 1 → Unit 2 → Unit 3 (each consumes the previous).
- Construction instances executing the research stories should themselves use
  multi-agent fan-out (parallel web researchers + verifier agents) per §2 — the method
  requirement travels with the stories.
