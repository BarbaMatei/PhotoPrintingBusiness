---
type: review-system
status: active
created: 2026-06-18
owner: Matei Barba
---

# Multi-Lens Parallel Review System

A reusable, parallelized review harness for any feature branch / PR / bolt. The
goal: maximize *recall* (catch what one reviewer in one sitting would miss) while
keeping the **main agent's context clean and unbiased** — each lens runs in its
own isolated subagent and reports back only its findings. The main agent's job is
**synthesis**: dedupe across lenses, resolve disagreements, rank, and record.

## Why parallel isolated subagents

- **No cross-contamination of bias.** A lens that hasn't seen the other lenses'
  conclusions can't anchor on them. When two isolated lenses independently land on
  the same finding, that convergence is real signal (e.g. bolt 035: security +
  test-coverage + correctness all independently flagged the tenant-isolation gap).
- **Clean main context.** Subagents read whole files and dump excerpts; only their
  distilled findings return. The orchestrator never holds the raw file noise.
- **Throughput.** All lenses run at once instead of serially.

## The lenses

| Lens | Question it answers | Backing skill | Subagent type |
|------|--------------------|---------------|---------------|
| **Correctness** | What input/state/timing makes this wrong? Concurrency, null paths, off-by-one, removed guards, broken call sites | `/code-review` | Explore ×N finders |
| **Security** | Auth/authz bypass, tenant isolation, injection, secret/PII exposure | `/security-review` | general-purpose + FP filters |
| **PR / requirements** | Does it deliver the claimed scope completely & correctly at the contract level? Doc/comment accuracy, observability | `/review` | Explore |
| **Quality / altitude** | Reuse, simplification, efficiency, right-layer fixes (report-only — never auto-apply during review) | `/simplify` | Explore |
| **Tests & verification** | Build + run tests; enumerate untested failure modes | `/verify`, `dotnet test` | main agent + Explore |

Add or drop lenses per change. A DB migration adds a **DB/parity** lens; a
frontend change adds an **accessibility/UX** lens.

## Orchestration flow

```
                 ┌─ Correctness finders (×N)  ─┐
                 ├─ Security  + FP filters     ─┤
  [main agent] ──┼─ PR / requirements          ─┼──► [main agent: synthesize]
  scope diff,    ├─ Quality / altitude         ─┤     dedupe · resolve conflicts
  save to file   └─ Tests / coverage           ─┘     rank · write review.md
        │                                                      │
        └────────── build + run tests (verify) ───────────────┘
```

1. **Scope.** Main agent confirms `HEAD == origin/<branch>`, saves the source diff
   (`git diff main...HEAD -- 'src/**/*.cs' ':!*Designer.cs'`) to a temp file and
   passes that **path** to every subagent (avoids re-diffing in each).
2. **Fan out.** Launch all lenses in one message (parallel). Each returns
   structured findings: `file:line · severity · summary · concrete failure/cost`.
3. **Verify** (in parallel with the read-only lenses): build, run the relevant
   tests, record pass/fail. A green suite that doesn't exercise the found failure
   modes is itself a finding.
4. **Synthesize.** Dedupe (same defect+location → one), reconcile disagreements
   (e.g. "is the null-secret fall-through a bug or intended recovery?"), drop
   refuted false-positives with a reason, rank by severity, write `review.md`.
5. **Record.** Append a row to [index.md](index.md).

## Severity scale

- 🔴 **High** — directly exploitable / breaks the feature's core promise / data loss.
- 🟠 **Medium** — real impact under specific-but-realistic conditions.
- 🟡 **Low** — defense-in-depth, edge cases, parity risk.
- ⚪ **Cleanup** — quality only, no behavioral impact.

## Verification model

Findings are kept as **Confirmed** (constructible from the code) or **Plausible**
(realistic state, not proven impossible). **Refuted** findings are dropped but
recorded with the reason, so the same false-positive isn't re-raised next time.
Security findings additionally carry a 1–10 confidence; `/security-review` only
*reports* ≥8, but this system records 7s too when they're real (below-bar ≠ false).

## Closing the loop: review → fix → resolution → verify

A review isn't done when findings are written — it's done when each finding is
**resolved and verified**. Three artifacts, three roles, modeled on a GitHub review
thread (reviewer comments → author resolves → reviewer verifies):

| Artifact | Author | Mutable? | Role |
|----------|--------|----------|------|
| `review-v<n>.md` | reviewer | **immutable** | findings (with IDs) + verdict, against a commit |
| `resolution-v<n>.md` | **fixer** | living until closed | one entry **per finding ID**: status + how + fix commit |
| `review-v<n+1>.md` | reviewer (re-run) | **immutable** | verifies the resolution against the new commit |

The finding IDs (`BUG-1`, `SEC-1`, `QUAL-3`, …) are the join key across all three. The
fixer **never edits the review file** — it responds in the resolution file. This keeps
the reviewer's point-in-time record intact and separates "what was found" from "what was
done about it."

### Per-finding lifecycle

```
open → in-progress → fixed → verified
                  ↘ wont-fix | deferred | disputed | false-positive
```

- A fixer may set anything **except `verified`**. `verified` is set ONLY by the
  re-review (`review-v(n+1)`) actually re-running the lenses against the fix commit.
  "The fixer says it's fixed" ≠ verified — critical for security/correctness findings.
- `wont-fix` / `deferred` / `disputed` require a rationale in the resolution file so the
  re-reviewer understands the intent (and can push back if the rationale is weak).
- The re-review reopens any finding whose fix doesn't hold, and may add NEW findings
  introduced by the fix.

### resolution-v<n>.md shape

Frontmatter rolls up state (`status: open | in-progress | resolved`, `fixed_commit`, and
a `findings:` map of `{id: {status, commit, note}}`); the body carries a table + a
decisions/rationale section. See [035-payment-idempotency/resolution-v1.md](035-payment-idempotency/resolution-v1.md).

## The fixer agent

The fixer is the counterpart to the review lenses — it runs *after* a review and drives
findings to closed. It can be a dispatched subagent (for hands-off loops) or the main
agent. Its contract:

**Reads:** the latest `review-v<n>.md` (findings) + `resolution-v<n>.md` (current state).
Only these two — it does not re-derive findings.

**Order of work (blocker-first):**
1. Address `blockers` (from the review frontmatter) before anything else.
2. Then remaining 🔴/🟠 by severity; 🟡/⚪ are optional/batchable.
3. For each: implement the fix following the repo's TDD/skill conventions — **add the
   test that the review said was missing** (e.g. the concurrency / cross-tenant cases),
   then the fix. A finding isn't `fixed` without a regression test that fails before and
   passes after, unless it's a doc/cleanup item.

**May touch:** source + tests for the finding being fixed. **Must not:** edit the
`review-v<n>.md` file; silently change unrelated behavior; mark anything `verified`.

**Writes (per finding):** update its row in `resolution-v<n>.md` → `status`, `commit`
(the SHA that fixed it), and a one-line `note`. For `wont-fix`/`deferred`/`disputed`,
record the rationale in the decisions section.

**Commits:** one focused commit per finding (or per tight group), message referencing the
ID, e.g. `fix(payments): scope idempotency lookup to caller (SEC-1, review 035-v1)`. When
all blockers are addressed and every finding has a terminal status, set the resolution's
top-level `status: resolved` + `fixed_commit`, then **hand back for re-review** (don't
self-verify).

**Then:** the orchestrator re-runs the multi-lens review against `fixed_commit` →
`review-v(n+1).md`, flips surviving findings to `verified` (or reopens), and updates
[index.md](index.md). Loop until the verdict is `approved`.

> Invoke via the `/fix-review` skill (codifies this contract), or ad-hoc:
> "Fix the open findings in review 035-v1." Like the review fan-out, keep it
> cost-aware — batch the cleanup items, don't spawn an agent per one-line doc fix.

## How to run

Quick (ad-hoc, this is what was used for bolt 035):
> "Review branch `<name>` with the multi-lens system" → main agent scopes the
> diff, fans out the lenses as parallel subagents, verifies, and writes the review.

Future automation: encode the fan-out as a `Workflow` script (`pipeline()` over the
lenses, a barrier before synthesis) so it's one command. See the flow above for the
stage shape. Not yet built — see [index.md](index.md) backlog.

## Cost discipline

This harness fans out several subagents per review. Keep finders focused (give them
the saved diff path, not "go read the repo"), prefer Explore for read-only lenses,
and scale the finder count to the change size — a 50-line fix doesn't need 8 finders.
Estimate before launching large fan-outs.

## Conventions

- One folder per reviewed unit: `reviews/<bolt-or-branch-id>/`.
- **Versioned review files:** `review-v<n>.md`, one per review pass. The first pass
  is `review-v1.md`; re-reviewing after fixes produces `review-v2.md`, etc. Never
  overwrite a prior version — each pass is a point-in-time record of what was found
  against which commit. Each file's frontmatter carries `version:` and `supersedes:`
  (the version it follows, or `null` for v1), plus the `commit:` it reviewed.
- `index.md` always links the **latest** review version + resolution per target, and
  carries a `Status` column for the resolution loop.
- **Resolution files** pair 1:1 with reviews: `resolution-v<n>.md` answers
  `review-v<n>.md`. The fixer writes here; the review file stays immutable.
- Per-PR review/resolution files ride with the code branch; this `README.md` (the
  *system* design) is agentic-system architecture and may instead belong on the
  `analysis/architect-review` branch — decide at commit time.
- Don't auto-apply fixes during a review. Review produces findings; fixing is a
  separate, explicit step (the fixer agent), and verification is a third (re-review).
