---
type: design-spec
topic: fix-round speed — deschedule the waits
date: 2026-08-03
status: approved-pending-spec-review
owner: Matei Barba
implements-into: .claude/skills/fix-review, .claude/skills/loop-driver, reviews/metrics-schema.md, reviews/lib, reviews/runbook-discovery.md, reviews/system
---

# Fix-round speed redesign — deschedule the waits

The review loop's stages, gates, severities, and records stay exactly as they are. This
design changes **when** the fix round's mandatory steps run (so its waits overlap its work),
adds the **runtime metric** every process was missing, lets the **review pass pre-answer**
the fix round's two most expensive questions, and adds **Speed** to the system scorecard so
the improvement is comparable against a locked baseline.

## 1 · Problem, measured (2026-08-03)

- The 044-045-v1 fix round runs at **~25 min per serious finding** (~6h+ of session time for
  17 of 23; >8h projected). Earlier rounds ran 4–6 min/finding (043-v7, 015-v5).
- Where the time went, from the round's own records:
  - **Approach-checks ran serially before each mechanism cluster**, at ~95k / ~95k / ~154k
    tokens against a designed ~20k — the fixer idles while each runs.
  - **Micro-reviews gate at cluster end**, then spawn follow-up fix commits (each found 4–5
    real defects; `a054fdd`, `bea8c98`, `3ca89b4`).
  - **~2 `dotnet test` invocations per behavioral finding** (red-proof + green-proof), each
    paying build + startup on a machine that cannot parallelize test runs.
  - **~400 lines of resolution bookkeeping** written by hand, serially.
  - **Mid-round owner stalls** (the F5 capability decision) and one **cancelled fixer** whose
    evidence was lost (F7/F16/F17 reconstructed from commits).
- **Nothing records time.** metrics.jsonl v2 meters pass tokens only; fix rounds write no
  line at all (the v2 "passes only" scope note is superseded by owner decision 2026-08-03).

Goals: fix-round wall-clock 2–3× down on mechanism-dense rounds; every process's runtime
measured (active / blocked-on-owner / idle); zero reduction in rigor — every existing MUST
and MUST NOT survives verbatim.

Non-goals: changing loop stages, router rows, severity rules, or verification independence;
parallel fixer lanes (explicitly deferred until the new metric shows the residual bottleneck
is single-lane thinking time); the prevention sweep (its own standalone document:
`docs/prevention-sweep-idea.md`, future project).

## 2 · Fix-round contract v2 (`fix-review` SKILL.md — replaced sections, per the rule budget)

**Stage 0 — Triage (new, ~15–20 min).** After the existing Inputs step:
1. Read all in-scope findings once. Group into **clusters by owner files** (the informal
   practice, now the unit of work). Order clusters blocker-first, then by highest severity
   they contain — the existing ordering rule applied at cluster level.
2. Classify each finding: trigger-list mechanism fix / behavioral (needs regression test) /
   doc-cleanup. Consume the review's **pre-check verdicts and fix briefs** where present (§5).
3. For trigger clusters with no usable pre-check: draft a 2–3 sentence fix approach.
4. Collect every foreseeable owner decision (capability removals, scope questions,
   wont-fix/dispute intents).

**One owner gate.** All triage-collected decisions are asked **together, once**, at triage
end. A mid-round discovery queues to hand-back unless it blocks a blocker fix. Every gate is
stamped in the worklog (§3) — this is where "I saw the question an hour later" becomes a
measured number.

**Checks fly at triage end.** All still-needed approach-checks dispatch **at once, in the
background** — one agent each, **hard cap ~20–30k output tokens**, brief = the finding + the
drafted approach + its files. The fixer implements no-check clusters while they run and folds
each verdict in on arrival. Deviating later from a checked/pre-cleared approach re-checks
**only if the deviation itself is trigger-list-shaped**.

**Per cluster (replaces the per-finding red→green cycle):**
1. Confirm each finding still exists at the current commit (unchanged step, now batched).
2. Class sweep (unchanged), scoped to the cluster.
3. Write **all** the cluster's regression tests → **one** scoped red run proving they all
   fail (failure lines recorded in the worklog as the red evidence) → implement the fixes →
   **one** scoped green run. Test invocations drop from ~2 per finding to ~2 per cluster.
4. Commits stay **one per finding** (or per tightly-related cleanup group), same message
   format. Tests land in the same commit as their fix, as today.
5. On the cluster's last commit: dispatch its **micro-review immediately** (1 anchored
   Explore agent, the same three questions, over that cluster's diff) in the background and
   start the next cluster. Fold follow-ups in on arrival as a follow-up commit per cluster.

**Test-runner rules (the machine constraint, restated as protocol):** exactly **one** test
process at any moment; runs queue FIFO; every run background-launched so the fixer works
while it executes; filters scoped per CLAUDE.md; one final scoped run over all touched
namespaces before hand-back. Frontend findings: same batching via `--include` filters.

**Hand-back gate:** all micro-reviews returned + follow-ups done + final green + records
rendered (§4) + auditor clean. Everything else in the contract — never `verified`, immutable
review files, resolution semantics, comment rules, inbox rule, rigor scaling for 🟡/⚪ —
unchanged word for word.

**Red-proof caveat (accepted):** within a cluster, reds are proven against pre-cluster code;
if an earlier fix in the same cluster invalidates a later test's premise, the green run
catches it and the fixer adjusts. The verification pass's revert-proof is unaffected.

## 3 · Worklog — `reviews/<target>/worklog.jsonl` (new, append-only, rides with the branch)

One JSON event per line, every event carrying `t` (ISO timestamp). Vocabulary:

```
round-start {round}            triage-done {clusters, checks_needed, gates}
gate-open {reason}             gate-closed {reason}
check-dispatched {cluster}     check-returned {cluster, verdict, tokens?}
test-run {kind: red|green|final, filter, passed, failed, duration_s}
finding {id, status, commit}   micro-review-dispatched|returned {cluster, found}
round-end {round}              pass-launch {pass, type}      pass-records-done {pass}
```

Purpose: (a) **crash-safety** — a cancelled fixer's evidence survives (the F7/F16/F17 loss
becomes impossible); (b) the **source of truth** for rendered records and runtime. Appends
are one `echo >>` — no tooling needed to write it.

**Runtime derivation (stated convention):** `blocked_s` = Σ gate-open→gate-closed spans.
`active_s` = Σ gaps between consecutive non-gate events **≤ 30 min** (larger gaps with no
open gate = nobody at the wheel). `idle_s` = round span − active − blocked. The 30-min cap is
a declared convention, not a claim of precision — set high so the speed metric over-counts
work rather than flattering itself (raised from 15 during implementation: the fixture showed
a real 20-min implementation stretch classifying as idle).

## 4 · Records rendering + metrics schema v3

**`reviews/lib/render-records.mjs` (new, sibling of the auditor):** reads worklog +
resolution frontmatter → refreshes the resolution body table, the index.md status cell, and
appends the metrics fix-round line. The fixer still hand-writes all judgment prose
(decisions, deviations, boundaries). Also kills SF12-class transcription drift for these rows.

**Schema v3** (`metrics-schema.md`; strict for lines dated on/after the day the v3 schema
change lands — earlier lines keep their v2/legacy validation):
- New line type, appended at hand-back by the renderer (amends the v2 rule "the fixer never
  writes here" — fix-round lines are the fixer's renderer's to write; pass lines remain
  synthesis-written):

```json
{"target":"…","round":1,"type":"fix-round","date":"…","base_commit":"…","fixed_commit":"…",
 "findings":{"fixed":0,"wont_fix":0,"deferred":0,"disputed":0,"false_positive":0,"open":0},
 "tests":{"invocations":0,"red_runs":0,"green_runs":0,"final":{"passed":0,"failed":0}},
 "approach_checks":{"pre_cleared_consumed":0,"run":0,"tokens":0},
 "micro_reviews":{"count":0,"follow_up_fixes":0},
 "cost":{"agents":0,"tokens":null},
 "runtime":{"started":"…","ended":"…","active_s":0,"blocked_s":0,"idle_s":0,
            "blocked":[{"reason":"…","s":0}]},
 "notes":"…"}
```

- Pass lines (discovery/delta/verification) gain optional `runtime:{started,ended}`, stamped
  by the loop-driver via worklog `pass-launch` / `pass-records-done` events.
- `cost.agents_by_stage` gains the allowed key `approach_checks` (for review-time pre-checks, §5).
- The v2 scope note ("passes only") is **replaced**: passes and fix rounds are metered;
  roll-ups from this file are labeled accordingly. Corrections rule unchanged.

**Auditor v3 additions:** validate the new line type and runtime fields; cross-check the
fix-round line's `findings` tallies against the resolution frontmatter; warn when a v3
fix-round line has no worklog events backing it.

## 5 · Review-time pre-answering (idea #2 — runbook-discovery synthesis + wf.js)

The discovery pass already pays for the knowledge the fixer re-derives; persist it:

**5a — Fix briefs.** `TRACE_SCHEMA` in `discovery-review.wf.js` gains two fields:
`filesTouched` (array of `file:line`, ≤6) and `testShape` (≤40 words). The trace-skeptic
prompt asks for them (it already walked the path; marginal cost ≈ nothing). Synthesis
persists a **Fix brief** block per serious finding in `findings-v<n>.md`: files:lines, the
traced failing path (already recorded today as trace evidence), suggested regression-test
shape, and the trigger-list classification. Convergence-confirmed findings (no trace ran)
get their brief from the main-agent recheck that already happens.

**5b — Pre-cleared approach-checks.** At synthesis, for serious findings whose *suggested
fix* is trigger-list-shaped, dispatch the adversarial approach-check **during the pass's own
record-writing** (parallel, background, same 20–30k cap) against the suggested fix. Persist
the verdict in the finding's brief: `Approach pre-check: cleared | revised (…) | refuted (…)`.
Count agents/tokens into the pass metrics under `cost.agents_by_stage.approach_checks`.
- The fix round consumes: `cleared` + fixer follows it → no round check. `revised` → follow
  the revision, no new check. `refuted` / absent → triage drafts an approach and the round
  checks it (§2). Evidence this earns its keep: this round the review's own F5 suggestion
  (`url.path`) was impossible on .NET 8 — a pre-check would have caught it before it anchored
  anyone.
- Delta passes: same, inside their existing token budget; skipped checks are noted as absent.
- Blinding unaffected: pre-checks run post-lens at synthesis, see only this pass's own
  findings and the code — the same posture as skeptics. Nothing under `reviews/` enters.

## 6 · Speed — scorecard dimension 11 (`reviews/system/scorecard.md`, new file)

The canonical 11-dimension rubric for all future re-grades: dimensions 1–10 copied verbatim
from system review v1 with their locked grades (2026-07-29, evidence: that review file);
dimension 11 **Speed**, graded on the metrics v3 runtime data:

| Grade | Anchor (median active min per serious finding fixed, last two metered rounds) |
|---|---|
| 10 | ≤5, blocked <10% of round span, idle ≈ 0 between round start and hand-back |
| 8 | ≤8 |
| 6 | ≤12 |
| 4 | ≤18 |
| 2 | ≥25, or runtime unmeasured |

**Baseline: 2/10, dated 2026-08-03** — evidence: the 044-045-v1 round timeline (~25 min per
serious finding, checks at 95–154k tokens, stalls unmeasured). Re-grades append dated columns
to the scorecard's grades table, each cell linking its evidence; grades are never edited in
place. Pass-level speed is recorded (runtime stamps) but not graded until data accumulates.

## 7 · Loop-driver stamping (`loop-driver` SKILL.md, small addition)

Step 3 appends `pass-launch` before executing; step 4 appends `pass-records-done` after the
auditor exits clean. Router exit-2/3 questions append `gate-open` when relayed; the next
invocation that consumes the answer appends `gate-closed`. Fix-round rows: the driver already
delegates to `/fix-review`, which owns its own events.

## 8 · Pilot and re-grade protocol

- **Pilot:** the six still-open 044-045-v1 serious findings (F8, F9, F14, F15, F18, F20 —
  clusters E and F) run under contract v2. No briefs/pre-checks exist for them (the v1 pass
  predates this design) — triage drafts approaches; the metrics note marks the line `pilot`.
- **Re-grade:** after two metered fix rounds (or 044-045's loop closing, whichever first),
  re-grade **Speed** plus the dimensions this design touches — cost efficiency, autonomy,
  self-measurement — against the locked baselines, in a dated scorecard column with evidence
  links.
- **Expected effect (to be verified, not asserted):** mechanism-dense rounds ~8h → ~3–4h;
  round token cost flat or slightly down (check caps save ~200k+/round; per-cluster
  micro-reviews and pre-checks add back less).

## 9 · Implementation inventory

| File | Change |
|---|---|
| `.claude/skills/fix-review/SKILL.md` | Contract v2: triage stage, single gate, background checks with cap, per-cluster test batching, pipelined micro-reviews, worklog protocol, renderer at hand-back. Replaced sections, no stacked exceptions |
| `.claude/skills/loop-driver/SKILL.md` | Worklog stamps (§7) |
| `reviews/metrics-schema.md` | v3: fix-round line type, runtime fields, `approach_checks` stage key, replaced scope note |
| `reviews/lib/render-records.mjs` | New: worklog+resolution → body table, index cell, metrics line |
| `reviews/lib/records-auditor.mjs` | v3 validation + fix-round↔resolution cross-check + worklog presence warning |
| `reviews/lib/discovery-review.wf.js` | `TRACE_SCHEMA` + trace prompt: `filesTouched`, `testShape` |
| `reviews/runbook-discovery.md` | Synthesis: persist fix briefs; dispatch + record pre-checks |
| `reviews/README.md` | Conventions: worklog file; metrics note now covers fix rounds |
| `reviews/system/scorecard.md` | New: 11-dimension rubric + grades table (baseline column) |

Note: `fix-review` and `loop-driver` SKILL.md carry uncommitted working-tree edits from the
in-flight 044-045 round — implementation rebases on whatever that session lands.

Standards are descriptive: every file above that states current behavior is updated in the
same change that changes the behavior. CLAUDE.md needs no edit (the test-run rule is obeyed,
not altered).
