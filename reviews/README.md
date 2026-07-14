---
type: review-system
status: active
created: 2026-06-18
updated: 2026-07-14
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

## Which pass comes next (the router)

**The pass type is never a human decision.** It is derivable from the state of
`reviews/<target>/`, so the owner's standing instruction is one sentence — *"Continue the review
loop for `<target>` per reviews/README.md"* — and the orchestrator picks the first matching row:

| If the target's current state is… | …the next step is |
|---|---|
| No `review-v1.md` yet | **Full discovery** pass (v1) |
| Latest review has open findings and no `resolved` resolution answering it | **Fix round** (`/fix-review`) |
| Latest resolution is `resolved` but not yet re-reviewed | **Verification** pass |
| Verification clean (0 reopened) and no delta pass since that fix round | **Delta discovery** |
| Latest delta pass quiet (no new 🔴/🟠) | **Certification**: freeze the commit, two parallel blinded full passes |
| Certification pair quiet | **Certified** — the only path to `approved`; loop done |
| Any pass raised new serious findings | Back to **Fix round** (the counter resets) |

Two guard rails: before launching anything at discovery scale (full, delta, or certification),
the orchestrator states the chosen pass type and expected cost in one line — and **certification
(~2× full-pass cost) always waits for an explicit owner go-ahead**. Whether a change warrants the
full loop at all (a doc tweak does not) is the *entry policy* in
[self-driving-loop-design.md](self-driving-loop-design.md).

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

### The middle tier: delta discovery

*(Added 2026-07-14, from the 042 cost data.)* After a fix round, the population of new defects
lives almost entirely in the **fix diff** — 042-v4's headline mediums (M1/M2) came from the v1
BUG-3 key change, and v6's (D61/D62/D75) from the v4 fixes — yet the only blinded instrument was
the ~2M-token whole-feature pass, which re-audited everything to find bugs sitting in the code
that had just changed. The **delta-discovery pass** is the middle instrument:

| | **Delta discovery** |
|---|---|
| Question | "What did the work since the last full pass break or introduce?" |
| Scope | The **cumulative diff since the last full discovery pass** + collaborators of the changed lines |
| Bias posture | **Blinded** like discovery (no `reviews/`); pass `passType: 'delta'` so lenses scope to the delta |
| Lenses | The lenses owning the fix classes + `correctness` + `race` + `completeness-critic` (~6–7, not the full manifest) |
| Cost | ~400–600k tokens — vs ~2M for a full pass |
| May declare the feature approved? | **No** — a quiet delta pass is the gate **to** certification, never certification itself |

**When each tier runs:** full discovery only at the ends — the first pass on a bolt, and the
two-parallel-pass certification at the close ([self-driving-loop-design.md](self-driving-loop-design.md)).
After every fix round: verification (unchanged) → delta discovery. Repeat fix → verify → delta
until a delta pass is quiet, then freeze and certify. What a delta pass structurally cannot see —
original-population defects outside the fix surface, like 042's D85 (the SplitQuery mis-paging
bug, found only on the *third* full pass) — is exactly what the certification pair exists to
catch: the delta tier replaces the middle full passes, not the safety net.

Metrics: record `type: "delta-discovery"`. The saturation / decay curve is computed on **full**
discovery passes only.

---

## Why parallel isolated subagents

- **No cross-contamination of bias.** A lens that hasn't seen the other lenses' conclusions
  can't anchor on them. When two isolated lenses independently land on the same finding,
  that convergence is real signal (bolt 035 v8: the SQLite message-substring fragility was
  hit by **5** lenses independently; the dead-code method by 3).
  **Caveat — convergence is only as independent as the prompts.** Every lens shares the same
  base context (project hints like "tests use InMemory, so migration DDL is not exercised").
  Agreement on a topic a shared hint planted is manufactured, not independent: the dedup agent
  marks such findings `hinted`, and they don't get the ≥3-convergence skeptic discount (measure #2).
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
  [main agent]         ┌─ lens ─┐    [dedup:          [verify:            [main agent:
  scope · codePack ───►│  ×N    │───► same defect ───► skeptics tiered ──► drop refuted ·
  · pick manifest      └────────┘     across lenses +  by severity &       rank · write
  · build+tests                       convergence]     convergence]        review · record]
        └───────────────── lens → dedup → verify all run inside the Workflow script ─────────┘
```

The fan-out is a committed **`Workflow` script**: [lib/discovery-review.wf.js](lib/discovery-review.wf.js).
Everything below in this section is the **discovery-pass runbook** — a verification pass never runs
the script or the manifest; its much cheaper runbook is [further down](#verification-pass--the-runbook).
The steps are **strict — follow them in order**; don't improvise the parts the script owns.

### Discovery: the main agent MUST do (before and after the script)

1. **Scope.** Confirm `HEAD == origin/<branch>`. Save the source diff(s) to temp files
   (`git diff main...HEAD -- 'src/**/*.cs' ':!*Designer.cs'`; a second for `src/PhotoPrint.UI/**`
   if the branch touches the frontend). Decide **discovery** vs **delta-discovery** vs
   **verification** pass (see *Two loops* and *The middle tier*); for verification, stop here and
   follow the [verification runbook](#verification-pass--the-runbook) instead; for delta, the
   diffs cover the work **since the last full discovery pass**, not `main...HEAD`.
2. **Assemble the `codePack` (measure #4).** Concatenate the changed files **and their key
   collaborators** (callers, cleanup/background jobs, middleware, config) into a **scratch file**
   (filename headers + contents — a small PowerShell loop, so the orchestrator's own context stays
   clean) and pass its path as `args.codePackPath`; each lens reads it once instead of re-reading
   the same files ~100×. (Inline `args.codePack` still works, but was skipped as "impractical" on
   every real run — the path form is the standard.) Include the collaborators because
   discovery-critical defects live in *unchanged* code (bolt-035's OrderNumber `CountAsync`
   collision lived outside the diff). Omit the pack only if you deliberately want lenses to
   explore fresh — but then say so. The pack goes to **lenses only** — a skeptic checks one
   finding and reads its file(s) directly, so pack × skeptic-count multiplication is gone by
   construction. **Budget the pack:** changed files in full, collaborators trimmed to the relevant
   members, whole pack under ~50k tokens (every lens still reads it). Past the budget, drop
   collaborators to grep hints. Never include anything under `reviews/` (it would break blinding —
   the pack file is one of the blinding auditor's scan targets).
3. **Pick the manifest lenses.** Map the change's characteristics to lenses per the
   [manifest table](#choosing-the-lenses-the-manifest); pass as `args.lenses`. Breadth is
   front-loaded here, not accreted over rounds.
4. **Build + run tests yourself**, record pass/fail for the review. *A green suite that doesn't
   exercise the found failure modes is itself a finding* (feed that to the tests-coverage lens).
5. **Extract `decidedFindings`, then invoke the script.** Pull the terminal-status rows (deferred /
   wont-fix / false-positive / disputed-upheld) from `reviews/<target>/ledger.md` as
   `[{dId, title, file, status, decision}]` — blinding holds because only the post-lens dedup agent
   ever sees them (measure #5). Then: `Workflow({ scriptPath: 'reviews/lib/discovery-review.wf.js',
   args: { target, repoRoot, scope, changedFiles, backendDiff, frontendDiff?, specDocs?, lenses,
   codePackPath, decidedFindings, passType? } })` — `passType: 'delta'` for a delta pass. Launch
   per the [launch checklist](#launch-checklist-discovery--delta-workflow).
6. **Synthesize.** The returned findings are **already deduped, convergence-counted, and verdicted**
   — do NOT re-run verification or re-dedup. Your job: drop `refuted` false-positives *with a reason*,
   sanity-check the `plausible`/high-convergence calls, rank by severity, write `review-v<n>.md`
   (record each finding's convergence count — N lenses agreeing is signal).
7. **Record.** Append a row to [index.md](index.md) and the pass's metrics line to
   `reviews/<target>/metrics.jsonl` per [metrics-schema.md](metrics-schema.md) — every pass, discovery
   and verification alike (it feeds the saturation analysis and can't be reconstructed later).
   Two feedback edges into the dev process: (a) a **v1** discovery pass's severity-weighted
   new-findings count is the dev-process KPI
   (`memory-bank/standards/bolt-process.md`, *Measuring*) — note it in the pass summary; (b) any
   finding **class** now observed in ≥2 targets gets a line in
   `memory-bank/standards/definition-of-done.md`, so the checklist stays empirical rather than
   aspirational.

### The script does automatically (do NOT re-do these)

- **Fans out** the manifest lenses in one blinded parallel batch (whole-feature for discovery).
- **#1 Dedups** all lens findings into canonical findings with a convergence count, via one in-pass
  **dedup agent** — so each real defect is verified once, not once per lens that raised it. (Not to
  be confused with the cross-pass **ledger reconciler**, which matches findings *across* passes and
  is still unbuilt — see *The persistent finding ledger*.) The dedup agent also marks findings whose
  topic the shared prompt hints planted (`hinted: true`) — see the convergence caveat under *Why
  parallel isolated subagents*.
- **#2 Convergence-weighted adversarial verify:** ≥3 lenses agree and not `hinted` → one
  anti-groupthink guard-hunt (escalate to a trace only on a surprising guard claim); 🔴/🟠 at
  convergence 1–2 (or `hinted`) → two independent skeptics; 🟡 → one trace, escalate to a guard-hunt
  only if no trace; ⚪ → none. Verdicts: `confirmed` / `plausible` / `refuted`, plus `disputed` when
  the two skeptics contradict each other (a guard found *and* a failing trace built) and
  `unverified-cleanup` for ⚪ (skeptics skipped).
- **#3 Output caps** on every agent (verdict + brief reason, not essays).
- **#5 Decided re-raises skip skeptics:** groups the dedup agent conservatively matches to a
  `decidedFindings` entry (same root cause, same site — when unsure, no match) get verdict
  `re-raise` with the prior decision attached and run **no skeptics**: existence was settled when
  the item entered the ledger; the synthesizer re-judges only the *decision* (3 of 5 recorded
  re-raises overturned the prior call, so a match never suppresses the find). On 042-v8 numbers —
  15 of 28 findings were re-raises — this removes roughly 40% of the skeptic layer.
- **Aborts before fan-out if args didn't bind** (no diff and no codePack resolved) — the 042-v4
  void run (~1.2M tokens of lenses reviewing placeholder defaults) was this failure. Override with
  `allowBare: true` only when you deliberately want free exploration.
- **Reports skeptic-run counts** (and the decided-re-raise skip count) in the `_canonical` summary
  line — copy them into the pass's metrics entry (`cost.agents_by_stage`).

### Verification pass — the runbook

Anchored, per-fix, cheap — the opposite posture of discovery (see *Two loops*). No blinding, no
manifest, no codePack, no workflow script:

1. Read the latest `review-v<n>.md` + `resolution-v<n>.md`; check out the resolution's
   `fixed_commit`.
2. **Revert-and-rerun every `fixed` finding:** revert the fix, confirm its regression test goes
   red; restore, confirm green. A fix whose test cannot go red is not verified — reopen it.
3. For findings that need judgment rather than a test (doc items, `wont-fix`/`deferred`/`disputed`
   rationales), dispatch one anchored Explore agent per finding — give it the finding, the
   resolution note, and the fix delta, not the whole feature. **Gate deferral re-checks on the
   code actually moving:** first run `git diff <last-affirmed-commit>..HEAD -- <cited file(s)>`
   yourself; unchanged → record "unchanged since `<commit>`, stands" with **no agent** (042 v7+v9
   re-affirmed 42 deferrals by agent, zero flips); changed → dispatch as before. The ledger row
   carries the commit at which each deferral was last affirmed.
4. **Review the fix diffs — three questions, not one.** Per fix cluster, by the owning lens:
   *class or instance* (do sibling sites still carry the defect?); *new surface at the bar*
   (an added mechanism has sized defaults, a signal, failure-mode tests, docs — the fixer's
   rule-2 bar; the resolution's new-surface notes say where to look); *regression* (bolt-035:
   2 of 5 out-of-audit findings were fix-caused). This is the one place a verification pass
   looks beyond its anchor — v5 asked only the regression question, answered "none", and v6
   then mined the same diffs for ~13 findings. Ask all three.
5. Write `review-v<n+1>.md` (`pass-type: verification`; verdict at most `approve-with-followups`),
   flip held findings to `verified`, reopen the failures, and append the metrics line.

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

**Only a *full discovery* pass may emit `approved`.** A verification pass, however green, emits at
most `approve-with-followups`, because "this fix held" is not "the feature is clean" (see
*Two loops*). A **delta-discovery** pass is capped the same way — it audits only the diff since
the last full pass, so a quiet delta gates *to* certification, it never certifies. Bolt-035 v8 — a single fresh discovery pass — correctly landed
`approve-with-followups`, not `approved`, precisely because one pass can't certify saturation.

## Verification model

Findings are kept as **Confirmed** (constructible from the code) or **Plausible** (realistic
state, not proven impossible). **Refuted** findings are dropped but recorded with the reason,
so the same false-positive isn't re-raised next time. A finding whose two skeptics *contradict*
each other — a guard found **and** a failing trace built — is kept as **Disputed**: the conflict
is surfaced to the synthesizer (and the round summary) instead of being averaged into
"plausible". ⚪ cleanups skip skeptics entirely and carry `unverified-cleanup`. Security findings additionally carry a
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

**Finding ID format (the standard).** Each pass numbers its findings with a **dumb, pass-local
handle** — `F1, F2, …` in ranked order — and carries `severity` and `category` as **columns, never in
the ID**. Severity is *mutable*: encoding it in the ID (as v4's `M1`/`L1` did) means a later
re-classification either lies or forces a renumber that breaks every reference. Older passes used type
prefixes (`BUG-`/`SEC-`/`QUAL-`); those are grandfathered — **`F#` is the standard going forward.**

**Two keys, two questions.** The pass-local ID (`F#`) is the join key **within** a pass —
`review-v<n>` ↔ `resolution-v<n>`. **Across** passes the join key is the canonical **`D#`** in the
ledger (below), *not* the pass-local ID. The fixer **never edits the review file** — it responds in the
resolution file, keeping the reviewer's point-in-time record intact.

**Nuance the old "IDs join all three" claim missed:** that clean ID join only holds for a
*verification* re-review — it's anchored and reuses the prior pass's IDs. A *discovery* re-review is
blinded and mints fresh `F#`, so it **cannot** join by ID; its findings are reconciled to prior passes
by `D#` in the ledger, after the fact.

Full per-finding detail (scenario/fix/evidence) for every finding — including the Lows/Cleanups the
review file may list only as one-liners — belongs in a durable `findings-v<n>.md` beside the review, so
nothing survives only in scratch/session-temp (bolt-042 v4 is the first:
[042-thumbnail-cache/findings-v4.md](042-thumbnail-cache/findings-v4.md)).

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

### The persistent finding ledger (across blinded passes)

Per-version review IDs are **pass-local and deliberately do not map across passes** — that's what keeps
each blinded discovery pass unbiased (bolt-035 v1's `BUG-1` ≠ v5's ≠ v8's; bolt-042 v1's `SEC-1` ≠
v4's `M#`). The cost is that the pass IDs alone can't tell a *re-find* from a *new find*, can't compute
overlap (the saturation signal above), and would re-litigate accepted deferrals as if fresh.

**The standard:** one **canonical ledger** per target at `reviews/<target>/ledger.md`. Every real
defect gets a stable **`D#`** that lives forever for that target; each pass's `F#` findings are mapped
onto `D#` by the synthesizer **after** the blinded pass completes — so blinding is preserved during the
search (the finders never see `D#`). The ledger gives you (a) overlap data for the saturation signal,
(b) a memory of known-and-accepted so deferrals aren't re-argued *blindly*, and (c) a true cumulative
recall count. **Caveat (from the labeled data): 3 of 5 re-raises of already-decided items turned out
*right* — the ledger attaches the prior decision to a re-find, it never suppresses it.**
Two mechanics hang off the ledger: its terminal-status rows feed the discovery script's
`decidedFindings` arg (measure #5 — re-raises skip skeptics, prior decision attached), and each
deferred/wont-fix row records the **commit at which it was last affirmed**, which the verification
runbook's deferral gate diffs against.

The synthesizing main agent builds the ledger **by hand today** (a **reconciler** to automate the
`F#`→`D#` mapping is still unbuilt). Worked artifacts: the first hand-labeled eval set is
[035-payment-idempotency/overlap-ground-truth.md](035-payment-idempotency/overlap-ground-truth.md); the
first standing per-target ledger is [042-thumbnail-cache/ledger.md](042-thumbnail-cache/ledger.md) —
whose own caveat is the key lesson: v1 and v4 ran against **different commits**, so their low overlap
**cannot** feed a capture–recapture population estimate (the fixes removed v1's population). Overlap is
only a clean saturation estimator across **parallel blinded passes on one frozen commit**.

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

**Comment discipline.** Keep code comments minimal — **do not narrate the fix in-code** ("fixed
X", "now handles Y", "cache miss: …"). The *why* of a change lives in the commit message and the
`resolution-v<n>.md` note, not scattered through the source where it drifts. Comment only genuinely
non-obvious intent the code can't express (a subtle invariant, a "why not the obvious approach").

**Bounding fix-generativity — the four rules.** Fixes create new review surface, and on
bolt 042 that surface became the dominant loop cost: **~13 of v6's 24 new defects — including
4 of its 5 mediums — trace to earlier rounds' fixes** (the D5→D34/D35/D38→D75 chain is three
generations deep; the M3 limiter alone yielded D61/D68/D69; the L13 mapping shipped without
its bomb event → D62; the stale-doc-token class took four rounds, C4 → V5-2 → F7 → F20).
Since the loop's only exit is a *quiet* discovery pass, a fixer that re-seeds the population
each round is what forces extra ~2M-token discovery passes — fix-created surface must not
wait for the most expensive instrument to inspect it. Four rules, applied per fix; scale by
severity — 🔴/🟠 get all four, 🟡/⚪ get #1, batched doc/cleanup commits skip #4:

1. **Class sweep before implementing.** State the defect class in one sentence, then search
   for sibling sites — the same pattern in code, the same stale value in *every* doc — and
   fix the class, or record in the note why only the instance. For doc drift the unit of fix
   is the stale token repo-wide, never just the file the finding cited.
2. **New-mechanism bar.** A fix that adds a mechanism (a class, catch/mapping, event, limit,
   retry, cache) is a mini-feature and ships at feature grade: defaults/sizing derived from
   the real constraint and stated in the note; an observability hook; tests for the failure
   modes the mechanism itself introduces; every doc that states the old behavior updated.
   D61 (default ignored RAM), D68 (silent saturation), D69 (untested slot release), and D64
   (undocumented HEIC removal) are one round of this rule not existing.
3. **Design-check escalation.** A fix that changes a design — key scheme, concurrency model,
   resource budget, retry semantics — is not a patch: run one adversarial agent against the
   *proposed approach before implementing* (~20k tokens; a race lens reading "temp file +
   `File.Move`" names the move-target race immediately). Both deep 042 chains were designs
   pushed through the patch loop unchecked.
4. **Fresh-eyes micro-review of the fix diff (replaces self-review).** A self-skim is the
   mind that wrote the fix asking itself only the regression question — and it answers
   "none" (resolution-v1/v2's self-reviews did; so did v5's regression-only skim) over a
   diff the next discovery pass then mines for a round of findings. Before hand-back,
   dispatch 1–2 anchored Explore agents (fresh context, one per fix cluster) over the full
   fix diff with exactly three questions: **class or instance? new surface at the bar of
   rule 2? anything adjacent broken?** ~200–400k tokens per fix round, against the ~2M
   discovery pass that otherwise finds it.

**Writes (per finding):** update its row in `resolution-v<n>.md` → `status`, `commit` (the
SHA that fixed it), and a one-line `note`. A mechanism-adding fix's note also names the
**new surface** (the mechanism and its failure modes) — that is where the verification pass
points the owning lens. For `wont-fix`/`deferred`/`disputed`, record the rationale in the
decisions section.

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

### Launch checklist (discovery / delta workflow)

Each of these has already burned a real run:

1. **Launch from an Opus 4.8 session** — three 042-v8 launches on Fable died on its session limit
   with zero lenses done.
2. **Resume, never relaunch:** on any mid-run death, re-invoke with
   `Workflow({ scriptPath, resumeFromRunId })` — completed agents return from cache (042-v1
   resumed 40 dead skeptics with zero errors).
3. **Run from an LF copy** — the Workflow tool rejects the committed CRLF file; and keep newlines
   out of arg strings (use ` || ` / ` · ` separators).
4. **Respect the arg-bind abort:** the script refuses to fan out if neither a diff nor a codePack
   resolved (the 042-v4 void run, ~1.2M tokens over placeholder defaults, was this failure). If it
   aborts, fix the args; `allowBare: true` is only for deliberate free-exploration.

Automation: the discovery fan-out is a committed three-stage `Workflow` script — lenses →
in-pass dedup → convergence-weighted verify — at [lib/discovery-review.wf.js](lib/discovery-review.wf.js)
(operating steps in *Orchestration flow*). The remaining gaps toward a one-command review are
the **ledger reconciler** (cross-pass identity / overlap — a different thing from the script's
in-pass dedup agent) and an automated **saturation** check — tracked in [index.md](index.md)
backlog and owned by [self-driving-loop-design.md](self-driving-loop-design.md).

## Cost discipline

This harness fans out many subagents, and discovery passes are deliberately the expensive
ones — so spend by loop type:

- **Verification passes** are cheap and frequent: give finders the saved **diff** path (don't
  point them at the whole repo — that's the discovery pass's job), prefer Explore, and scale
  finder count to the *fix* size — a 50-line fix doesn't need 8 finders.
- **Delta-discovery passes** sit between the two (see *The middle tier*): blinded manifest-subset
  lenses over the cumulative fix diff, ~400–600k tokens, after every fix round — so full passes
  run only at the ends (first pass + certification).
- **Discovery passes** are expensive and rare: they review the **whole feature** with the
  full manifest, and you run *several* independent ones to reach saturation. Budget for that
  — completeness is the product here, and one cheap pass demonstrably under-delivers it.
- Estimate before launching large fan-outs. Adversarial verification adds ~2 agents per
  finding; useful for precision, but it's not discovery — don't let it eat the finder budget.

### Tiering the adversarial skeptics (what they're actually worth)

Adversarial verification is **precision insurance, not recall** — it drops false positives and
calibrates severity; it does not find new bugs. So spend on it where a *wrong call is expensive*,
and trim the long tail. **Convergence overrides severity:** ≥3 non-`hinted` lenses independently
agreeing is itself the precision signal, so such a finding gets a single anti-groupthink
guard-hunt whatever its severity (measure #2). For the rest:

| Finding severity (convergence 1–2) | Skeptics to run |
|---|---|
| 🔴 High / 🟠 Medium | **Both** — independent guard-hunter *and* trace-constructor (a false blocker wastes fixer time; a missed refutation ships a bad fix) |
| 🟡 Low | **One — trace-constructor only.** If it builds a concrete trace → *confirmed* + you get the failure scenario for the write-up. If it *can't*, escalate to one guard-hunter to decide *refuted* vs *plausible*. |
| ⚪ Cleanup | **None** — accept the lens, or let the fixer judge. |

This cuts the skeptic count by roughly a quarter on a full discovery pass (bolt-042: 98 → ~73
skeptics; convergence-weighting — measure #2 — cuts it further) and materially lowers the odds
of a mid-run stall, while keeping full rigor on everything that could block a merge.

**What the skeptics bought on bolt-042 (the calibration datapoint):** 98 skeptics over 49
non-cleanup findings caught **2 genuine false positives** (~4%) and correctly downgraded **7**
findings to "plausible / not-triggerable-today" (latent cloud-provider + migration-parity) so
they weren't over-ranked as active bugs. The remaining ~40 findings were *corroborated* — added
confidence and a concrete trace, but no new information. The single highest-value skeptic was a
trace-constructor that **ran the real ImageSharp 3.1.11 API** to prove `IdentifyAsync` throws
rather than returning null, refuting a "fail-open" finding two lenses had independently raised —
a non-obvious refutation the synthesizer would likely have accepted. Takeaway: the value is real
but concentrated in (a) findings hinging on a checkable external fact (library/config behavior, a
guard elsewhere) and (b) High/Medium calls; blanket 2×-on-everything over-pays.

### Where the tokens actually go, and how to cut them without cutting recall

*This subsection is rationale — the **why**. The operating steps are in [Orchestration flow](#orchestration-flow)
(what the main agent does) and are enforced by [lib/discovery-review.wf.js](lib/discovery-review.wf.js)
(what the script does automatically).*

On the bolt-042 pass the **12 lenses were ~11% of the agents; the ~98 skeptics were ~89%** and a
similar share of the ~3.5M tokens. So the waste is in the *verification* layer, not the *finding*
layer — cut there, and leave lens breadth alone (breadth is the product; under-provisioning it is the
documented bolt-035 failure). That is why the five baked-in measures — **#1 dedup-before-verify, #2
convergence-weighted verify, #3 output caps, #4 read-once codePack, #5 decided-re-raise skip** — all
trim verification cost or redundant re-reading and none touch how many lenses run. On bolt-042 the bomb guard / leak / TOCTOU
were each found by 5 lenses, i.e. up to 10 skeptic runs per bug before dedup — that is the redundancy
#1/#2 remove. Measure #4 now takes a **file path** (`codePackPath`) — the inline-args form was skipped as
impractical on every real run — and goes to **lenses only**: a skeptic checks one finding and reads
its file(s) directly, so the pack no longer multiplies across the skeptic layer. Step 2 still
budgets the pack (~50k) because every lens reads it.

**Held in reserve (apply only if token use is still too high):** model tiering — Opus for lenses +
High/Med skeptics, Sonnet for the bulk of Low/spot skeptics. Deferred because it carries a small
confidence cost the five above do not; revisit if #1–#5 don't move the number enough. Validation is
now cheap and pre-registered:
[experiments/skeptic-tiering/experiment-design.md](experiments/skeptic-tiering/experiment-design.md)
replays 042-v8's low-tier skeptics on the cheaper model and diffs the verdicts — run it only after
#5 has landed in a real pass (it removes many low-tier runs, shrinking both the cost and the
benefit of tiering).

**Always measure a change like this** the honest way (per *Recall & convergence*): re-run one frozen
commit with and without the change and confirm the lenses still surface the same findings and no
outcome-changing verdict flips. If nothing flips, the saving was free.

## Conventions

- One folder per reviewed unit: `reviews/<bolt-or-branch-id>/`.
- **Versioned review files:** `review-v<n>.md`, one per review pass. The first pass is
  `review-v1.md`; re-reviewing produces `review-v2.md`, etc. Never overwrite a prior version —
  each pass is a point-in-time record of what was found against which commit. Frontmatter
  carries `version:`, `supersedes:` (`null` for v1), the `commit:` reviewed, and a **required**
  `pass-type: discovery | delta-discovery | verification` so the index and the saturation analysis
  can tell a clean-room audit from a delta sweep from a fix-check.
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
