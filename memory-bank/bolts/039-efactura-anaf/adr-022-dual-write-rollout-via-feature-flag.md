---
bolt: 039-efactura-anaf
created: 2026-06-03T12:30:00Z
status: accepted
---

# ADR-022: Dual-Write Rollout for Regulated Integrations via Feature Flag, Not Branch Deploy

> **Frozen bolt record.** Two statements below no longer hold: the flag gates no customer
> email (no send path exists), and the XML build, ANAF upload, PDF render and storage write
> are gated by `Anaf:Enabled`, not unconditional. `memory-bank/standards/decision-index.md`
> carries the current summary — read it before flipping anything in production.

## Context

Bolt 039 ships the full e-Factura pipeline: XML build, PDF render,
ANAF SPV upload, customer PDF download endpoint, customer email
attachment. The bolt's overview explicitly calls for a **dual-write
rollout posture**: in the first production week, the pipeline should
*generate* and *submit* invoices to ANAF (so we can inspect the XML
and verify ANAF accepts our format), but *not yet email the PDF to
customers* (so a render bug doesn't reach the customer inbox).

This is a recurring shape. The same posture will repeat for:

- **Credit-note submission** (future intent): generate storno
  invoices, submit to ANAF, don't email until verified
- **e-Receipts** (hypothetical future requirement): submit to
  ANAF e-Receipt API before exposing to customers
- **Any new regulated integration** that has an external-facing
  side effect we want to inspect before exposing

The team needs a project-level pattern for this rollout shape, not
ad-hoc decisions per bolt.

Two competing patterns:

1. **Feature flag in config** — the customer-facing side effect is
   gated by a setting (`Invoicing:CustomerEmailAttachments:Enabled`,
   default `false`). All the upstream pipeline runs regardless;
   only the *delivery* side is conditional.
2. **Branch deploy / two-build approach** — ship the bolt with
   customer email disabled in code; merge a follow-up PR a week
   later that re-enables it. The "off" version is its own deploy.

Both work. The choice has long-term implications for how future
regulated integrations get rolled out.

## Decision

**Dual-write rollouts of regulated integrations are gated by a
feature flag in `appsettings.json`, NOT by a separate code branch
or deploy. The flag defaults to `false` (customer-facing side effect
suppressed) and is flipped to `true` in production config after the
inspection week.**

Concretely for bolt 039:

- Setting: `Invoicing:CustomerEmailAttachments:Enabled` (default
  `false`).
- Strongly-typed in `InvoicingSettings.CustomerEmailAttachments.Enabled`.
- Read at runtime in three places:
  - The order-confirmation email pipeline checks the flag before
    attaching the PDF.
  - `InvoicePdfReadyNotifier.NotifyAsync` checks the flag and
    short-circuits when `false` (no follow-up "PDF ready" email).
  - Admin endpoints return PDF URLs unconditionally (admin tooling
    is not customer-facing).
- The flag is read on every request, not cached statically. Flipping
  the value in `appsettings.Production.json` and restarting the
  process is sufficient; no code change.
- Everything upstream of the customer-facing side effect runs
  regardless of the flag: XML build, ANAF upload, PDF render,
  storage write. The point is to exercise the full pipeline so the
  inspection week is realistic.

The same pattern applies to any future regulated integration:

- The setting name follows `{Integration}:CustomerFacingDelivery:Enabled`
  or similar; the path is settled per-integration but the *shape*
  (config flag, default false, suppresses one specific side effect)
  is the standard.

## Rationale

The feature-flag pattern wins on three dimensions that matter for
regulated work:

1. **Reversibility.** If the inspection week surfaces a problem
   that's caught late on day 7 (e.g. a Romanian-locale formatting
   issue that only manifests when an actual customer reads the
   PDF), flipping the flag back to `false` is a config-only change.
   No revert PR, no re-deploy of an older artefact. The pipeline
   keeps running; only the delivery stops.
2. **Inspectability without staging-vs-prod divergence.** With a
   branch-deploy approach, the production code path is *not* the
   one we tested in staging — we tested the "off" version in
   staging then deployed the "on" version a week later. The flag
   keeps both code paths identical; only data flow differs.
3. **Audit trail.** A config-only flip leaves a single "deploy
   2026-06-15" log entry. A branch-deploy approach mixes the
   inspection-week change with whatever else the team merged in
   the meantime, complicating the post-mortem if something goes
   wrong after the flip.

### Alternatives Considered

| Alternative | Pros | Cons | Why Rejected |
|---|---|---|---|
| **Config feature flag (chosen)** | Reversible without a deploy; production code path matches inspection week; clear audit trail. | Flag scaffolding lives in code permanently; small ongoing cost (one if-statement, one config read). | — |
| **Branch deploy / two-build** | No flag scaffolding in code. | Production code path diverges from inspection-week path; rollback requires a re-deploy of the prior artefact; mixes with other changes merged in the same week. | Reversibility cost is too high for regulated work. |
| **Per-customer rollout flag (gradual percentage)** | Even more cautious — first 1% of customers get the PDF, then 10%, then 100%. | Adds significant complexity (customer-set targeting logic, audit trail per customer). Overkill for a one-week inspection. | Disproportionate for the inspection scope. |
| **Manual cron toggle (job runs at 09:00 on day 8)** | Automatic flip. | Brittle (timezone bugs, off-hours surprise); harder to delay if inspection finds issues. | Loses the human-in-the-loop control. |
| **No dual-write — ship hot, monitor closely** | Lowest engineering cost. | If inspection finds a render bug on day 3, customers received broken PDFs for 3 days. Risk doesn't match the regulated nature of the work. | Risk profile wrong for tax-document workflow. |

## Consequences

### Positive

- **One-line rollback.** Inspection-week issue → flip the flag
  back, restart the process. No re-deploy of stale code.
- **Pipeline tested end-to-end during the inspection week.** XML
  build, ANAF upload, PDF render, storage write all run on real
  prod traffic. The only thing that doesn't run is the email
  attachment. By day 8, we've already proven the pipeline at
  scale.
- **Pattern is reusable.** The next regulated integration (credit
  notes, e-receipts) ships with the same shape. Reviewers know
  what to look for; operators know how to roll back.
- **No code drift between inspection and production.** The "off"
  and "on" code paths are the same code; only one config value
  changes.

### Negative

- **Persistent flag in code.** The `if (settings.Enabled)` branch
  stays in the codebase forever (or until a follow-up PR removes
  it). Small but real ongoing complexity. Mitigation: the flag is
  one if-statement and one config read; trivial to delete once the
  rollout is permanent.
- **Operators must remember to flip the flag.** The inspection
  week has a calendar end; the flip is a manual config push.
  Mitigation: the rollout plan in DEPLOYMENT.md names the date and
  the exact config change; the deploy is scheduled like any other
  prod change.
- **A future contributor may misread the flag's purpose** ("this
  is a feature toggle, let me reuse it for A/B testing"). The flag
  name (`CustomerEmailAttachments:Enabled`) implies a single-use
  rollout, not a multi-tenant toggle. Mitigation: this ADR; the
  setting's XML doc-comment names the rollout phase explicitly.

### Risks

- **Risk: the flag is never removed and rots.** Pattern leak —
  five flags accumulate, each from a past rollout, all stuck at
  `true`. Mitigation: a follow-up PR after permanent rollout
  deletes the flag and the if-statement. Tracked as a cleanup
  ticket at the time of the flip.
- **Risk: someone hot-flips the flag in production without going
  through the deploy process.** This is a general operations risk
  with file-based config; not specific to this flag. Mitigation:
  config changes go through the same review as code changes; the
  flag is in source-controlled `appsettings.Production.json` (per
  the existing project standard).
- **Risk: the flag protects only one side effect — a future PR
  adds a second customer-facing leak (e.g. a webhook to a partner
  CRM) that doesn't check the flag.** Mitigation: code review;
  this ADR's Decision section names the flag's purpose ("suppresses
  one specific side effect") and any new customer-facing side
  effect during dual-write should consult the flag.

## Related

- **Stories**: 003-invoice-pdf-renderer-and-endpoint (the immediate
  consumer); the broader pattern applies to all future regulated
  integrations.
- **Standards**: candidate for a section in
  `memory-bank/standards/deployment-patterns.md` if that file is
  ever created — the dual-write rollout shape would belong there.
- **Previous ADRs**: ADR-008 (two-tier storage) — similar
  config-gated runtime behaviour; ADR-013 (Sameday token cache) —
  same precedent of master-`Enabled` flags for new integrations.
- **Future ADRs**: any change to the rollout strategy (e.g. moving
  to a percentage rollout) should supersede this ADR or document
  the divergence.
- **Read when**: planning a rollout of any new regulated
  integration (credit notes, e-receipts, anything ANAF-adjacent);
  reviewing PRs that add a new "off by default" feature flag;
  reviewing PRs that read `Invoicing:CustomerEmailAttachments`;
  flipping the flag in production (use this ADR to recall what
  side effect is gated and what's NOT gated); cleaning up unused
  feature flags after a permanent rollout.
