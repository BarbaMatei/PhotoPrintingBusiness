---
type: runbook
for: discovery · delta-discovery · certification passes
updated: 2026-08-12
---

# Discovery runbook (full · delta · certification)

Blinded search for defects — the whole feature (full/certification) or the cumulative fix diff
since the last full pass (delta). Checking a specific fix is a different job:
[runbook-verification.md](runbook-verification.md).

**Blinding is best-effort today:** prompts bar lenses from `reviews/` and git history, but no
tool verifies it (the blinding auditor is unbuilt — see
[self-driving-loop-design.md](../notes/self-driving-loop-design.md)). Keep `reviews/` content out of
anything a lens receives; commit messages and test names are an accepted leak.

## Before the script

1. **Scope.** Confirm `HEAD == origin/<branch>`. Save diff(s) to temp files — backend
   (`git diff main...HEAD -- 'src/**/*.cs' ':!*Designer.cs'`), frontend separately if touched.
   Delta pass: diff since the last full discovery pass instead. Certification: freeze the
   commit; a feature's **first** certification attempt runs **two** of these passes in
   parallel, a re-certification after a small independently-verified fix round runs **one**
   ([README note ²](../README.md#the-router)) — either way folding in any still-owed manifest
   lenses.
2. **Pick lenses** from the manifest below. Delta: the lenses owning the fix classes +
   correctness + completeness-critic, **max 5** (script-enforced).
3. **codePack — optional.** Default is targeted per-lens reading (each lens reads what its
   question needs, guided by the diff). Build a pack only when most lenses need the same small
   file set: changed files in full + collaborators trimmed to their relevant members, **≤50k
   tokens**, **never anything under `reviews/`**; pass its path as `args.codePackPath`.
   Collaborators matter either way: discovery-critical defects live in *unchanged* code.
4. **Build + run the suites scoped to the reviewed diff**; record pass/fail. The **full**
   suites run exactly once per loop, at the certification freeze — a certification pass
   runs them (sequential batches per CLAUDE.md); every other pass runs only what the diff
   touches. A green suite that can't reach a named failure mode is itself a finding —
   feed it to the tests lens.
5. **decidedFindings.** Pull the terminal-status rows from `reviews/<target>/ledger.md` as
   `[{dId, title, file, status, decision}]` — the script's field name; `dId` carries the
   row's `PPW-<n>`. Blinding holds: only the post-lens dedup agent sees them.

## Lens manifest

Core lenses on every full pass:

<!-- generated:core-lenses -->
| Lens | Question | Backing |
|---|---|---|
| Correctness | What input/state/timing makes this wrong? | `/code-review`, Explore finders |
| Security | Authz bypass, tenant isolation, injection, secret/PII exposure | `/security-review` |
| Requirements | Delivers the claimed scope, at the contract level? | `/review` |
| Quality / altitude | Reuse, simplification, right layer — **report-only, never auto-apply** | `/simplify` |
| Tests & verification | Untested failure modes; test the tests | main agent + Explore |
| Completeness critic | What did we *not* look at? Runs **last** | Explore |
<!-- /generated:core-lenses -->

Added by what the change touches:

<!-- generated:added-lenses -->
| Change touches… | Add lens |
|---|---|
| DB migration / schema | DB / migration-parity (does the DDL run in any test? provider divergence) |
| Second provider behind one interface | Per-provider / per-entry-point symmetry |
| New request header / external input | Input-validation (trim, length, null, encoding) |
| New exception type / error path | Observability (distinguishable triage signal?) |
| Concurrency / idempotency / retries | Race (TOCTOU, transaction boundaries, crash windows) |
| Money / charges / orders | Security at full strength |
| Frontend change | Accessibility / UX |
<!-- /generated:added-lenses -->

## Launch

```
Workflow({ scriptPath: 'reviews/lib/discovery-review.wf.js',
  args: { target, repoRoot, scope, changedFiles, backendDiff, frontendDiff?, specDocs?,
          lenses, codePackPath?, decidedFindings, passType? } })   // passType: 'delta' for delta
```

- Launch from an Opus session — Fable's session limit has killed runs mid-fan-out.
- No raw newlines inside arg strings; use ` || ` / ` · ` separators.
- Mid-run death → `Workflow({ scriptPath, resumeFromRunId })`: completed agents return from
  cache. Never relaunch from scratch.
- The script **aborts before fan-out** if neither a diff nor a pack binds — fix the args;
  `allowBare: true` only for deliberate free exploration.

## What the script does (do NOT redo)

- Fans the lenses out in one blinded parallel batch.
- **Dedups** into canonical findings with a convergence count; marks findings whose topic a
  shared prompt hint planted (`hinted` — no convergence discount for those).
- **Adversarial verify, trace-first:** ≥3 non-hinted lenses agree → `confirmed`, no skeptic
  (agreement is the precision signal); everything else gets **one trace-constructor**, and a
  guard-hunt only if no trace builds — the two can never contradict. Delta passes additionally
  skip 🟡 entirely (`unverified-low`), and a `tokenBudget` (default 600k) skips remaining
  skeptics once exceeded. ⚪ never get skeptics.
- **Skeptic models:** 🔴/🟠 checks run on the session model (Opus); 🟡 checks run on Sonnet
  (~5× cheaper, validated by replay); ⚪ never get skeptics — Haiku is designated if that ever
  changes.
- **Decided re-raises skip skeptics:** groups matched to a `decidedFindings` entry get verdict
  `re-raise` with the prior decision attached — existence was settled; the synthesizer
  re-judges only the decision. A match never suppresses the find.
- Output caps on every agent. The `_canonical` summary line reports per-stage agent counts —
  copy them into the metrics entry.

Verdict vocabulary: `confirmed` · `plausible` · `refuted` · `re-raise` ·
`unverified-cleanup` / `unverified-low` / `unverified-over-budget` (= lens verdict
unchallenged, not refuted). `disputed` appears only in records older than trace-first.

## After the script (synthesis)

1. Findings arrive deduped, convergence-counted, verdicted — don't re-verify or re-dedup.
   Drop `refuted` with a stated reason; sanity-check `plausible` and high-convergence calls;
   rank by severity.
2. **Reconcile first.** Match this pass's finds onto the ledger's rows with the
   **`reconcile-findings` skill** (scored against the 035 ground truth before trust — see its
   Scores section). Each **new** defect is minted the next `PPW-<n>` from
   [id-counter](../state/id-counter) — the skill writes the incremented counter in the same change —
   and gets its ledger row *and* its detail block
   ([templates/ledger.md](../templates/ledger.md), [doc-contracts.md](../rules/doc-contracts.md)) — the
   block is the defect's only full description, ever. It carries What / Evidence / Suggested
   fix / History; for **serious** findings the Suggested-fix lines include what the fix round
   would otherwise re-derive:
   - **Fix brief** — files:lines (the trace skeptic's `filesTouched`, or your own recheck
     for convergence-confirmed findings), the traced failing path, a suggested
     regression-test shape (`testShape`) — since 2026-08-28 that shape is the **assertion
     spec**: the fixer writes the test to its words and the test-meaning audit checks the
     test against them, so state the assertions, not a vibe — and the trigger
     classification, written exactly as `**Trigger-list-shaped:** yes (<why>)` or
     `Not trigger-list-shaped (<why>)` (the list lives in the `/fix-review` skill; the
     auditor matches the `yes` marker mechanically, so the wording is load-bearing).
     The brief's file paths feed the auditor's overlap detection for protocol clusters —
     name every real file, or two findings on one surface will not be seen to share it.
   - **Approach pre-check** — for trigger-list-shaped suggested fixes, dispatch the
     adversarial approach-check NOW, in parallel, in the background (~20–30k output-token
     cap each, same posture as skeptics: this pass's findings + the code, nothing from
     `reviews/`) and keep writing records while they run. Record the verdict as a History
     line: `Approach pre-check: cleared | revised (how) | refuted (why)`. The fix round
     consumes `cleared`/`revised` and only re-checks deviations. A wrong suggested fix dies
     here instead of anchoring the fixer. Delta passes run these inside their existing token
     budget; when skipped, say so in the History line.
   Re-finds get one appended History line, never a re-description.
3. Write `review-v<n>.md` from [templates/review.md](../templates/review.md) — after
   reconciliation, so its one `ID` column carries each finding's `PPW-<n>`. Lean: ranked table, refuted
   table, notes for the fixer; defects are referenced, never re-described. Immutable once
   the round's doc gate passes. **There is no findings file** (retired 2026-08-10).
4. Append the [metrics.jsonl](../rules/metrics-schema.md) line (v3: include the per-finding
   `findings[]` array, `runtime: {started, ended}` from the loop-driver's worklog stamps,
   and count the pre-checks under `cost.agents_by_stage.approach_checks`; v4: every
   fix-caused entry carries `seed_round` — the fix round whose commits it is attributed
   to, the reconciler's judgment — and `area`, its backlog area word; `null` when
   unattributable, never guessed — the router refuses certification on a missing value as
   "unmeasured" rather than reading it as zero) and the one-line
   [index.md](../state/index.md) row, then run
   `node reviews/lib/records-auditor.mjs <target>` — it must exit clean.
5. Write `summary-v<n>.md` via the **`owner-summary` skill**, from
   [templates/summary.md](../templates/summary.md) — the page the owner reads; the review file
   is the record, the summary is the interface.
6. Feedback edges: a v1 pass's severity-weighted new-finding count is the bolt-process KPI;
   a finding class now seen in ≥2 targets gets a `definition-of-done.md` line.
7. Verdict caps: a delta pass gates *to* certification, never certifies; only a
   certification-grade close emits `approved`.
