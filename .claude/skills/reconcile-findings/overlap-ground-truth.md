---
type: reconciler-eval-set
target: bolt-035-payment-idempotency
created: 2026-07-04
labeled-by: Claude (hand judgment over review-v1/v5/v8 + resolution-v1/v5/v8) — owner spot-check requested before freezing
status: ground-truth draft — correct anything wrong, then treat as frozen
inputs: review-v1.md (commit 691e23d) · review-v5.md (224c711) · review-v8.md (50fc692) · all three resolutions
---

# Bolt 035 — hand-labeled finding overlap (reconciler ground truth)

Bolt 035 was audited from scratch three times (v1, v5, v8). Each audit used its own local
finding IDs. This file is the **answer key**: every finding mapped to a canonical problem, so a
future reconciler tool can be scored against known-correct matches before being trusted.

## Headline results

- **53 finding IDs were ever issued** (15 + 15 + 18 across the audits, plus 5 from
  verification rounds). They collapse to **50 distinct problems**.
- **The true cross-audit identity overlap is exactly 1 problem** (P01b, the snapshot drift,
  found by all three audits). Not the "~4 in common" the README estimated.
- **The audits did not review the same code.** v1 ran pre-fix; 13 of its 15 findings were
  fixed before v5 ran; more fixes landed before v8. The problem population was *open*:
  fixes removed old problems and created new ones. **12 of the 50 problems (~a quarter) were
  introduced by fixes.** Any overlap math that assumes a closed population (capture–recapture)
  is invalid across these audits.
- **At the moment v5 declared "approved, 0 blockers", at least 14 problems known today were
  present in the code and un-named** (one of them later rated Medium). None was High.
- **The only High ever found (P04) was caught by the first audit** — the "serious surfaces
  early" data point. Still n=1.
- **5 findings re-raised something already decided** (deferred / wont-fix / "not a finding").
  **3 of the 5 re-raises won** — the recorded decision was overturned and the code improved.
  A reconciler must *link* re-finds to prior decisions, never auto-suppress them.

## Labeling rules used

- **Same problem** = same defect mechanism at the same site, even if framed differently or
  rated a different severity. Everything else is distinct.
- **Fix-residual** = a *new* defect living in the fix of an earlier finding (e.g. the catch
  added for P04 became P05's too-broad catch). Causally linked, **never** "same".
- **Present, missed** = the problem demonstrably existed at that audit's commit but got no ID.
  Marked *(unverified)* where presence at that commit is inferred, not checked in the code.
- **~ noted, no ID** = the audit's text touched the problem (a test-gap row, a fix-note
  aside, a refutation) without issuing a finding ID. These escape any ID-based ledger.

## The identity matches (the only cross-audit "same problem" group)

| Canonical | v1 | v5 | v8 | Evidence |
|---|---|---|---|---|
| **P01b** — model snapshot is Npgsql-typed → phantom diff on next Npgsql scaffold | BUG-5 (residual half) | DB-1 | DB-2 | resolution-v5 DB-1 says literally "= BUG-5 v1 residual"; v8.DB-2 marked "known/deferred" via the migration breadcrumb |

Every other pair that looks like a match is a *related-but-distinct* ruling — see the hard
cases below. That's the eval set's core lesson: naive file/theme matching over-merges badly.

## Full ledger

Cell legend: `ID` = found with that ID · `fixed pre` = fixed before this audit ran (not
findable) · `present, missed` = findable, not found · `not yet` = didn't exist yet ·
`~` = touched without an ID.

### Migration / DB parity

| # | Problem | v1 | v5 | v8 | Born | Final status |
|---|---------|----|----|----|------|--------------|
| P01a | Migration not provider-aware (Npgsql needs varchar + filtered index) | BUG-5 (fixed half) | fixed pre | fixed pre | bolt | verified (2f1872c) |
| P01b | Snapshot Npgsql-typed → phantom scaffold diff | BUG-5 (residual) | DB-1 | DB-2 | bolt | accepted-deferred (migration/deploy phase) |
| P02 | No Postgres in test matrix at all (migration DDL never run; Npgsql arm untested) | ~ T8 test-gap row, no ID ("Critical") | ~ noted in QUAL-5 + verify notes, no ID | DB-1 🟠 | bolt tests | accepted-deferred |
| P03 | `StripeClientSecret varchar(255)` zero headroom → prod-only 500 | present, missed *(unverified)* | DB-2 🟠 | fixed pre | pre-bolt/bolt | verified (varchar 512) |

### Collision classification (a three-generation fix-residual chain)

| # | Problem | v1 | v5 | v8 | Born | Final status |
|---|---------|----|----|----|------|--------------|
| P04 | Concurrent same-key INSERT → unhandled 500 | **BUG-1 🔴** | fixed pre | fixed pre | bolt | verified |
| P05 | Recovery catch too broad — infers cause via `AnyAsync` | not yet (created by P04's fix) | BUG-1 | fixed pre | fix (v1 round) | verified |
| P06 | PostgreSQL violation detection via message substring | not yet | not yet (created by P05's fix) | BUG-1 (×5 lens convergence) | fix (v5 round) | verified (shared const + code 2067) |
| P07 | OrderNumber collision in same-key race → 500 (PostgreSQL count-based numbering) | not yet (PostgreSQL count branch added v4) | present, missed — **pre-dismissed in v4 resolution note as "not a new finding"** | BUG-4 | fix (v4 round, BUG-6) | verified (bounded retry) — the dismissal was overturned |

### Tenant scoping / key namespace

| # | Problem | v1 | v5 | v8 | Born | Final status |
|---|---------|----|----|----|------|--------------|
| P08 | Idempotency lookup + stale-free not tenant-scoped (IDOR, secret leak) | SEC-1 🟠 | fixed pre | fixed pre | bolt | verified |
| P09 | Both-null owner predicate degenerates to "any guestless order" | not yet (the predicate IS P08's fix) | SEC-1 | fixed pre | fix (v1 round) | verified (guard) |
| P10 | Global single-column uniqueness = existence oracle + key squatting | ~ aside in SEC-1's fix note ("consider (owner,key)") | ~ probed and refuted-as-accepted ("key-namespace griefing") | SEC-1 | bolt | accepted-deferred (threat note; migration phase) |
| P11 | Expired key reclaimable only by its owner → 24h contract broken cross-caller | not yet (created deliberately by P08's fix) | present, not re-raised — **already accepted as INFO-2 (v2, wont-fix)** | REQ-1 | fix (v1 round) | verified-as-documented (decision re-affirmed) |

### Header / filter input handling (another fix-residual chain)

| # | Problem | v1 | v5 | v8 | Born | Final status |
|---|---------|----|----|----|------|--------------|
| P12 | Header extraction + missing-key warning copy-pasted per endpoint | QUAL-3 🟠 | fixed pre (filter created) | fixed pre | bolt | verified |
| P13 | Key length (spec 1..80) not validated → >80 chars 500s on Postgres | not yet (filter not yet) | SEC-2 | fixed pre | fix (v3 round) | verified (400) |
| P14 | Key not trimmed → padded variants defeat dedupe (double charge) | present, missed *(unverified — pre-filter inline read)* | present, missed (found adjacent P13, missed this) | SEC-2 | bolt/fix | verified (trim) |
| P15 | Missing-key event logs at Warning on every request (noise) | present, missed | present, missed | OBS-3 | bolt | verified (Information; doc-alignment reopened v9, closed v10) |
| P16 | Planned "header required" breaking change untracked | OPS-1 🟠 | fixed pre (TODO) | fixed pre | bolt | verified (token later dropped and restored — see P48) |

### Controller / replay path

| # | Problem | v1 | v5 | v8 | Born | Final status |
|---|---------|----|----|----|------|--------------|
| P17 | Replay/compute/persist duplicated across Stripe + EuPlatesc branches | QUAL-4 🟠 | fixed pre (`CreateIntentAsync<T>`) | fixed pre | bolt | verified |
| P18 | Client's key forwarded to Stripe (recycled-key collision at gateway) | BUG-4 | fixed pre (keyed by order.Id) | fixed pre | bolt | verified |
| P19 | Recovery-replay path (replay + null cached) unobserved / undocumented | ~ T6 test-gap row, no ID ("intentional, add a test") | OBS-3 | fixed pre (log + doc + test) | bolt | verified |
| P20 | EuPlatesc recovery rebuilds a *different* signed URL (verbatim-replay invariant broken) | present, missed *(unverified)* | ~ seen inside OBS-3, judged benign, no ID | BUG-2 (×3 convergence) | bolt | accepted-deferred (row-lock needs Postgres arm; asymmetry documented) |
| P21 | Replay-logging branches duplicated in `CreateIntentAsync` | not yet | largely not yet (worsened by P19's fix) | QUAL-5 | fix (v5 round) | verified (single switch) |
| P22 | Two-phase save: order saved in service, again in controller after gateway | QUAL-6 | ~ adjacent QUAL-3 (different claim) | present, not re-raised | bolt | **wont-fix stands** (intentional crash-recovery) |
| P23 | Controller persists via `_db.SaveChangesAsync` directly (altitude) | ~ subsumed under QUAL-6's view, no ID | QUAL-3 | present, not re-raised | pre-existing convention | accepted-deferred (codebase-wide boundary decision) |
| P24 | Stale-key free + INSERT in two saves → crash loses key linkage | ~ mentioned inside BUG-1's text, no ID | ~ doc facet only (see P25); behavior judged intentional | BUG-3 | bolt | verified (single SaveChanges) — **the recorded "intentional" design was overturned** |

### Observability / contract

| # | Problem | v1 | v5 | v8 | Born | Final status |
|---|---------|----|----|----|------|--------------|
| P25 | ddd-02 claims free+insert is "same transaction" (doc wrong) | not yet (doc claim predates? treated as v5's find) | DOC-1 | fixed pre | bolt docs | verified |
| P26 | 409 `divergentFields` gated out of Development + body never asserted | present, missed *(unverified)* | OBS-1 🟠 | fixed pre | bolt | verified |
| P27 | Reserved `payments.idempotency.conflict` event never emitted | present, missed | OBS-2 | fixed pre (scoped to same-caller conflicts, by recorded decision) | bolt | verified |
| P28 | Cross-tenant collision 409 has no distinct type/log | (folded in P27 pre-split) | not yet as distinct (created by P27's scoped fix + accepted decision) | OBS-1 🟠 | fix (v5 round decision) | verified (`IdempotencyKeyTakenException` + reserved event) — **the accepted scoping decision was overturned** |
| P29 | 409 body undocumented in `ProducesResponseType` (OpenAPI) | present, missed | present, missed | OBS-2 | bolt | verified (typed ProblemDetails) |
| P30 | Middleware resolves `IHostEnvironment` two different ways | present, missed | present, missed | QUAL-6 | pre-existing | verified |

### Docs

| # | Problem | v1 | v5 | v8 | Born | Final status |
|---|---------|----|----|----|------|--------------|
| P31 | Stale-key comment says Postgres-only per-statement check | DOC-1 | fixed pre | fixed pre | bolt | verified |
| P32 | Unique-index NULL comment phrasing | DOC-2 | fixed pre | fixed pre | bolt | verified |
| P33 | ddd-02 sketch places conflict handling in controller (impl. moved it) | DOC-3 | present, missed | present, missed | bolt docs | **still deferred — never fixed, never re-found** |
| P34 | ddd-02 doesn't document Stripe keyed by order.Id | not yet (keying itself is P18's fix) | DOC-2 | fixed pre | fix (v1 round) | verified (edit was incomplete → P49) |

### Quality / tests

| # | Problem | v1 | v5 | v8 | Born | Final status |
|---|---------|----|----|----|------|--------------|
| P35 | Redundant second DB round-trip for the stale row | QUAL-1 🟠 | fixed pre | fixed pre | bolt | verified |
| P36 | `IdempotencyConflictException` overlaps `ConflictException` | QUAL-2 | present, missed | present, missed | bolt | **wont-fix stands** (note: P28's fix later added a *third* conflict type; nobody reconciled this with P36's concern) |
| P37 | `HttpContext.Items` raw string key for CorrelationId | QUAL-5 | fixed pre | fixed pre | bolt | verified |
| P38 | Pre-INSERT + post-collision resolution blocks near-duplicate | not yet (post-collision block created by P04's fix) | QUAL-1 | fixed pre | fix (v1 round) | verified |
| P39 | DB provider magic strings repeated | partially present, missed *(unverified)* | QUAL-2 | fixed pre | mixed | verified (`DbProviders`) |
| P40 | EF1002 warning unsuppressed/unjustified on year-sequence SQL | not yet *(branch reworked at v4)* | QUAL-5 | fixed pre | pre-bolt/fix | verified (pragma + comment) |
| P41 | Payment POST builders / request helpers duplicated across test files | not yet (tests grew v1→v5) | QUAL-4 | fixed pre (partial, accepted leftovers) | fix rounds | verified (v7) |
| P42 | Cart-seed entity graph duplicated across 3 fixtures (drifting) | not yet | ~ adjacent QUAL-4 territory, no distinct ID | QUAL-3 | fix rounds (fixtures from v2–v3) | verified (`TestCartSeed`) |
| P43 | Concurrency test hand-builds winner with magic totals | not yet (test created by P04's fix) | present, missed | QUAL-4 | fix (v1 round) | verified |
| P44 | `GetByIdempotencyKeyAsync` is dead production code | not yet (killed by P04/P08 fixes) | present, missed — **v5's own QUAL-1 fix even touched the dead method** | QUAL-1 (×3 convergence) | fix (v1 round) | verified (removed) |
| P45 | `ResolveUnitPrice` duplicates and diverges from `CartService` | present, missed | present, missed | QUAL-2 | **pre-existing (before the bolt)** | verified (`PricingTierResolver`) |
| P46 | `DivergentFields` ignores cart items → wrong photos replayed | BUG-3 🟠 | fixed pre (`ItemsSignature`) | fixed pre | bolt | verified |

### Raised outside the audits (verification rounds — for ledger completeness)

| # | Problem | Raised | Born | Final status |
|---|---------|--------|------|--------------|
| P47 | Cross-tenant 409 test vacuous on InMemory (index not enforced) | v2 INFO-1 | bolt tests | verified (PostgresPaymentFactory; proven non-vacuous) |
| — | Stale cross-tenant key → 409 accepted consequence | v2 INFO-2 | = **P11** (same problem; INFO-2 is its first record) | wont-fix (v2) → re-raised as v8.REQ-1 → documented |
| P48 | OrderNumberService had no PostgreSQL branch → dev 500s | v3 BUG-6 | pre-existing, exposed by P47's fix | verified |
| P49 | P12's refactor dropped P16's grep-able TODO token | v3 DOC-4 | **fix regression** | verified (restored) |
| P50 | P34's doc edit incomplete — sketch still showed client key | v6 DOC-3 | **fix regression** | verified (3faaae6) |

## The hard cases — rulings a reconciler must reproduce

These are the pairs where naive matching (same file, same theme) gives the wrong answer.
Over-merging any of them inflates overlap and would make the loop stop early:

1. **v5.BUG-1 vs v8.BUG-1 — DISTINCT.** Same catch block, same finding ID string, different
   defect: v5 = catch infers cause too broadly; v8 = the *fix's* PostgreSQL substring matching is
   fragile. Generation N+1 of a chain, not a re-find.
2. **v5.DOC-1 vs v8.BUG-3 — DISTINCT.** Same code fact (two-save free+insert). v5 asserted
   "the doc is wrong about it"; v8 asserted "the behavior itself is a crash-window bug."
   Different defect claims, different fixes (doc edit vs atomicity change).
3. **v5.OBS-3 vs v8.BUG-2 — DISTINCT.** Same recovery path. v5: unobserved/undocumented
   (and explicitly judged the URL rebuild benign). v8: the rebuild violates the
   replay-verbatim invariant. v8 found a defect *inside* what v5 cleared.
4. **v5.OBS-2 vs v8.OBS-1 — DISTINCT.** v5: reserved conflict event never emitted (fixed,
   deliberately scoped to same-caller conflicts, decision recorded and accepted by v6).
   v8: the excluded cross-tenant half. A reconciler with the ledger should say "this
   re-opens the v5-round scoping decision" — which it did, and the re-open won.
5. **v1.QUAL-6 vs v5.QUAL-3 — DISTINCT.** Same code site (controller save). v1: two-save
   round-trips (efficiency; wont-fix as intentional). v5: controller holding `_db` at all
   (layering; deferred as codebase-wide). Different claims, different remedies.
6. **v5.SEC-2 vs v8.SEC-2 — DISTINCT.** Same filter, same ID string, adjacent input-validation
   gaps: length vs trim. v5 found one and missed the other in the same ten lines.
7. **v5.QUAL-4 vs v8.QUAL-3 — DISTINCT (closest call in the set).** Both are test-code
   duplication; v5 named request helpers + factory setup, v8 named the seeded entity graph.
   v8's target is plausibly the accepted leftover of v5's partial fix. Ruled distinct on
   asserted content; a reconciler flagging "possible remainder of P41" would be *better* than
   this label, not wrong.
8. **v1.BUG-5 vs v5.DB-1 vs v8.DB-2 — SAME (P01b).** The one true match. Note v1.BUG-5 was a
   composite: its migration-correctness half was fixed; only the residual recurs.
9. **v8.REQ-1 vs v2.INFO-2 — SAME (P11).** The re-find crossed an *audit/verification*
   boundary, which is why v8 couldn't know: INFO-2 lived only in a resolution file.
10. **v8.DB-1 vs v1's T8 row vs v5.QUAL-5's aside — SAME GAP, but only v8 gave it an ID.**
   ID-less findings (test-gap tables, verification asides, fix-note asides) escape any
   ID-keyed ledger. Rule for the future: **everything gets an ID**, including test-gap rows.

## Fix-residual chains (causal lineage — link, don't merge)

- **Classification:** P04 (no catch) → P05 (catch too broad) → P06 (PostgreSQL substring) + P07 (OrderNumber sibling)
- **Scoping:** P08 (unscoped lookup) → P09 (predicate degeneracy) + P11 (owner-only reclaim, deliberate)
- **Filter:** P12 (duplication → filter created) → P13 (length) → P14 (trim)
- **Conflict observability:** P27 (event missing, fixed scoped) → P28 (excluded half)
- **Dead code / test surface:** P04+P08 fixes → P38, P43, P44; P12's fix → P49; P34's fix → P50

## What this changes in the review-system design

1. **Capture–recapture across sequential audits is invalid** — population open, true identity
   overlap 1 (the README's "~4 shared → ≈56" figure is retracted). The estimator only applies
   to the certification protocol: parallel blinded passes against one frozen commit.
2. **The hidden-population lesson survives and sharpens:** ≥14 of today's known problems sat
   in the code at v5's "approved" — for the minor tier, quiet ≠ clean is now measured, not
   anecdotal. For the serious tier the record stays thin: 1 High, found immediately; two
   Medium-rated problems survived at least one audit unfound (P02, P03).
3. **Fix-generativity is measured: ~a quarter of all problems were created by fixes.** The
   fixer's self-review step earns its cost.
4. **Re-litigation is not waste.** 5 re-raises of recorded decisions; 3 overturned the
   decision correctly (P07, P24, P28). The reconciler must attach prior decisions to
   re-finds — context, not suppression.
5. **Every finding needs an ID**, including test-gap rows and asides, or it escapes the ledger
   (P02 was called "Critical" in v1's test table and still took two more audits to get an ID).

## Scoring a reconciler against this file

Feed it the three raw finding lists (strip this file). Score:

- **Identity recall:** does it produce the P01b group (and P11↔INFO-2 if given resolutions)?
- **Over-merge count (the dangerous error):** any of hard-cases 1–7 merged = serious failure;
  weight these highest.
- **Over-split count (the cheap error):** P01b split apart.
- **Chain awareness (stretch goal):** residual chains linked as lineage without being merged.
- **Ledger use (stretch goal):** re-finds of decided items (P07, P11, P28) annotated with the
  prior decision.
