# Maintenance Log

Status-integrity syncs and housekeeping performed by the Master Agent's `analyze-context` integrity check.

---

## 2026-06-05T10:30:00Z - Status Sync

**Triggered by**: analyze-context integrity check (Master Agent), after inception of architect-review-2026-06-03 intents 025–031.

### Intent status corrections

| Artifact | Old Status | New Status | Reason |
|----------|------------|------------|--------|
| intents/024-order-photo-archive/requirements.md | draft | complete | Bolts 051/052/053 are complete (shipped 2026-05-30→06-01); story-index already lists 024 as complete |
| intents/022-coupon-promo-codes/requirements.md | complete | inception-complete | Bolts 047/048 are still `planned` — coupon is specced but NOT built; `complete` falsely implied shipped |

### Bolt terminology normalization (`status:` line only)

Normalized 21 bolts from non-standard done-states to the template-standard `complete` (the `completed:` timestamp fields and `stages_completed:` arrays were left untouched). Result: 49 `complete` + 19 `planned` (was 28 complete / 19 completed / 2 done / 19 planned).

- **`completed` → `complete` (19):** 001, 003, 012, 013, 014, 015, 016, 017, 018, 019, 020, 021, 022, 027, 028, 029, 030, 031, 032
- **`done` → `complete` (2):** 007-guest-sessions, 011-product-catalog-ui

### Story-index overview refresh

| Artifact | Change | Reason |
|----------|--------|--------|
| story-index.md (Overview bullets) | Moved 016 (VAT) + 020 (observability) from "planned" → "complete (shipped)"; split out 021 as "parked"; clarified 022 + 054–069 as "planned (not built)" | Header was stale — listed shipped intents as planned and shipped bolts (038/039/044/045) as planned |

### Notes

- **Intent 021 / bolt 046 (Redis multi-replica)** remains correctly **parked** — deprioritized until the app is deployed and there is real scaling pressure. Not a defect.
- **Remaining `planned` bolts (19):** 046 (parked), 047, 048 (coupons), 054–069 (architect-review 2026-06-03 backlog).

---

## 2026-06-05T13:00:00Z - Inception (roadmap Phase 3–4 intents 032–033)

**Triggered by**: Inception Agent run for the owner's roadmap Phase 3 (stabilize) + Phase 4 (environment triad). Source: `docs/analysis/ai-workflow-review-2026-06-05.md` §6. Self-validated at all four checkpoints (no human available mid-run; owner reviews afterward).

### Intents created

| Intent | Type | Units | Stories | Bolts |
|--------|------|-------|---------|-------|
| 032-regression-and-e2e-stabilization | brown-field / stabilization | 3 | 15 | 070, 071, 072 |
| 033-environment-triad | brown-field / infrastructure readiness (NOT deployment) | 3 | 10 | 073, 074, 075 |

### Dependency edges declared (build-on, not duplicate)

- **070-e2e-data-strategy** `requires_bolts: [066-ci-quality-gates, 062-test-infrastructure]` — extends the Playwright foundation + reuses the Builders.
- **071-e2e-journey-coverage** `requires_bolts: [070, 066]`; gated coupon/refund specs reference bolts 047/048 + 068/069 (authored, not implemented).
- **072-regression-methodology** `requires_bolts: [071]`; soft dependency on 057 (KNOWN_FAILURES.md).
- **073/074/075** form an internal chain (073 → 074 → 075); no external bolt dependencies (builds from shipped infra assets).

### Story-index changes

| Artifact | Change | Reason |
|----------|--------|--------|
| story-index.md | Appended intent 032 (15 stories) + 033 (10 stories) sections, all ✅ GENERATED | New inception artifacts |
| story-index.md (Overview) | Total listings 155 → 180; files-on-disk 142 → 167; added Phase 3–4 intent/bolt planned bullets; recorded bolts 070–075; reaffirmed bolt 050 unallocated | Keep header truthful |

### Notes

- **Intent 033 is infrastructure readiness only — NOT deployment.** Deployment is roadmap Phase 6. No deploy/provision/cutover work was planned; bolt 075 carries an explicit Phase-6 deferral note.
- **Bolt 050 remains unallocated by design** — new bolts started at 070 per the run constraint.
- **Self-validation concern flagged for owner**: unit 002 of intent 032 carries 8 stories (over the 5–6 soft cap) — kept as one bolt (071) because they are thin, parallel, domain-sliced specs over one shared fixture layer; owner may prefer a 2-bolt split.
- **Remaining `planned` bolts now (25):** 046 (parked), 047, 048 (coupons), 054–069 (architect-review), 070–075 (roadmap Phase 3–4).

---

## 2026-09-03T08:29:15Z - Reconciliation of intent 035 with the review loop

**Triggered by**: the theory-vs-practice comparison of the bug-hunter blueprint against the
review loop that was built while reviewing, and the owner's ruling to rewrite the intent in
place around what is missing (rather than start a new intent or mark the built parts complete).

**What it is.** Intent 035 planned the bug-hunting system as 43 construction briefs in ten
bolts, 085–094, all `planned`. The engine has since been built — it is the **review loop under
`reviews/`**, in pre-merge mode. One engine, two modes: the pre-merge pass over a branch
exists; a standing sweep over `main` does not. Of the 43 briefs, **12 are satisfied**,
**31 remain** (16 missing + 15 partial). The status of every brief lives in the guide's
"Implementation status (2026-09)" table — that table, not this log, is the record.

### Bolts removed

| Bolt | Why | Where its work lives now |
|------|-----|--------------------------|
| 085-phase-1-skeleton-core | Satisfied: the ledger, dedup, report rendering and the owner-decision channel all exist | `reviews/lib/records/`, `.claude/skills/reconcile-findings/`, `reviews/templates/`, `reviews/lib/drive/gates.mjs` |
| 086-phase-1-skeleton-agents | Satisfied: the hunter and the six-slot coordinator exist | the core six lenses (`reviews/lib/records/schema.mjs`), `.claude/skills/loop-driver/`, `reviews/lib/drive/route-next-pass.mjs`, `reviews/lib/discovery-review.wf.js` |

**Removed, not marked `complete`.** `bolt-process.md` allows `status: complete` only after the
bolt's first discovery pass; neither of these had one, and neither was built as a bolt at all.
The status vocabulary stays `planned`/`complete`, so a satisfied bolt is retired instead. Their
stories stay on disk under the intent's unit folder, each marked with the file that satisfies
it — the history is kept, the schedule is not.

### What changed

| Artifact | Change |
|----------|--------|
| intents/035/requirements.md | Header note, "Intent Overview", In Scope and Out of Scope rewritten around the gaps; each gap names the `reviews/lib` seam it extends; the skill-creator mandate narrowed to new standalone skills; five dangling citations in the inception log repointed at `git show b4329a8^:…` |
| intents/035/units.md | Re-cut in the ruled order, still **6 units**: unit 001 satisfied (no bolt), unit 003 read as three waves — 3a map (088), 3b specialists (089 ∥ 090), then the oracle tier as its last bolt (091, gated), so the oracle's external gate no longer holds up the map |
| bolts 087–094 (8 bolt.md files) | Overviews re-scoped to the gaps; the construction-method box now points at the review-loop seam instead of skill-creator-for-everything; 087 `requires_bolts` → `[]`; `requires_units` no longer names the satisfied unit 001; 091 marked last in the order with a `notes:` key (nothing waits on it) and 092 re-pointed at 089/090; statuses all still `planned` |
| intents/035 stories (43 files) | Each satisfied story carries `**Status:** satisfied by <path> (2026-09)`; every other story carries `**Workbench seam:** <path>`; `001-suppression-learning` marked superseded (decision attachment); the seven unit-001 stories now have `assigned_bolt: null`; prose references to bolts 085/086 repointed at the review loop |
| story-index.md | Overview planned-bolt list 085–094 → 087–094; a dated re-scope note under the 035 heading; the 12 satisfied stories marked `✅ GENERATED · satisfied by the review loop (2026-09)`. No listing added, removed or renumbered |

### Order

Ruled by the owner and written up in the integration contract §7: **087** (trust upgrades —
tool ingest, risk score, execution proof, moved/fixed detection) → **088** (the Map slot:
application map, code index, reachability, budget) → **089 ∥ 090** (specialists, both waiting on
the map) → **092** (learn & measure) → **093** (remediation hand-off) → **091** (oracle tier,
last, gated on the knowledge builder's `ledger-query`, and the last bolt of unit 003); **094**
optional integration comes after 091 in §7's list and stays ⏸ adoption-gated — its only bolt
dependency is 092.

### Pointers

- [docs/agent-systems/theory-vs-practice-2026-09.md](../docs/agent-systems/theory-vs-practice-2026-09.md) — the blueprint-versus-workbench comparison this re-scope came out of.
- [docs/agent-systems/reconciliation-plan-2026-09.md](../docs/agent-systems/reconciliation-plan-2026-09.md) — the plan it was executed under, including the owner rulings.
- `docs/agent-systems/bug-hunter-build-guide.md` § "Implementation status (2026-09)" — the per-brief status table (43 briefs: 12 · 15 partial · 16 missing).

### Notes

- **Remaining `planned` bolts now (42, was 44):** 046 (parked), 047, 048 (coupons), 054–069
  (architect-review), 070–075 (roadmap Phase 3–4), 076–084 (intent 034 research), 087–094
  (intent 035 — 091 ⛔ gated, 094 ⏸ adoption-gated).
- Standing-sweep mode (the second of the two modes) has no bolt of its own yet.
- No production code, tests or `reviews/**` files were touched: this was a documentation
  reconciliation only.

---
