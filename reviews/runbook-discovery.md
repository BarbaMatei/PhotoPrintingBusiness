---
type: runbook
for: discovery · delta-discovery · certification passes
updated: 2026-07-24
---

# Discovery runbook (full · delta · certification)

Blinded search for defects — the whole feature (full/certification) or the cumulative fix diff
since the last full pass (delta). Checking a specific fix is a different job:
[runbook-verification.md](runbook-verification.md).

**Blinding is best-effort today:** prompts bar lenses from `reviews/` and git history, but no
tool verifies it (the blinding auditor is unbuilt — see
[self-driving-loop-design.md](self-driving-loop-design.md)). Keep `reviews/` content out of
anything a lens receives; commit messages and test names are an accepted leak.

## Before the script

1. **Scope.** Confirm `HEAD == origin/<branch>`. Save diff(s) to temp files — backend
   (`git diff main...HEAD -- 'src/**/*.cs' ':!*Designer.cs'`), frontend separately if touched.
   Delta pass: diff since the last full discovery pass instead. Certification: freeze the
   commit; a feature's **first** certification attempt runs **two** of these passes in
   parallel, a re-certification after a small independently-verified fix round runs **one**
   ([README note ²](README.md#the-router)) — either way folding in any still-owed manifest
   lenses.
2. **Pick lenses** from the manifest below. Delta: the lenses owning the fix classes +
   correctness + completeness-critic, **max 5** (script-enforced).
3. **codePack — optional.** Default is targeted per-lens reading (each lens reads what its
   question needs, guided by the diff). Build a pack only when most lenses need the same small
   file set: changed files in full + collaborators trimmed to their relevant members, **≤50k
   tokens**, **never anything under `reviews/`**; pass its path as `args.codePackPath`.
   Collaborators matter either way: discovery-critical defects live in *unchanged* code.
4. **Build + run both suites yourself**; record pass/fail. A green suite that can't reach a
   named failure mode is itself a finding — feed it to the tests lens.
5. **decidedFindings.** Pull the terminal-status rows from `reviews/<target>/ledger.md` as
   `[{dId, title, file, status, decision}]`. Blinding holds: only the post-lens dedup agent
   sees them.

## Lens manifest

Core lenses on every full pass:

| Lens | Question | Backing |
|---|---|---|
| Correctness | What input/state/timing makes this wrong? | `/code-review`, Explore finders |
| Security | Authz bypass, tenant isolation, injection, secret/PII exposure | `/security-review` |
| Requirements | Delivers the claimed scope, at the contract level? | `/review` |
| Quality / altitude | Reuse, simplification, right layer — **report-only, never auto-apply** | `/simplify` |
| Tests & verification | Untested failure modes; test the tests | main agent + Explore |
| Completeness critic | What did we *not* look at? Runs **last** | Explore |

Added by what the change touches:

| Change touches… | Add lens |
|---|---|
| DB migration / schema | DB / migration-parity (does the DDL run in any test? provider divergence) |
| Second provider behind one interface | Per-provider / per-entry-point symmetry |
| New request header / external input | Input-validation (trim, length, null, encoding) |
| New exception type / error path | Observability (distinguishable triage signal?) |
| Concurrency / idempotency / retries | Race (TOCTOU, transaction boundaries, crash windows) |
| Money / charges / orders | Security at full strength |
| Frontend change | Accessibility / UX |

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
2. Write `review-v<n>.md` (immutable; frontmatter `pass-type`; record each finding's
   convergence count) and `findings-v<n>.md` (full per-finding detail).
3. Map this pass's `F#` onto ledger `D#` rows with the **`reconcile-findings` skill** (scored
   against the 035 ground truth before trust — see its Scores section) and update the ledger.
4. Append the [metrics.jsonl](metrics-schema.md) line (v2: include the per-finding
   `findings[]` array) and the one-line [index.md](index.md) row, then run
   `node reviews/lib/records-auditor.mjs <target>` — it must exit clean.
5. Write `summary-v<n>.md` via the **`owner-summary` skill** — the page the owner reads; the
   review file is the record, the summary is the interface.
6. Feedback edges: a v1 pass's severity-weighted new-finding count is the bolt-process KPI;
   a finding class now seen in ≥2 targets gets a `definition-of-done.md` line.
7. Verdict caps: a delta pass gates *to* certification, never certifies; only a
   certification-grade close emits `approved`.
