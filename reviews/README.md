---
type: review-system
status: active
created: 2026-06-18
updated: 2026-06-19
owner: Matei Barba
---

# Multi-Lens Parallel Review System

A reusable, parallelized review harness for any feature branch / PR / bolt. The
goal is **recall** — catch what one reviewer in one sitting would miss — while keeping
the **main agent's context clean and unbiased**: each lens runs in its own isolated
subagent and reports back only its findings. The main agent's job is **synthesis**:
dedupe across lenses, resolve disagreements, rank, and record.

The hard-won lesson driving this design (see *What bolt 035 taught us* below) is that
**a single review pass — even a wide, well-run one — has bounded, stochastic recall.**
You do not catch everything at once; you catch a *sample*. So the system is built around
three ideas, in priority order:

1. **Parallel isolated lenses** — breadth and unbiased convergence within one pass.
2. **Repeated independent passes** — because one pass is a sample, not a sweep.
3. **A saturation-based stop criterion** — stop when independent passes start agreeing,
   not when one narrow pass happens to go quiet.

> Where this is heading: the plan to make this whole loop run itself — including when it
> should stop and how the final plain-English summaries get written — is in
> [self-driving-loop-design.md](self-driving-loop-design.md).

---

## Two loops, not one: Discovery vs Verification

The single most important distinction in this system. A "re-review" is doing one of two
*opposite* jobs, and conflating them is what made bolt 035 take seven rounds:

| | **Discovery** | **Verification** |
|---|---|---|
| Question | "What is wrong with this feature?" | "Did this specific fix hold?" |
| Scope | The **whole feature** (changed files in full + their collaborators + call sites) | The **fix delta** for one finding |
| Bias posture | **Blinded** — lenses forbidden from reading `reviews/` | **Anchored** — reads the finding + the resolution on purpose |
| Breadth | All manifest lenses (see below) | The one or two lenses that own the finding |
| Cost | Expensive — run **rarely** | Cheap — run **per fix** |
| Exit | *Saturation* across independent passes | The reverted-fix test goes red, the applied-fix test goes green |
| May declare the whole feature "approved"? | **Yes** (only a saturated discovery pass may) | **No** — a quiet verification round means "this fix held," *not* "the code is clean" |

The two pull in opposite directions: anchoring *helps* verification (you want to re-check
exactly the claimed fix) and *hurts* discovery (it suppresses the wide search). Keep them
as distinct activities with distinct exit criteria. **A verification round must never be
the thing that stamps a feature done.** This split is the *corrective* drawn from bolt 035,
which did **not** keep them apart — its seven rounds were two discovery audits and five
verification rounds run as if interchangeable.

> ### What bolt 035 taught us
> Bolt 035 (payment idempotency) ran v1→v7 and was declared "loop complete, 0 open." Three
> *independent, blinded* discovery audits of the same **feature** — but **not the same code**:
> each audit ran after earlier rounds' fixes landed (13 of v1's 15 findings were already fixed
> when v5 ran), so the problem population was open, not closed. The audits went:
> **v1 → 15 findings · v5 → 15 findings (near-disjoint from v1) · v8 → 18 findings** (on the
> code v7 called clean). Across all seven prior rounds, only **5** findings were ever raised
> *outside* a full audit (v2: INFO-1/INFO-2 · v3: BUG-6/DOC-4 · v6: DOC-3), and **zero**
> fixes ever had to be reopened.
>
> The reading: **verification worked** (fixes held; the cheap anchored rounds did their
> job). **Discovery did not converge** — each independent audit caught a different ~half of
> the population, and the loop terminated on reviewer quiet, not on code cleanliness. The
> seven rounds were really *two full audits + five verification rounds*, and the iteration
> count came from re-discovering breadth the first audit missed — not from churning bugs.

---

## Why parallel isolated subagents

- **No cross-contamination of bias.** A lens that hasn't seen the other lenses' conclusions
  can't anchor on them. When two isolated lenses independently land on the same finding,
  that convergence is real signal (bolt 035 v8: the SQLite message-substring fragility was
  hit by **5** lenses independently; the dead-code method by 3).
- **Clean main context.** Subagents read whole files and dump excerpts; only their distilled
  findings return. The orchestrator never holds the raw file noise.
- **Throughput.** All lenses run at once instead of serially.
- **Recall is a draw, not a sweep.** Any single finder reads with finite depth and surfaces
  a *sample* of what's there — run the same lens twice with different framing and you get
  partly different findings. Breadth (many lenses) and repetition (many passes) are how you
  push the sample toward the population. Convergence across independent draws is the only
  trustworthy signal that you're near complete.

## The lenses

| Lens | Question it answers | Backing skill | Subagent type |
|------|--------------------|---------------|---------------|
| **Correctness** | What input/state/timing makes this wrong? Concurrency, null paths, off-by-one, removed guards, broken call sites | `/code-review` | Explore ×N finders |
| **Security** | Auth/authz bypass, tenant isolation, injection, secret/PII exposure | `/security-review` | general-purpose + FP filters |
| **PR / requirements** | Does it deliver the claimed scope completely & correctly at the contract level? Doc/comment accuracy, observability | `/review` | Explore |
| **Quality / altitude** | Reuse, simplification, efficiency, right-layer fixes (report-only — never auto-apply during review) | `/simplify` | Explore |
| **Tests & verification** | Build + run tests; enumerate untested failure modes; *test the tests* (below) | `/verify`, `dotnet test` | main agent + Explore |
| **Completeness critic** | What did we *not* look at? Which lens didn't run, which entry point / provider / unchanged collaborator got less scrutiny than the main path? | — | Explore, run last |

Lenses are not chosen ad hoc per round — they're selected from a **manifest** (next
section) so breadth is *designed*, not something that accretes over rounds.

## Choosing the lenses: the manifest

Breadth must be front-loaded. The bolt-035 inefficiency was that v1 ran 5 lenses, v5 added
db-parity + observability + input-validation depth, and v8 added a per-entry-point parity
lens — so the *first* audit kept missing a whole disjoint half that a later, broader audit
found in the **same** code. Fix: map the change's characteristics to required lenses up
front. A standing checklist:

| If the change touches… | …add this lens |
|---|---|
| A DB migration / schema | **DB / migration-parity** (does the migration DDL actually run in tests? dual-provider divergence) |
| A second provider/backend behind one interface (SQLite+Postgres, Stripe+EuPlatesc) | **Per-entry-point / per-provider symmetry** (is the *second* path reviewed as hard as the first?) |
| A new request header / external input | **Input-validation** (trim, length, null, encoding, canonicalization) |
| A new exception type or error path | **Observability** (is the incident-triage signal distinguishable in logs?) |
| Concurrency / idempotency / retries | **Race lens** (TOCTOU, transaction boundaries, isolation level, crash-between-commits) |
| Money / charges / orders | **Security** (tenant isolation, replay, double-charge) at full strength |
| A frontend change | **Accessibility / UX** |

The **completeness critic** runs last on every discovery pass: its only job is to name what
was *under*-reviewed (the unrun lens, the unverified claim, the second code path). What it
names becomes the next pass's work.

## Orchestration flow

```
                 ┌─ Correctness finders (×N)  ─┐
                 ├─ Security  + FP filters     ─┤
  [main agent] ──┼─ PR / requirements          ─┼──► [adversarial verify] ──► [main agent:
  scope,         ├─ Quality / altitude         ─┤     2 skeptics/finding       synthesize ·
  fan out        ├─ DB / parity (if migration) ─┤     confirm/plausible/       dedupe · rank ·
  (whole feature ├─ Tests / coverage           ─┤     refute                   write review.md]
   for discovery)└─ Completeness critic        ─┘                                    │
        │                                                                            │
        └────────── build + run tests (verify) ──────────────────────────────────────┘
```

This fan-out is now prototyped as a **`Workflow` script** (parallel lens stage → adversarial
verify stage → main-agent synthesis); bolt-035 v8 ran it end to end (7 lenses + build/test +
2 skeptics per finding). Run shape:

1. **Scope.** Main agent confirms `HEAD == origin/<branch>`. Save the source diff
   (`git diff main...HEAD -- 'src/**/*.cs' ':!*Designer.cs'`) to a temp file. **For a
   verification pass**, the diff path is what every subagent gets. **For a discovery pass**,
   give finders the diff path *for orientation* but explicitly tell them to open the full
   changed files and search the repo for call sites — interactions with *unchanged* code
   (e.g. bolt-035's OrderNumber `CountAsync` collision and `ResolveUnitPrice` divergence)
   live outside the diff and are missed by diff-only scoping.
2. **Fan out.** Launch the manifest lenses in one parallel batch. Each returns structured
   findings: `file:line · severity · summary · concrete failure/cost · suggested fix · confidence`.
3. **Verify** (in parallel with the read-only lenses): build, run the relevant tests, record
   pass/fail. *A green suite that doesn't exercise the found failure modes is itself a finding.*
4. **Adversarially verify each finding.** Two independent skeptics per finding — one hunts
   for an existing guard that *prevents* it, one tries to *construct* the concrete failing
   trace — yielding Confirmed / Plausible / Refuted (below). This is precision insurance; it
   does not find new bugs, so don't let it crowd out finder breadth.
5. **Synthesize.** Dedupe (same defect+location → one, but *record the convergence count* —
   N lenses agreeing is signal), reconcile disagreements, drop refuted false-positives *with
   a reason*, rank by severity, write `review.md`.
6. **Record.** Append a row to [index.md](index.md), and append the pass's metrics line to
   `reviews/<target>/metrics.jsonl` per [metrics-schema.md](metrics-schema.md) — every pass,
   discovery and verification alike (this feeds the stop-rule/saturation analysis; it cannot
   be reconstructed later).

## Severity scale

- 🔴 **High** — directly exploitable / breaks the feature's core promise / data loss.
- 🟠 **Medium** — real impact under specific-but-realistic conditions.
- 🟡 **Low** — defense-in-depth, edge cases, parity risk.
- ⚪ **Cleanup** — quality only, no behavioral impact.

### Verdicts

A review file's frontmatter carries exactly one verdict:

- `request-changes` — blockers open.
- `approve-with-followups` — no blockers; residual 🟡/⚪ remain.
- `approved` — a saturated discovery pass found nothing new.

**Only a *discovery* pass may emit `approved`.** A verification pass, however green, emits at
most `approve-with-followups`, because "this fix held" is not "the feature is clean" (see
*Two loops*). Bolt-035 v8 — a single fresh discovery pass — correctly landed
`approve-with-followups`, not `approved`, precisely because one pass can't certify saturation.

## Verification model

Findings are kept as **Confirmed** (constructible from the code) or **Plausible** (realistic
state, not proven impossible). **Refuted** findings are dropped but recorded with the reason,
so the same false-positive isn't re-raised next time. Security findings additionally carry a
1–10 confidence; `/security-review` only *reports* ≥8, but this system records 7s too when
they're real (below-bar ≠ false).

### Testing the tests — "green ≠ proven"

The highest-value finding class in bolt 035 (DB-1, BUG-1, BUG-4) was *"474/474 green, but
the production code path is never exercised."* "Green" was true at v7 and meant far less than
it looked. So the tests/verification lens must not stop at a passing run:

- **Coverage of failure modes, not lines.** For each failure mode a finding names, ask:
  *which test goes red if I inject this bug?* If none, the green suite is hiding it.
- **Revert-and-rerun (the cheap mutation test).** The bolt-035 verification rounds proved
  fixes non-vacuously by *reverting the fix and confirming a test went red*. Generalize that
  to discovery: a finding's regression test must fail before the fix and pass after.
- **Provider/parity coverage.** A suite that only proves the dev provider (SQLite) says
  nothing about prod (Postgres). Name what the suite *cannot* reach.

## Recall & convergence: when is a review *done*?

A review is **not** done when one pass goes quiet. One pass is a sample. Convergence is a
property of *agreement across independent samples*, and the system should estimate it rather
than assume it. *(Aspirational — not yet automated; the model the loop is moving toward.)*

- **Run independent blinded discovery passes** (ideally in parallel, with varied lens
  framings / finder seeds) and measure their **overlap**. Two passes that find largely
  *different* sets ⇒ a large hidden population remains ⇒ keep going. Two passes that find
  largely the *same* set ⇒ near-saturation ⇒ stop.
- **Capture–recapture, for the *sign* not the number.** Borrowed from ecology: if pass A
  finds N_A, pass B finds N_B, and they share M, the population ≈ N_A·N_B / M.
  **Correction (2026-07-04, from the hand-labeled ground truth): the bolt-035 audits cannot
  feed this estimator at all.** The three audits ran against three different commits — fixes
  landed between them, removing old problems and creating new ones — so the population wasn't
  closed, and the true ID-level identity overlap is **1**, not the ~4 this section previously
  assumed (see [035-payment-idempotency/overlap-ground-truth.md](035-payment-idempotency/overlap-ground-truth.md);
  the earlier "≈56" figure is retracted). The estimator is only meaningful for **parallel
  blinded passes against one frozen commit** — the certification protocol in
  [self-driving-loop-design.md](self-driving-loop-design.md). The qualitative signal survives,
  measured rather than estimated: v5's commit already contained **≥14** problems that only v8
  later named. Use overlap to decide *whether to keep going*, not to quote a population.
- **Stop criterion:** *K consecutive independent full-breadth passes find nothing new* — not
  "the latest narrow round was quiet." Track the per-pass new-finding count; if it isn't
  decaying, you are not near the fixed point.
- **The discovery curve is the instrument.** Plotting new-findings-per-pass tells you whether
  another pass is worth it. Without it, "approved" measures reviewer attention, not the code.

## Closing the loop: review → fix → resolution → verify

A review isn't done when findings are written — it's done when each finding is **resolved
and verified**. Three artifacts, three roles, modeled on a GitHub review thread (reviewer
comments → author resolves → reviewer verifies):

| Artifact | Author | Mutable? | Role |
|----------|--------|----------|------|
| `review-v<n>.md` | reviewer | **immutable** | findings (with IDs) + verdict, against a commit |
| `resolution-v<n>.md` | **fixer** | living until closed | one entry **per finding ID**: status + how + fix commit |
| `review-v<n+1>.md` | reviewer (re-run) | **immutable** | verifies the resolution against the new commit |

The finding IDs (`BUG-1`, `SEC-1`, `QUAL-3`, …) are the join key across all three. The fixer
**never edits the review file** — it responds in the resolution file. This keeps the
reviewer's point-in-time record intact and separates "what was found" from "what was done
about it."

### Per-finding lifecycle

```
open → in-progress → fixed → verified
                  ↘ wont-fix | deferred | disputed | false-positive
```

- A fixer may set anything **except `verified`**. `verified` is set ONLY by the re-review
  (`review-v(n+1)`) actually re-running the lenses against the fix commit. "The fixer says
  it's fixed" ≠ verified — critical for security/correctness findings.
- `wont-fix` / `deferred` / `disputed` require a rationale in the resolution file so the
  re-reviewer understands the intent (and can push back if the rationale is weak).
- The re-review reopens any finding whose fix doesn't hold, and may add NEW findings
  introduced by the fix (see *bounding fix-generativity*).

### The persistent finding ledger (across blinded passes) — *proposed*

Per-version review IDs are **pass-local and deliberately do not map across passes** — that's
what keeps each blinded discovery pass unbiased (bolt-035 v1's `BUG-1` ≠ v5's ≠ v8's). The
cost is that the system can't, on its own, tell a *re-find* from a *new find*, can't compute
overlap (the saturation signal above), and re-litigates accepted deferrals as if fresh (v8
unknowingly re-raised v7's accepted-deferred Postgres-coverage item as if it were brand new).

Fix *(proposed)*: a single **canonical ledger** per target, maintained by a post-hoc
**reconciler** that maps each blinded pass's findings onto stable cross-pass identities
*after* the passes complete (so blinding is preserved during the search). That ledger gives
you (a) the overlap data for capture–recapture, (b) a memory of known-and-accepted so
deferrals aren't re-argued *blindly*, and (c) a true cumulative recall count. (Caveat from the
labeled data: 3 of the 5 re-raises of already-decided items turned out to be *right* — the ledger
must attach the prior decision to a re-find, not suppress it.) Until built, the synthesizing main
agent does this reconciliation by hand and notes re-finds explicitly — the first hand-built
ledger + eval set is [035-payment-idempotency/overlap-ground-truth.md](035-payment-idempotency/overlap-ground-truth.md).

### resolution-v<n>.md shape

Frontmatter rolls up state (`status: open | in-progress | resolved`, `fixed_commit`, and a
`findings:` map of `{id: {status, commit, note}}`); the body carries a table + a
decisions/rationale section. See [035-payment-idempotency/resolution-v1.md](035-payment-idempotency/resolution-v1.md).

## The fixer agent

The fixer is the counterpart to the review lenses — it runs *after* a review and drives
findings to closed. It can be a dispatched subagent (for hands-off loops) or the main agent.
Its contract:

**Reads:** the latest `review-v<n>.md` (findings) + `resolution-v<n>.md` (current state).
Only these two — it does not re-derive findings.

**Order of work (blocker-first):**
1. Address `blockers` (from the review frontmatter) before anything else.
2. Then remaining 🔴/🟠 by severity; 🟡/⚪ are optional/batchable.
3. For each: implement the fix following the repo's TDD/skill conventions — **add the test
   that the review said was missing** (e.g. the concurrency / cross-tenant cases), then the
   fix. A finding isn't `fixed` without a regression test that fails before and passes after,
   unless it's a doc/cleanup item.

**May touch:** source + tests for the finding being fixed. **Must not:** edit the
`review-v<n>.md` file; silently change unrelated behavior; mark anything `verified`.

**Bounding fix-generativity.** Fixes create new review surface — and in bolt 035, **2 of the
5** findings ever raised outside a full audit were regressions a *fix* introduced (a refactor
that dropped a grep-able `TODO` token; an incomplete doc edit that left a sketch
self-contradictory). So before handing back, the fixer **self-reviews its own diff** — the
new tests (duplication? magic-number coupling?), new comments (drift? dropped affordances?),
removed guards — with the relevant narrow lens. Cheap, and it catches the regression class
before it costs a whole extra round.

**Writes (per finding):** update its row in `resolution-v<n>.md` → `status`, `commit` (the
SHA that fixed it), and a one-line `note`. For `wont-fix`/`deferred`/`disputed`, record the
rationale in the decisions section.

**Commits:** one focused commit per finding (or per tight group), message referencing the ID,
e.g. `fix(payments): scope idempotency lookup to caller (SEC-1, review 035-v1)`. When all
blockers are addressed and every finding has a terminal status, set the resolution's
top-level `status: resolved` + `fixed_commit`, then **hand back for re-review** (don't
self-verify).

**Then:** the orchestrator runs a **verification** re-review against `fixed_commit` →
`review-v(n+1).md`, flips surviving findings to `verified` (or reopens), and updates
[index.md](index.md). Loop the *verification* until each fix holds; closing the *feature*
still requires a **saturated discovery** pass (see *Two loops* / *Recall & convergence*).

> Invoke via the `/fix-review` skill (codifies this contract), or ad-hoc: "Fix the open
> findings in review 035-v1." Like the review fan-out, keep it cost-aware — batch the cleanup
> items, don't spawn an agent per one-line doc fix.

## How to run

Quick (ad-hoc):
> "Review branch `<name>` with the multi-lens system" → main agent scopes, fans out the
> manifest lenses as parallel subagents, runs the build/test verify, adversarially verifies
> each finding, synthesizes, and writes the review.

Fresh/unbiased re-audit (a discovery pass):
> "Do a fresh unbiased review — don't take the prior vN reviews into account." → every lens
> and verifier is barred from reading `reviews/`; produces the next `review-v<n>` as a
> clean-room audit of the whole feature. This is what surfaces what earlier passes missed.

Automation: the fan-out is prototyped as a `Workflow` script (parallel lens stage →
adversarial-verify stage → main-agent synthesis). The remaining gaps toward a one-command
review are the **reconciler** (cross-pass ledger / overlap) and an automated **saturation**
check — tracked in [index.md](index.md) backlog.

## Cost discipline

This harness fans out many subagents, and discovery passes are deliberately the expensive
ones — so spend by loop type:

- **Verification passes** are cheap and frequent: give finders the saved **diff** path (don't
  point them at the whole repo — that's the discovery pass's job), prefer Explore, and scale
  finder count to the *fix* size — a 50-line fix doesn't need 8 finders.
- **Discovery passes** are expensive and rare: they review the **whole feature** with the
  full manifest, and you run *several* independent ones to reach saturation. Budget for that
  — completeness is the product here, and one cheap pass demonstrably under-delivers it.
- Estimate before launching large fan-outs. Adversarial verification adds ~2 agents per
  finding; useful for precision, but it's not discovery — don't let it eat the finder budget.

## Conventions

- One folder per reviewed unit: `reviews/<bolt-or-branch-id>/`.
- **Versioned review files:** `review-v<n>.md`, one per review pass. The first pass is
  `review-v1.md`; re-reviewing produces `review-v2.md`, etc. Never overwrite a prior version —
  each pass is a point-in-time record of what was found against which commit. Frontmatter
  carries `version:`, `supersedes:` (`null` for v1), the `commit:` reviewed, and (recommended)
  a `pass-type: discovery | verification` so the index can tell a clean-room audit from a fix-check.
- `index.md` always links the **latest** review version + resolution per target, and carries
  a `Status` column for the resolution loop.
- Each target folder carries a `metrics.jsonl` (one line per pass, append-only) — schema and
  rules in [metrics-schema.md](metrics-schema.md).
- **Resolution files** pair 1:1 with reviews: `resolution-v<n>.md` answers `review-v<n>.md`.
  The fixer writes here; the review file stays immutable.
- Per-PR review/resolution files ride with the code branch. **This `README.md` is the *system*
  design** — a living working spec we refine as more PRs are reviewed.
  [self-driving-loop-design.md](self-driving-loop-design.md) layers on top: it owns the *loop
  automation* rules (stop/certification protocol, calibration experiments, tool build order) and
  defers to this file for all review mechanics. On any overlap, this README wins. Once the approach has
  been stress-tested across several reviews, the matured theory (the saturation model, the
  reconciler, the manifest) graduates to the `analysis/architect-review` branch as connected
  concept notes for building the review agentic-system properly. Until then it stays here and
  evolves; do **not** push it to that branch prematurely.
- Don't auto-apply fixes during a review. Review produces findings; fixing is a separate,
  explicit step (the fixer agent), and verification is a third (re-review).
```
