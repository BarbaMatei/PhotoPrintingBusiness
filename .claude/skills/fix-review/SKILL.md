---
name: fix-review
description: >-
  Drive code-review findings to closure — the fixer half of the multi-lens review
  loop. Use whenever the user wants to fix, address, resolve, act on, or "apply" the
  findings of a review: phrases like "fix the review findings", "address the blockers
  in review X", "resolve the open findings", "apply the review feedback", or right after
  a /code-review or multi-lens review produces a review-v<n>.md + resolution-v<n>.md
  under reviews/. Runs the round descheduled: triage first — protocol blocks before code for
  clusters sharing a stateful surface — one batched owner gate (parked instead when the run
  is unattended), then clusters — approach-checks and test runs execute in the background
  while the fixer works, and one round-scope composition review plus a test-meaning audit
  gate hand-back. Fixes findings blocker-first WITH the regression tests the review asked
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
   finding list (all `status: open`) before starting —
   `node reviews/lib/mint-id.mjs scaffold-resolution <target> --version <n>` seeds it from
   `review-v<n>.md`'s findings table and `reviews/templates/resolution.md`. A round that answers
   a **verification** pass has no review file of its own: its round number is the next free
   resolution version (so it runs ahead of the newest review), its Findings rows are seeded from
   the reopened and new ids that verification recorded on the ledger, and the file is hand-copied
   from the template — `scaffold-resolution` needs a review file and refuses without one.
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

Append one event to `reviews/<target>/worklog.jsonl` **at the moment the event happens**,
always through the stamper. It owns the timestamp, enforces the event vocabulary and each
event's required fields, and refuses a second open round — a hand-typed `echo` line skips
all three, which is how mislabelled rounds reached the live records:

```bash
node reviews/lib/wl.mjs <target> <ev> [--<key> <value>]... [--json '<obj>']
node reviews/lib/wl.mjs <target> round-start --round 1
node reviews/lib/wl.mjs <target> check-dispatched --round 1 --cluster awb --ids PPW-557,PPW-561
```

`--ids` comma-splits into an array; `--json` merges an object for what flags cannot express.
This is what survives a cancelled session — the loss that forced three findings' fix records
to be reconstructed from commits must stay impossible — and it is the source the renderer and
the runtime metric read. Never edit a past line: a stamp that was wrong is retracted by a
`void` event naming it, never by rewriting history.

| Event | When | Extra fields |
|---|---|---|
| `round-start` / `round-end` | first action / after hand-back summary | `round` |
| `triage-done` | cluster plan written | `round`, `clusters` (both required), plus `checks_needed`, `pre_cleared` (review pre-checks consumed), `gates` |
| `protocol-written` | a cluster's protocol block is written — BEFORE any of its `finding` events | `round`, `cluster`, `ids` (the cluster's PPW ids) |
| `gate-open` / `gate-closed` | before asking the owner / when the answer arrives | `reason` |
| `check-dispatched` / `check-returned` | approach-check out / verdict in | out: `round`, `cluster`, `ids` (the PPW ids the check covers — the auditor matches on them); back: `round`, `cluster`, `verdict`, and `tokens` if known. `ids` belongs to the dispatch only |
| `test-run` | stamped for you by `run-scoped-tests.mjs` when a run finishes — never by hand | `kind: red\|green\|final\|baseline\|revert-and-rerun`, `filter`, `passed`, `failed`, `duration_s` |
| `finding` | a finding reaches a status | `id`, `status`, `commit` |
| `round-review-dispatched` / `round-review-returned` | once, when the last cluster's commits land | `round`, on return `found` |
| `test-audit-dispatched` / `test-audit-returned` | once, alongside the round review | `round`, on return `verdict` |

Every field named above is one `wl.mjs` refuses the event without — the stamper prints
`ERROR` and appends nothing, so a stamp you thought you took is simply missing. `round` must
be a number (an unquoted `--round 3` becomes one); `ids` must be a non-empty list of
`PPW-<n>`.

The auditor refuses `status: resolved` without this evidence (rounds closed on/after
2026-08-28): a `protocol-written` event timestamped before each protocol cluster's first
fix, a consumed pre-check or a `check-dispatched` event naming every trigger-classified
fix in its `ids`, the `round-review-dispatched`/`-returned` pair on every code round, and
`test-audit-returned` whenever a red run happened. Write the events as the work happens —
they are the gate's input, not decoration.

**The round's mandatory events, once each:** `round-start` … `round-end` (a round stopped
and resumed re-stamps `round-start` for each part — the renderer measures paired spans only,
and the time between two parts belongs to no round), `triage-done`, a `protocol-written` per
protocol cluster before any of its fixes, a `check-dispatched`/`-returned` pair per check, a
`test-run` per run (the wrapper stamps it), a `finding` per finding, the
`round-review-dispatched`/`-returned` pair, and `test-audit-dispatched`/`-returned` when any
test ran red. The unit's `verify-result` events are **not yours**: `verify-fixes.mjs` writes
one per row during the verification that follows, and the driver commits them.

## Workflow

### Stage 0 — Triage (one reading, ~15–20 min, before any fix)

1. Read all in-scope findings once (new 🟡/⚪ go to the ledger backlog per the README
   router, not the fix round). Group them into **clusters by owner files** — the unit of
   work from here on. Order clusters blocker-first, then by the highest severity each
   contains (the blocker-first rule, applied at cluster level).
2. Classify each finding: **trigger-list** (see below) / **behavioral** (needs a regression
   test) / **doc-cleanup** (no test). Take the classification from the finding's Fix brief
   when one exists; judge it yourself when not.
3. **Protocol-first clusters (audit R1).** When two or more in-scope findings sit on the
   same stateful surface — same entity, state machine, key, stored path, or schedule; the
   auditor detects it mechanically as serious findings whose fix briefs overlap on files —
   the cluster's **first artifact is a protocol block**: a `### Protocol — <label>` block
   under the resolution's `Decisions` stating the states, the invariant(s) — each with a
   quantifier ("never", "at most one", "exactly once") — and the ordered rules for who
   mints/retires/cancels what. Write it from the findings, **before any of the cluster's
   code**, stamp `protocol-written` (with the cluster's `ids`) the moment it exists, and
   name it in the scope table's Protocol column. The fixes are derived from it; a protocol
   written after the code, paraphrasing the diff, is spec-theatre and the auditor's
   ordering check refuses it. The cluster's test set must include **at least one invariant
   test exercising the composed flows** — the sequence the findings share (e.g. decline →
   retry → hand-over → late success), not one mechanism at a time. This replaces
   per-finding approach drafting for the cluster.
4. For each trigger-list cluster, settle its approach: a review-time pre-check verdict of
   `cleared` (and you follow that approach) or `revised` (adopt the revision) needs **no
   new check**; `refuted` or no pre-check means a check runs this round. A **protocol
   cluster gets exactly one check, and it critiques the protocol block** — the spec, never
   the individual patches.
5. Collect every foreseeable owner decision — capability removals, scope questions,
   wont-fix/dispute intents.
6. Write the cluster plan as the resolution body's scope table (Cluster · Findings ·
   Files · Protocol — the Approach-check prose column is retired; the check is the
   `check-dispatched`/`-returned` event pair, and "not needed" is not a writable value),
   append `triage-done`.

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
adversarial agent per cluster (race/resource/frontend lens as fits). For a protocol
cluster the prompt is the protocol block plus the findings and files, asked to refute the
**spec** — break the invariant, find the flow the ordered rules mishandle; for a singleton
trigger fix it is the finding, your drafted approach, and its files, asked to refute the
approach and name what it misses.
**Hard cap ~20–30k output tokens each** — the 044-045 round let them balloon to 95–154k;
the cap is part of the contract. Stamp `check-dispatched` with the round, the cluster and the ids the check covers
(`--round <n> --cluster <label> --ids PPW-…` — all three are required fields, and the
auditor matches trigger-classified fixes against the ids). Then start fixing the clusters
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
- Hand-back is unchanged: `round-end`, the round's commit, hand to the driver — the unit's
  records and its single doc gate run after the verification. `status: resolved` is legal
  with parked findings — `deferred` is a terminal status, and the run-end report carries
  every parked item to the owner.

### Per cluster (rigor scaling unchanged: 🔴/🟠 get every step; 🟡/⚪ batched, class-swept)

1. **Confirm each finding still exists** at the current commit (open the cited code). If
   one doesn't — already fixed, or you judge it a false positive — set
   `false-positive`/`disputed` with a one-line rationale instead of changing code.
2. **Name the class, sweep for siblings** (unchanged): state the defect *class*, grep code
   and docs for other sites of it, fix the class or say in the note why only the instance.
   Doc drift is fixed token-wide, never file-wide.
3. **Write ALL the cluster's regression tests first** — the concurrency case, the
   cross-tenant case, the edge input the review named, and for a protocol cluster the
   invariant test over the composed flows — then prove them red in **one**
   scoped run (background, `test-run kind:red`; the failure lines are the red evidence,
   quoted in the resolution note). Doc-only and pure-cleanup findings need no test.
   **The fix brief's suggested test shape is the assertion spec** (audit R4): write the
   test to its words; any deviation is justified in the finding's note, and the
   test-meaning audit checks the test against the brief's words, not against your code.
   Three rules every new test must satisfy: (a) an assertion never reads the production
   constant or symbol under test — assert the literal; (b) persisted state is asserted
   through a fresh context or a second connection, never the one that wrote it; (c)
   "in-flight" behaviour is tested with a genuinely asynchronous fake, not a synchronous
   stand-in.
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
7. **Record the fix's revert proof**: the smallest lever that reintroduces the defect (one
   branch, one call, one constant — never a whole-file revert) and the red line it
   produces, in the resolution (a `Revert proofs` decision block, or the note). This is
   what the verification's evidence audit samples and re-runs.
8. Move on to the next cluster. There is no per-cluster review — the round review (next
   section) covers the whole round's diff at once.

**Every test run goes through the wrapper** — it builds the scoped command, holds a
machine-global lock so only one test process runs on this machine (exit 3 if another holds
it), and stamps exactly one `test-run` event with the parsed counts and the measured duration:

```bash
node reviews/lib/run-scoped-tests.mjs <target> --kind red --filter "PhotoPrint.Tests.Unit.Orders" --round 1 --cluster orders
node reviews/lib/run-scoped-tests.mjs <target> --kind green --ui --include "order-summary" --round 1 --cluster orders
```

`--kind` is `red|green|final|baseline|revert-and-rerun`; `--filter` (API) or `--ui --include`
(UI) is required either way, so every stamped event carries the scope it ran. Runs queue FIFO
and every run is background-launched so you work while it executes; one final scoped run over
all touched namespaces before hand-back (`--kind final`).

Two rules against runs whose result is thrown away:

- **Skip the rebuild when no source changed since the previous run** — a rebuild of an
  unchanged tree costs ~15 s per invocation and proves nothing. The wrapper has no
  `--no-build` flag of its own; hand it the command instead, `{filter}` and all:
  `--cmd "dotnet test src/PhotoPrint.Tests --no-build --filter \"FullyQualifiedName~{filter}\""`
  — and keep passing `--filter` (or `--ui --include`) alongside it, or the wrapper exits 2:
  the flag is what the stamped event records as the scope, whatever command ran.
  Docs-only and records-only steps between two runs do not invalidate the build.
- **Never launch the final run while the round review is in flight.** Its follow-up fixes
  land after, so the run is dead on arrival and pays full cost. Wait for
  `round-review-returned`, fold the follow-ups in, then run once.
- **No full-suite runs at round end.** The final run is scoped to the touched namespaces;
  the full suites run exactly once per loop, at the certification freeze (README note ³).

## Round review — one composition review over the whole round (audit R3)

Your own re-read of the diff does not count: it is the same mind that wrote the fixes.
Per-cluster micro-reviews are retired too — each saw one cluster and none saw the round,
which is how three individually-verified payment fixes composed into a double charge.
When the **last** cluster's commits land, dispatch **one anchored agent over the round's
entire diff plus all resolution notes, in the background** (`round-review-dispatched`),
with this fixed brief:

1. **Enumerate every pair of fixes that share state, files, or schedules**; for each
   pair, trace the combined behaviour of the flows both touch.
2. **Enumerate every caller of each changed state transition**, and **every reader of
   each signal a fix retired** — who never sees the key/event/status again, and what
   that reader's absence breaks.
3. The old per-cluster questions, now at round scope: sibling sites still carrying a
   defect class; each added mechanism at the bar (sized defaults, a signal, failure-mode
   tests, docs); adjacent behavior changed.

Budget: what five small micro-reviews cost, spent once at the altitude where
cross-cluster interactions are visible. Fold what it returns (`round-review-returned`,
`found`) into follow-up commits; anything left open goes in the decisions section for the
re-reviewer. It gates hand-back on every code round — the auditor refuses a resolved
resolution without the event pair. Doc/cleanup-only rounds may skip it. It does not
replace the blind delta pass; it exists to starve it.

## Test-meaning audit — a cheap sidecar over the new tests (audit R4)

Alongside the round review, dispatch one small agent (`test-audit-dispatched`) over
**only the tests this round added or changed**, with each test's fix brief. It checks the
three rules from the per-cluster step (assert the literal, fresh-context reads, genuinely
asynchronous fakes) and that each test asserts what the brief's suggested test shape
says **in the brief's words** — a deviation is legitimate only where the note justifies
it. Stamp `test-audit-returned` with the verdict; required whenever the round proved any
test red. Three of one round's regression tests passed for reasons unrelated to their
bug — this sidecar is what makes that visible before hand-back instead of two passes
later.

## Recording in the resolution file

After each finding (worklog `finding` event at the same moment), update its entry in
`resolution-v<n>.md`:

- The `## Findings` body table (the machine-read state — frontmatter carries scalars only):
  one row per finding, `| ID | Status | Commit | Note |` keyed by `PPW-<n>`, hand-written at the moment the
  finding closes. Note = one line, what you did or why you won't, **max 240 characters** —
  the story behind it goes in the decisions section, each decision ≤ 15 lines, per
  `reviews/rules/doc-contracts.md`. A mechanism-adding fix's note also names the **new surface** —
  that is where the re-review points the owning lens.
- `render-records.mjs` no longer generates any table — it reads your Findings rows for the
  tallies and computes the round's runtime + metrics line, and the **driver** runs it after
  the verification, not you. You hand-write everything: the table, the decisions section,
  deviations, boundaries; your rows are the renderer's only input, so a missing row is a
  missing tally.

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

When the round review and the test audit are folded in and the final scoped run is green:

1. **Close the round.** `node reviews/lib/wl.mjs <target> round-end --round <n>`, then commit
   the code and the resolution together — frontmatter `status: resolved`, `fixed_commit:`,
   `closed:`. `worklog.jsonl` is tracked and your round's stamps are still uncommitted, so
   either include the worklog in that commit or leave it for the driver's records commit,
   which takes it up before the verification — but say in the hand-back which you did. The
   tree must be clean at the round's tip: `verify-fixes.mjs` refuses a dirty tree by design,
   and that tip is the commit its reverts are measured against.
2. **Hand to the driver.** A fix round and its verification are **one reviewed unit**: the
   verification runs now, at this tip, and the unit's records follow it — the renderer, then
   the auditor, then one doc-gate sitting covering both halves. So you do **not** run
   `render-records.mjs`, **not** `records-auditor.mjs`, **not** the doc gate, and you do
   **not** write the index row. The evidence those gates read is still entirely yours: the
   round's worklog events, the revert proofs, the round review and the test audit — a round
   that skimped on them fails the driver's auditor, not yours.
3. **Invoked standalone, with no driver:** stop after step 1 and say so — "unit records
   pending verification; run the loop-driver". Never render or gate the round yourself to
   make it look finished; a rendered round with no verification is exactly the split-unit
   record this sequence removes.
4. Summarize to the user: which findings are `fixed` / `deferred` / `wont-fix`, the commits,
   and that the resolution is `resolved`/`in-progress`. Then state plainly that the next step
   is the **verification pass** against `fixed_commit`, run by the driver as the rest of this
   unit — it writes no files of its own; it flips surviving findings to `verified` on the
   ledger (or reopens them). Never mark verification yourself.

## Guardrails recap

- Immutable review file — respond in the resolution, never edit the review.
- Code comments follow the CLAUDE.md hard rule: never narrate a fix in code; no
  finding-ID/review citations in comments (the history lives in commits and the resolution).
- Class sweep before every fix; doc drift is fixed token-wide, not file-wide.
- Trigger-list fixes get an adversarial approach-check before implementation — dispatched
  at triage in the background, capped at ~20–30k tokens, recorded in the note; review-time
  pre-checks (`cleared`/`revised`) satisfy it; deviations re-check only if trigger-shaped.
- Mechanism-adding fixes ship at feature grade and name their new surface in the note.
- Protocol clusters: the protocol block first (`protocol-written` before any of the
  cluster's fixes), the check critiques the spec, and one invariant test drives the
  composed flows.
- Regression tests before the cluster's fixes; reds proven in one batched run, quoted as
  evidence; the fix brief's test shape is the assertion spec.
- Blocker-first ordering, applied at cluster level.
- One commit per finding, message names the ID; each fix's smallest-lever revert proof
  recorded for the evidence audit.
- One test process at a time, always in the background, scoped filters only; no
  full-suite run — that happens once, at the certification freeze.
- One round review over the whole diff plus the test-meaning audit gate hand-back;
  self-review alone doesn't count.
- Worklog events at the moment they happen, always via `wl.mjs`; every test run via
  `run-scoped-tests.mjs` — no hand-typed stamps, no unwrapped test process.
- Hand back at `round-end` plus the round's commit: the renderer, the auditor, the doc gate
  and the index row belong to the driver's verification step, not to you.
- Never `COMMENTS_OK=1` or `DOCGATE_OK=1` — an override is logged, and during an
  unattended run it stops the run; fix the cause instead.
- Never self-mark `verified`; hand back for re-review.
