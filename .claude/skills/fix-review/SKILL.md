---
name: fix-review
description: >-
  Drive code-review findings to closure — the fixer half of the multi-lens review
  loop. Use whenever the user wants to fix, address, resolve, act on, or "apply" the
  findings of a review: phrases like "fix the review findings", "address the blockers
  in review X", "resolve the open findings", "apply the review feedback", or right after
  a /code-review or multi-lens review produces a review-v<n>.md + resolution-v<n>.md
  under reviews/. Runs the round descheduled: triage first, one batched owner gate (parked instead when the run is unattended), then
  clusters — approach-checks, test runs, and micro-reviews execute in the background while
  the fixer works. Fixes findings blocker-first WITH the regression tests the review asked
  for, records each fix in the resolution file and the worklog, commits per finding
  referencing its ID, and hands back for re-review. Does NOT re-derive findings, does NOT
  edit the immutable review file, and never marks a finding "verified" (only a re-review can).
---

# Fix Review

The fixer half of the review loop documented in `reviews/README.md`. A review produced
findings; your job is to drive each one to a terminal state and record what you did,
without corrupting the reviewer's point-in-time record — and without ever standing idle
while a check, a test run, or a question does its waiting.

## Where this fits

Three artifacts, three roles (like a GitHub review thread):

```
review-v<n>.md      (reviewer, IMMUTABLE)  — findings with IDs + verdict, at a commit
resolution-v<n>.md  (you, the fixer)       — per-finding: status + commit + note
verification pass   (re-review, no files)  — flips ledger rows to "verified" or reopens
```

Every defect carries one id, `PPW-<n>`, global across all targets and minted at
reconciliation. The review's `ID` column and the resolution's `## Findings` table are both
keyed by it, per `reviews/rules/doc-contracts.md`. You write only in the resolution file and the
worklog. You hand back for re-review — you do **not** declare anything verified.

`reviews/README.md` owns the loop conventions (router, severities, verdicts, file shapes).
**This skill is the sole owner of the fixer contract** — how a fix round runs is defined here
and nowhere else.

## Inputs — locate the work

1. Resolve the **target** from the user's request (a bolt id, branch, or folder under
   `reviews/`). If ambiguous, list `reviews/*/` and ask which one.
2. In `reviews/<target>/`, find the **highest** `review-v<n>.md` and its paired
   `resolution-v<n>.md`. If the resolution file is missing, create it from the review's
   finding list (all `status: open`) before starting — copy `reviews/templates/resolution.md`.
3. Read the review's frontmatter `blockers:` list and its findings table (id, severity,
   location). The defect detail lives on each id's **ledger detail block** — What / Evidence /
   Suggested fix / History; serious findings' Suggested-fix lines carry the **Fix brief**
   (files:lines, traced failing path, suggested test shape, trigger classification) and the
   History an **Approach pre-check** verdict — triage consumes both. Read the resolution to
   see what's already done — never redo a finding already at a terminal status.

Do not re-run the review or invent new findings. If you spot something genuinely new
while fixing, note it in the resolution's decisions section for the re-reviewer; don't
silently fix outside the finding set.

## The contract

**You MAY** edit source + tests for the finding you're fixing, and edit the resolution
file and the worklog.

**You MUST NOT**
- modify any `review-v<n>.md` (it's an immutable record — editing it destroys the audit trail);
- mark any finding `verified` (that status belongs to the re-review — a fixer vouching
  for its own fix is exactly the bias the loop exists to prevent);
- change unrelated behavior, or fix things outside the finding set without recording why;
- create a `reviews/<target>/` folder or ledger for anything — a defect you notice outside
  the finding set is **proposed at this round's owner gate** (immediately, if it is serious),
  and the owner either routes it into `reviews/state/backlog.md` — one row, the next `PPW-<n>` from
  `reviews/state/id-counter`, a one-line What, and a pointer to the commit or resolution recording
  it — or drops it. Write the ruling, either way, into this round's `Decisions`. A drop on
  the owner's word is allowed; a silent drop is not. Only an owner-opened loop creates a
  target folder;
- close a non-trivial bug/security finding without a regression test (see below);
- run more than **one** test process at a time, ever (the machine rule in CLAUDE.md).

## The worklog — write it as you work, not after

Append one JSON line per event to `reviews/<target>/worklog.jsonl` **at the moment the
event happens** (`date -Is` for the timestamp). This is what survives a cancelled session —
the loss that forced three findings' fix records to be reconstructed from commits must stay
impossible — and it is the source the renderer and the runtime metric read. Never edit a
past line.

```bash
echo '{"t":"'$(date -Is)'","ev":"round-start","round":1}' >> "reviews/<target>/worklog.jsonl"
```

| Event | When | Extra fields |
|---|---|---|
| `round-start` / `round-end` | first action / after hand-back summary | `round` |
| `triage-done` | cluster plan written | `clusters`, `checks_needed`, `pre_cleared` (review pre-checks consumed), `gates` |
| `gate-open` / `gate-closed` | before asking the owner / when the answer arrives | `reason` |
| `check-dispatched` / `check-returned` | approach-check out / verdict in | `cluster`, on return `verdict`, `tokens` if known |
| `test-run` | each suite invocation finishes | `kind: red\|green\|final`, `filter`, `passed`, `failed`, `duration_s` |
| `finding` | a finding reaches a status | `id`, `status`, `commit` |
| `micro-review-dispatched` / `micro-review-returned` | per cluster | `cluster`, on return `found` |

## Workflow

### Stage 0 — Triage (one reading, ~15–20 min, before any fix)

1. Read all in-scope findings once (new 🟡/⚪ go to the ledger backlog per the README
   router, not the fix round). Group them into **clusters by owner files** — the unit of
   work from here on. Order clusters blocker-first, then by the highest severity each
   contains (the blocker-first rule, applied at cluster level).
2. Classify each finding: **trigger-list** (see below) / **behavioral** (needs a regression
   test) / **doc-cleanup** (no test). Take the classification from the finding's Fix brief
   when one exists; judge it yourself when not.
3. For each trigger-list cluster, settle its approach: a review-time pre-check verdict of
   `cleared` (and you follow that approach) or `revised` (adopt the revision) needs **no
   new check**; `refuted` or no pre-check means you draft a 2–3 sentence approach now.
4. Collect every foreseeable owner decision — capability removals, scope questions,
   wont-fix/dispute intents.
5. Write the cluster plan as the resolution body's scope table, append `triage-done`.

**The trigger list (unchanged, still mandatory):** a fix that changes a key scheme,
concurrency model, resource budget, or retry semantics, OR adds/converts any of: a
background job / timer / periodic sweep, a cache, a retry/backoff, an event, a limiter, a
catch/mapping layer, a refresh/self-heal or other UI state machine — is a design, not a
patch, and gets an adversarial approach-check BEFORE implementation. This is a trigger
list, not a judgment call: on 043, the two fix clusters that skipped it generated 8
findings across the next two delta passes — ~3M tokens of review to find what the ~20k
check names up front.

### Stage 0b — One owner gate

Ask **all** triage-collected owner decisions together, once, right after triage — stamped
`gate-open`/`gate-closed` with reasons. A decision that surfaces mid-round queues for the
hand-back summary unless it blocks a blocker fix. Never drip questions.

### Stage 0c — Checks fly

Dispatch **every** still-needed approach-check now, in parallel, in the background — one
adversarial agent per cluster (race/resource/frontend lens as fits), prompt = the finding,
your drafted approach, and its files, asked to refute the approach and name what it misses.
**Hard cap ~20–30k output tokens each** — the 044-045 round let them balloon to 95–154k;
the cap is part of the contract. Stamp `check-dispatched`. Then start fixing the clusters
that need no check; fold each verdict in when it returns (`check-returned`), and record in
the resolution note that the check ran and what it flagged. If you later deviate from a
checked or pre-cleared approach, a new check is needed **only if the deviation itself is
trigger-list-shaped**.

### Unattended variant — a fix round inside an unattended run

Applies only when the driver's instruction says the round is unattended. Everything in
this skill still applies except stage 0b:

- **No owner gate.** Each triage-collected decision is parked instead: append
  `gate-parked` (`{kind: "fixer-decision", default, reason}`) to the worklog, take the
  conservative default, and record the parked question plus the default taken in this
  round's `Decisions`.
- **Conservative defaults.** A finding needing an owner ruling (a wont-fix intent, a
  capability removal, a scope question) is set `deferred` with a note starting `parked:`
  — never `wont-fix`, never silently fixed. A defect noticed outside the finding set is
  parked the same way in `Decisions`; no backlog row is minted, because routing it is
  the owner's ruling.
- **Blocker exception.** A decision that blocks a 🔴 fix ends the round: leave
  `status: in-progress`, append `round-end`, and hand the driver the question — the run
  stops with it.
- Hand-back is unchanged: renderer, auditor, doc gate, index row. `status: resolved` is
  legal with parked findings — `deferred` is a terminal status, and the run-end report
  carries every parked item to the owner.

### Per cluster (rigor scaling unchanged: 🔴/🟠 get every step; 🟡/⚪ batched, class-swept)

1. **Confirm each finding still exists** at the current commit (open the cited code). If
   one doesn't — already fixed, or you judge it a false positive — set
   `false-positive`/`disputed` with a one-line rationale instead of changing code.
2. **Name the class, sweep for siblings** (unchanged): state the defect *class*, grep code
   and docs for other sites of it, fix the class or say in the note why only the instance.
   Doc drift is fixed token-wide, never file-wide.
3. **Write ALL the cluster's regression tests first** — the concurrency case, the
   cross-tenant case, the edge input the review named — then prove them red in **one**
   scoped run (background, `test-run kind:red`; the failure lines are the red evidence,
   quoted in the resolution note). Doc-only and pure-cleanup findings need no test.
4. **Implement the cluster's fixes** at the right altitude (prefer the review's recommended
   approach; if you deviate, say why in the note). A fix that **adds a mechanism** — a new
   class, catch/mapping, event, limit, retry, cache — is a mini-feature and ships at
   feature grade: defaults/sizing derived from the real constraint, an observability hook,
   tests for the failure modes the mechanism itself introduces, and updates to every doc
   that states the old behavior.
5. **One green run** for the cluster (background, `test-run kind:green`) — while it runs,
   start the next cluster's reading or prose. Broken adjacent tests are yours to fix now.
6. **Commit one focused commit per finding** (or per tightly-related cleanup group), tests
   with their fix, message referencing the ID and review version:
   `fix(<area>): <what> (<FINDING-ID>, review <target>-v<n>)`. Stamp `finding` events.
7. **Dispatch the cluster's micro-review immediately** (next section) and move on.

**Test-runner rules:** exactly one test process at any moment; runs queue FIFO; every run
background-launched so you work while it executes; scope filters per CLAUDE.md
(`--filter FullyQualifiedName~<Namespace>` / `--include='**/<name>*.spec.ts'`); one final
scoped run over all touched namespaces before hand-back (`test-run kind:final`).

## Fix-diff micro-review — per cluster, pipelined

Your own re-read of the diff does not count: it is the same mind that wrote the fixes.
When a cluster's commits land, dispatch **one anchored Explore agent over that cluster's
diff, in the background**, asking exactly three questions:

1. **Class or instance** — do sibling sites (code or docs) still carry the defect?
2. **New surface at the bar** — does each added mechanism have sized defaults, a signal,
   failure-mode tests, and doc updates?
3. **Regression** — did the fix change any adjacent behavior?

Keep working the next cluster while it runs. Fold what it finds into a follow-up commit
for that cluster; anything you leave open goes in the decisions section for the
re-reviewer. The **last** cluster's micro-review is the only one that gates hand-back.
Batched doc/cleanup-only rounds may skip micro-review.

## Recording in the resolution file

After each finding (worklog `finding` event at the same moment), update its entry in
`resolution-v<n>.md`:

- The `## Findings` body table (the machine-read state — frontmatter carries scalars only):
  one row per finding, `| ID | Status | Commit | Note |` keyed by `PPW-<n>`, hand-written at the moment the
  finding closes. Note = one line, what you did or why you won't, **max 240 characters** —
  the story behind it goes in the decisions section, each decision ≤ 15 lines, per
  `reviews/rules/doc-contracts.md`. A mechanism-adding fix's note also names the **new surface** —
  that is where the re-review points the owning lens.
- `node reviews/lib/render-records.mjs <target>` no longer generates any table — it reads
  your Findings rows for the tallies and computes the round's runtime + metrics line. You
  hand-write everything: the table, the decisions section, deviations, boundaries.

Status values: `fixed` · `wont-fix` · `deferred` · `backlog` · `disputed` ·
`false-positive` (never `verified`). For anything other than `fixed`, write the
rationale in the **decisions** section so the re-reviewer can agree or push back.

When every finding has a terminal status **and all blockers are addressed**, set the
top-level `status: resolved`, `fixed_commit:`, and `closed:` date. If you stopped partway,
leave `status: in-progress` — the worklog means a cancelled round loses nothing.

**Example finding rows:**
```
| PPW-45 | fixed | `a1b2c3d` | scoped GetByIdempotencyKeyAsync + stale-free to userId/guestSessionId; added cross-tenant test |
| PPW-52 | wont-fix | — | DivergentFields payload justifies a distinct type; not worth refactoring ConflictException now |
```

## Hand back — do not self-verify

When the last micro-review is folded in and the final scoped run is green:

1. Append `round-end`, then run `node reviews/lib/render-records.mjs <target>` — it
   computes the round's runtime (active / blocked / idle) from the worklog, reads your
   Findings rows for the tallies, and appends the round's `fix-round` line to
   `metrics.jsonl`.
2. Run `node reviews/lib/records-auditor.mjs <target>` — it must exit clean. Then the doc
   gate on the resolution: `node reviews/lib/doc-gate.mjs <target> <n>` (must exit clean)
   plus the Sonnet judge (Agent, `model: sonnet`; input `reviews/rules/doc-contracts.md` + this
   round's changed `reviews/` files; approve, or disapprove with reasons you then fix).
   Append a `doc-gate` worklog event with the verdict.
3. Hand-write the round's row in `reviews/state/index.md`'s Passes table — adapt the
   suggestion line the renderer printed — and refresh the target's State cell if the
   round changed what it says.
4. Summarize to the user: which findings are `fixed` / `deferred` / `wont-fix`, the
   commits, the round's runtime split, and that the resolution is `resolved`/`in-progress`.
   Then state plainly that the next step is a **verification pass** against `fixed_commit` —
   it writes no files; it flips surviving findings to `verified` on the ledger (or reopens
   them) and records its verdict in the index row. Offer to trigger it, but don't mark
   verification yourself.

## Guardrails recap

- Immutable review file — respond in the resolution, never edit the review.
- Code comments follow the CLAUDE.md hard rule: never narrate a fix in code; no
  finding-ID/review citations in comments (the history lives in commits and the resolution).
- Class sweep before every fix; doc drift is fixed token-wide, not file-wide.
- Trigger-list fixes get an adversarial approach-check before implementation — dispatched
  at triage in the background, capped at ~20–30k tokens, recorded in the note; review-time
  pre-checks (`cleared`/`revised`) satisfy it; deviations re-check only if trigger-shaped.
- Mechanism-adding fixes ship at feature grade and name their new surface in the note.
- Regression tests before the cluster's fixes; reds proven in one batched run, quoted as
  evidence.
- Blocker-first ordering, applied at cluster level.
- One commit per finding, message names the ID.
- One test process at a time, always in the background, scoped filters only.
- Micro-review per cluster, pipelined; the last one gates hand-back; self-review alone
  doesn't count.
- Worklog events at the moment they happen; renderer + auditor clean before hand-back.
- Never self-mark `verified`; hand back for re-review.
