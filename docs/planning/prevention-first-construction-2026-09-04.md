# Prevention-first construction — tests from the design, before the code

*Design and implementation plan, written 2026-09-04 for whoever builds it (an agent or a person).
Revised the same day after review: the review's nine fixes and its items E1, E2, E3, E4, E9 are
folded in below. Status: approved by the owner 2026-09-04; being built on branch
`feat/prevention-first-construction`.*

This document is self-contained. Section 1 explains the project and the words used. Section 2 states
the general problem. Section 3 is the design. Section 4 is what to build, file by file, in order, with
acceptance criteria. Section 5 says how we will know it worked. Section 6 lists what is deliberately
left out and the decisions taken.

The plan is deliberately free of examples from any particular past bolt. The goal is a general
mechanism that makes defects less likely in every future bolt, not a fix for the last one reviewed.

---

## 1. Background — what you need to know first

### 1.1 The project

FotoTipar is a photo-printing web shop for the Romanian market: an ASP.NET Core 8 API
(`src/PhotoPrint.API`), an Angular 21 web app (`src/PhotoPrint.UI`), xUnit tests
(`src/PhotoPrint.Tests`), PostgreSQL 16. It is built almost entirely by AI coding agents (Claude Code
sessions) supervised by one owner. The repository's root instruction file for those agents is
`CLAUDE.md`; the project's standards live under `memory-bank/standards/`.

### 1.2 How work is organised: intents, units, bolts, stages

Work is planned with a method called AI-DLC (the `.specsmd/aidlc/` folder holds its templates and
scripts). Its vocabulary:

- An **intent** is a feature area.
- A **unit** is a slice of an intent with a brief and **stories** (small requirements with acceptance
  criteria).
- A **bolt** is one unit of construction work, usually one to three days, with a folder
  `memory-bank/bolts/<id>/` holding `bolt.md` (frontmatter with `type`, `status`, `current_stage`,
  `stages_completed`) and the stage artifacts.
- A bolt runs through **stages** defined by its type (`.specsmd/aidlc/templates/construction/bolt-types/`):
  a *simple* bolt runs plan → implement → test; a *ddd* bolt runs domain-model → technical-design →
  (adr-analysis) → implement → test; a *spike* bolt explores and documents and builds no product code.
  Each stage produces a named artifact (ddd: `ddd-01-domain-model.md`, `ddd-02-technical-design.md`,
  code + tests, `ddd-03-test-report.md`; simple: `implementation-plan.md`,
  `implementation-walkthrough.md`, `test-walkthrough.md`).
- Every unit keeps a running `construction-log.md`
  (`memory-bank/intents/<intent>/units/<unit>/construction-log.md`) where sessions append what they
  did, a **stage-exit block** at each stage boundary, and a **session-cost row** (see 1.5).

`memory-bank/standards/bolt-process.md` is the lifecycle standard. It already requires two things this
plan builds on:

- **The failure-mode table** (design stage of a ddd bolt, plan stage of a simple bolt): one row per
  thing that can go wrong — `what can fail → what should happen → which test proves it (name it now)
  → what log line fires`. The table is copied into the test report at the end with the real test names.
  Before this plan nothing checked that those tests were written before the code, or that they can
  fail. Only two of the 49 complete bolts in the repository carry such a table at all, and both use a
  two-column shape a program cannot read.
- **Two gates**: an *adversarial design check* (a fresh agent attacks the design before code) and a
  *fresh-eyes micro-review* (fresh agents read the finished diff and answer three fixed questions).
  Both are dispatched as separate agents with no memory of the author's work. The design check's
  output is prose the author may ignore.

`memory-bank/standards/definition-of-done.md` is the hand-back checklist: four rules and twelve
**defect classes** distilled from earlier review findings. Class numbers are referenced below (e.g.
"class 5: failure modes have tests — green ≠ proven").

### 1.3 How code is checked afterwards: the review loop

After a bolt hands off, it goes through the **review loop** (`reviews/README.md`): parallel AI reviewers
("lenses") read the change blind, produce **findings** with a severity (🔴 high, 🟠 medium, 🟡 low,
⚪ cleanup), record them in a per-target **ledger** (`reviews/<target>/ledger.md`) with global ids
`PPW-<n>`, and drive fix rounds and verifications until the loop is quiet or the target is certified.
Every pass appends a line to `reviews/<target>/metrics.jsonl` with `new_findings` by severity and the
`lenses` that ran.

Passes are the expensive part of the whole pipeline: a discovery pass costs on the order of millions of
tokens, and the loop's own notes estimate hundreds of thousands of tokens to *find* one serious defect,
before fixing and verifying it. The number of passes a bolt needs grows with the number of serious
findings its first pass produces.

The review loop is not the subject of this plan. This plan is about **what reaches it**.

### 1.4 The prevention sweep — an approved idea that was never run

`docs/superpowers/specs/2026-08-10-prevention-sweep-design.md` (owner-approved 2026-08-10) designs a
feedback path from the review loop back into construction:

1. Every canonical finding gets a **class tag** from definition-of-done's taxonomy, stored in a
   machine-readable file `reviews/state/defect-classes.jsonl` (the "sidecar").
2. A script, `reviews/lib/ledger-miner.mjs`, ranks the classes by weighted cost (🔴 5 · 🟠 3 · 🟡 1 ·
   ⚪ 0.5), writes a ranked table into `definition-of-done.md` between markers, and can print a
   ranking **per area** (storage, payments, orders, checkout, auth, sameday, observability, tests,
   frontend, docs, infra).
3. A one-time **backfill** classifies the existing ledger rows (owner-approved budget).
4. At hand-back, the builder names the current top-ranked classes it swept and the test pinning each.

None of it has been built. The backfill was budgeted for ~290 ledger rows; the archived ledgers now
hold roughly six times as many PPW references, so the budget is re-estimated before it runs (4.7).
This plan reuses the sweep as designed and changes only *when* the builder sees the ranking (at the
start of the bolt, not the end — see 3.2).

### 1.5 Tooling that already exists and this plan reuses

- **The test wrapper** `reviews/lib/run-scoped-tests.mjs` (implementation in `reviews/lib/fix/`): runs
  a scoped `dotnet test` or Vitest command, holds a machine-wide lock so only one test process runs at a
  time, parses the totals, and **stamps** a worklog event with a `--kind` of `red`, `green`, `final`,
  `baseline` or `revert-and-rerun`. `--summary` prints only totals and failing names; `--no-events`
  suppresses stamping. Before this plan construction used it with `--no-events`; the stamps were used
  only by the review loop's fixer.
- **The stage launcher** `.specsmd/aidlc/scripts/launch-stage.ps1` starts one fresh Claude Code
  session per bolt stage, builds its prompt from `bolt.md` plus the last stage-exit block, appends the
  working rules in `.specsmd/aidlc/scripts/working-rules.md` to the system prompt, and on exit runs
  `.specsmd/aidlc/scripts/session-cost.mjs`, which appends a **session-cost row** to the unit's
  construction log. The launcher never marks a stage complete: the session itself edits `bolt.md` and
  commits. Stage prompts are the natural place to state stage entry and exit conditions; the commit is
  the natural place to enforce them.
- **The pre-commit hook** `.githooks/pre-commit` blocks commits that add comment lines and runs the
  review library's fixture tests when `reviews/lib` changes. It is the precedent for a mechanical gate.
  It can be skipped with `--no-verify`; CI (`.github/workflows/ci.yml`) cannot.

---

## 2. The problem, in general terms

The standards tell builders what good code looks like, and the two gates catch a good share of what
the builders miss. What survives to the review loop still falls, again and again, into a small number
of kinds:

| Kind of defect | Why it survives the current process |
|---|---|
| A behaviour nobody enumerated before coding — retries, expiry, concurrency, the second of two paths, the other caller of a changed contract | The design lists what the feature *does*; what it must do *when things go wrong* is written down late or not at all, so no test asks for it |
| A test that exists but proves nothing, or a mechanism with no test at all | Green is accepted as proof; nobody has ever seen the test red, so it may be asserting nothing that matters |
| A statement in docs, config or a report that is not true of the code | Prose is written from memory of intent, not from the code, and nothing checks the two agree |
| A change on one side of a boundary not carried to the other | The other side is typed by hand and the author has to remember it |
| Something a compiler check would have flagged | The checks exist but are configured to warn, and warnings are not read |
| A cross-cutting rule broken by a change far from where the rule is stated | The rule lives in one place; the change happens elsewhere; no test spans the two |

The first two rows are the costliest to find later and share one root cause: **the tests come after
the code, if at all, and nothing proves they can fail.** A test written before the code forces the
author to enumerate the behaviour; a test that has been seen red proves it asks a real question.

---

## 3. The design

### 3.1 The one rule

> A bolt cannot leave its design stage without a complete list of failure tests, and cannot leave its
> implement stage until every one of those tests exists, was seen **red** before the code, and was seen
> red **once more with its mechanism broken by the tool** after the code.

Everything else in this plan either feeds that list (3.2, 3.3) or backstops it (3.5, 3.6).

Why this rule: the expensive defects are behaviours nobody wrote down. Forcing the list before the code
forces the enumeration. Forcing the red stamp forces the test to exist before the code shapes it.
Forcing the tool-applied mutation kills the "green but proves nothing" class without trusting the
author's word. The review loop's fixer already works this way ("a fix is a failing test first"); this
applies the same contract to first-hand construction.

The rule is a **hard stage-exit condition**, enforced at commit time and in CI (3.4). Spike bolts are
exempt (they build no product code). A bolt with genuinely no failure modes (a docs-only bolt) says so
in one row with a reason, and the adversarial check attacks that reason like any other row.

### 3.2 The failure-mode list — one structured file, four sources

The list lives in **`memory-bank/bolts/<id>/failure-modes.jsonl`**: one JSON object per line, one
line per failure mode. The Markdown table in the design artifact is *rendered from* this file, never
the other way round: a program has to read the list, and free-form tables have already drifted in
shape in the two bolts that have one.

Row fields:

| Field | Meaning |
|---|---|
| `id` | `FM-<n>`, unique in the file |
| `source` | `bolt` (the bolt's own mechanism or error path) · `class` (a ranked defect class for the area, 1.4) · `invariant` (a shared invariant the bolt touches, 3.3) · `attack` (a row the adversarial design check produced) |
| `fails` | what can fail |
| `expected` | what should happen instead |
| `test` | the test that proves it, as a fragment usable **verbatim** as the wrapper's `--filter` (API) or `--include` (UI): a test class or method name |
| `ui` | `true` when the test is a Vitest spec |
| `log` | the log line that fires (may be empty for UI rows) |
| `disposition` | `accepted` (default) or `rejected`; a rejected row needs a `reason` and no test |
| `reason` | why a row is rejected, or why the bolt has no failure modes |

Two special rows:

- `{"none": true, "reason": "..."}` — the bolt has no failure modes. Waives the test rules for the
  file; the design check still has to run and still has to attack the reason.
- `{"source": "attack", "none": true, "reason": "..."}` — the adversarial check ran and found nothing to add.

The four sources:

1. **The bolt's own behaviours** — as today: every new mechanism and error path.
2. **The ranked defect classes for the bolt's area** — from the prevention sweep's per-area ranking
   (1.4). For each of the top classes the file gets a row: how this bolt avoids it, and the test that
   proves it. This is the sweep's hand-back checklist moved to the *start* of the bolt, where it shapes
   the design instead of auditing it. Until the miner exists (4.7) the builder takes the classes from
   the routing table in `bolt-process.md`.
3. **The shared invariants the bolt touches** — from the invariant suite (3.3).
4. **The adversarial design check's attacks.** The check no longer returns prose. It returns rows
   (`source: attack`) in the same shape, and the builder must give every one of them a disposition:
   `accepted` with a named test, or `rejected` with a written reason. The stage cannot exit while an
   attack row has neither, and cannot exit at all until at least one attack row (or the "found nothing"
   row) is present, which proves the check ran.

### 3.3 The invariant suite — rules that must hold everywhere

Some facts are true of the whole application, not of one bolt, and a bolt can break them without
touching the code that states them. Write each once as an automated test that runs in every bolt's
test stage. The initial set is derived from the product's own rules and from definition-of-done's
classes, for example:

- **Money**: every page and document that shows an order (cart, checkout review, confirmation, order
  detail, admin order detail, email, invoice PDF, invoice XML) satisfies `subtotal + shipping −
  discount = total`, and VAT rounds identically everywhere (class 9: one constant, one home).
- **Counters that mirror rows**: any stored count equals the number of rows it summarises (e.g. a
  coupon's redemption count equals its non-cancelled redemptions).
- **Guest state**: the guest session in localStorage survives login and logout per the matrix in
  definition-of-done class 11.
- **Storage routing**: every upload read/write/delete goes through `IStorageRouter.For(...)`
  (a CLAUDE.md constraint), proven by an architecture test over the compiled assembly.

The suite lives in `src/PhotoPrint.Tests/Invariants/` (API) and `src/PhotoPrint.UI/src/app/invariants/`
(UI). A new invariant is added whenever a review finds a cross-cutting break; the finding's class tag
(1.4) is the trigger.

### 3.4 The proof stamps and the gate

The wrapper writes one JSON line per run to **`memory-bank/bolts/<id>/test-stamps.jsonl`** when
called with `--log <that path>`. Each stamp carries the time, the `kind`, the `filter` or `include`,
the parsed counts, the runner's **exit code**, and the duration. Construction uses three kinds per
failure-mode row:

- `--kind red` — the run **before** the feature code. **Red means the runner exited non-zero**: a
  test that does not compile because the type it names does not exist yet is red, exactly as a
  failing assertion is.
- `--kind green` — the run after the feature code, exit zero.
- `--kind revert-and-rerun --mutate <file>:<line>` — after green: **the wrapper, not the builder,
  breaks the code.** It copies the named file, rewrites the one line (flips the comparison or boolean
  operator it finds there, otherwise removes the statement), runs, restores the file byte-for-byte in a
  `finally` block, and records the file, the line, the original and the mutated text in the stamp. It
  refuses to mutate a test file. A green run follows to prove the restore.

**The check** — `.specsmd/aidlc/scripts/check-stage-exit.mjs <bolt> <stage>` — reads `bolt.md`,
`failure-modes.jsonl` and `test-stamps.jsonl` and answers, per stage:

- *design / plan*: the file exists and parses; every accepted row names a test; every rejected row has a
  reason; every attack row has a disposition; at least one attack row exists.
- *implement*: all of the above, and for every accepted test: a `red` stamp with non-zero exit whose
  time is earlier than its first zero-exit `green` stamp; a `revert-and-rerun` stamp with non-zero exit
  and a `mutate` record naming a non-test file; a zero-exit `green` stamp later than that mutation.
  A stamp matches a row when either name contains the other, so one class-level red run covers the
  class's rows.
- *test*: all of the above, and the test artifact records both gates under headings a reader can find
  (adversarial design check, fresh-eyes micro-review).

**Where it runs.** The session updates `bolt.md`'s `stages_completed` and commits; that commit is the
gate. The pre-commit hook diffs the staged `bolt.md` against `HEAD`, and for every newly completed stage
runs the check; exit 1 blocks the commit and lists the unmet conditions. A `STAGE_GATE_OK=1` override
exists for a genuine false positive and is logged like the hook's other overrides. **CI runs the same
check on every pull request** for every `bolt.md` whose completed stages grew against the base branch,
with no override: the local hook can be skipped, the merge cannot. The launcher runs the check at
launch (to put the previous attempt's unmet list into the prompt) and at exit (to print it), but
enforces nothing itself.

### 3.5 Claims carry evidence

Reports written from memory of intent drift from the code. A lint over the stage artifacts
(walkthroughs, test reports): every bullet under "Done" or an equivalent heading must contain a
repository path, a `file:line`, or a command. Numbers are a weaker signal — years, ids, line numbers
and versions are numbers too — so an unevidenced number is a warning by default and an error only
under `--strict`. The lint runs in the same pre-commit and CI step as the check, over the stage
artifacts the commit touches, so the fresh-eyes gate spends its tokens on real defects instead of on
checking the author's arithmetic.

### 3.6 Compiler checks as errors (one-time backstop)

The C# project has `Nullable` enabled but no `.editorconfig`, no analyzer severities, and does not fail
the build on warnings (the Angular side is already fully strict). Add an `.editorconfig` and a
`Directory.Build.props` that raise the .NET analyzers to a documented level and set
`TreatWarningsAsErrors`, then a one-time cleanup of the existing warnings. After that, the class of
defects a compiler can see (unused parameters, unawaited tasks, undisposed resources,
culture-sensitive comparisons) never reaches a human. Today's default-level build produces four
warnings (two `NU1902` vulnerable-package advisories, two `NU1603`, one `EF1002`); the real count only
appears once the analysis level is raised, which is why 4.9 starts with a warning-only dry run.

### 3.7 Feedback — the loop closes

Whatever the review loop still finds gets a class tag (the sweep's sidecar) and changes the per-area
ranking, so the next bolt's failure-mode list starts sharper. That is how the standards learn without
the owner rewriting them by hand.

---

## 4. What to build, in order

Each item names the files, what "done" means, and how to verify it. Do them in this order; each is
independently useful, and every prompt or standard refers only to tooling that already exists when it
lands. Acceptance checks refer to a small synthetic fixture you create or to the two bolts that carry a
failure-mode table today (039, 044) — never tune a check to one particular bolt's known gaps.

### 4.1 Construction stamps and the mutation in the test wrapper

- **Files**: `reviews/lib/fix/run-scoped-tests.mjs`, `reviews/lib/tests/unit/run-scoped-tests.test.mjs`.
- **Change**: `--log <path>` appends the stamp as a JSON line to that file (creating it) instead of the
  review worklog. Every stamp, in either destination, gains `exit` (the runner's exit code). `--mutate
  <file>:<line>` is accepted only with `--kind revert-and-rerun`: it rewrites the line as 3.4 describes,
  restores the file in `finally` and on `SIGINT`/`SIGTERM`, refuses paths under `PhotoPrint.Tests/`,
  `*.spec.ts` or any `tests/` folder (exit 2), and records `mutate: {file, line, original, mutated}`
  in the stamp. The machine lock and the summary parsing are unchanged.
- **Done when**: fixture tests cover the new flags, the refusal, the restore after a failing command,
  and `node reviews/lib/tests/run-tests.mjs` is green (the pre-commit hook runs it too).

### 4.2 The stage-exit check script

- **Files**: `.specsmd/aidlc/scripts/check-stage-exit.mjs` (new; plain Node, no dependencies, usage in
  a `--help` string, no comments — the repository's comment rule), tests under
  `.specsmd/aidlc/scripts/tests/` run by `node --test`.
- **Input**: bolt id, stage. Reads `bolt.md` for the type and the stage names, `failure-modes.jsonl`,
  `test-stamps.jsonl`, and for the test stage the test artifact.
- **Output**: exit 0 with a one-line summary, or exit 1 listing each unmet condition as one line a
  reader can verify by hand. Spike bolts and stages with no conditions (domain-model, adr-analysis)
  exit 0 with a note.
- **Done when**: on a synthetic fixture with one satisfied row and one row per unmet condition it
  reports exactly those conditions; run against bolt 044's real folder it exits 1 and the list is
  true of that folder.

### 4.3 The claims lint

- **File**: `.specsmd/aidlc/scripts/lint-claims.mjs` (new), tests beside 4.2's. Input: an artifact
  path. Rule: under headings named `Done`, `What changed`, `Completed Work`, `Verification`, `Results`
  (configurable), every bullet must contain a repository path, a `file:line`, or an inline command.
  An integer over 9 not followed in the bullet by "measured", "from", "via", "per" or a path is a
  warning, an error under `--strict`; years, ISO timestamps, `PPW-<n>`, bolt ids, `:<line>`, `#<n>`,
  `v<n>` and dotted versions are never counted.
- **Done when**: on a synthetic artifact with evidenced and unevidenced bullets it flags exactly the
  unevidenced ones; run over any finished bolt's walkthrough, every flagged line is one a reader agrees
  lacks evidence.

### 4.4 The gate: pre-commit and CI

- **Files**: `.specsmd/aidlc/scripts/check-changed-bolts.mjs` (new: one driver both callers use —
  `--staged` compares the index with `HEAD`, `--base <ref>` compares `HEAD` with a ref; for every
  `bolt.md` whose `stages_completed` grew it runs 4.2 for each new stage, and runs 4.3 over every
  changed stage artifact), `.githooks/pre-commit`, `.github/workflows/ci.yml` (a `construction-gates`
  job: the two scripts' tests, then the driver with `--base` on pull requests).
- **Done when**: a fixture commit that completes a stage with an unmet condition is refused by the
  hook with the list; the same commit passes with the condition met; the CI job runs the same driver.

### 4.5 Stage prompts, working rules and bolt-type templates

- **Files**: `.specsmd/aidlc/scripts/launch-stage.ps1`, `.specsmd/aidlc/scripts/working-rules.md`,
  the three bolt-type templates.
- **Change**: the design/plan stage prompt states the exit conditions and the exact wrapper commands
  with `--log memory-bank/bolts/<id>/test-stamps.jsonl` (paths quoted: the repository lives under a
  path with spaces). The implement prompt states the red/green/mutate sequence. The test prompt states
  that both gates must be recorded in the artifact. The launcher runs 4.2 at launch and, when a
  previous attempt left files behind, puts the unmet list into the prompt; at exit it prints the
  check's result. The templates list `failure-modes.jsonl` as a design/plan-stage artifact and the
  stamps as an implement-stage artifact.
- **Done when**: a `-DryRun` of each stage prints the conditions verbatim.

### 4.6 Update the standards to describe the new reality

- `memory-bank/standards/bolt-process.md`: a "Stage exit conditions" section stating 3.1, the file and
  its four sources (3.2), the stamps, the mutation and the gate (3.4); the Stage-2 gate text asks the
  adversarial agent for rows, not prose; the measuring section carries §5 below.
- `memory-bank/standards/definition-of-done.md`: class 5's text points at the stamps as the proof.
- `CLAUDE.md` "Working cheaply": one line — tests named in `failure-modes.jsonl` run red first through
  the wrapper with `--log`.

Not before: standards are descriptive; they change when the tooling exists.

### 4.7 Feed the prevention sweep (re-budgeted backfill + a script, own bolt)

- **Files**: exactly as `docs/superpowers/specs/2026-08-10-prevention-sweep-design.md` specifies —
  `reviews/state/defect-classes.jsonl`, `reviews/lib/ledger-miner.mjs`, the ranked table between
  markers in `memory-bank/standards/definition-of-done.md`, the `reconcile-findings` skill's feed step.
- **Addition from this plan**: `ledger-miner.mjs --area <slug> --top 5 --as-rows` prints the ranked
  classes as ready-to-append `failure-modes.jsonl` rows (`source: class`, `test` blank); the
  design-stage prompt tells the builder to run it and fill the blanks once the miner exists.
- **Before it runs**: count the real ledger rows (archived targets included) and re-estimate the
  backfill budget; the 2026-08-10 figure assumed ~290 rows.
- **Done when**: the miner's ranked table matches a hand count on a 20-row sample; the design stage of
  the next bolt shows the rows in its file.

### 4.8 The invariant suite (one small bolt per invariant group)

- **Files**: `src/PhotoPrint.Tests/Invariants/*.cs`, `src/PhotoPrint.UI/src/app/invariants/*.spec.ts`,
  a short `docs/testing/invariants.md` listing each invariant, why it holds, where it came from and
  which kind of test proves it (architecture test, property test, component test).
- **Change**: implement the invariants in 3.3 first, one story per invariant with its test kind named.
  The test stage prompt runs the suite through the wrapper with `--summary`.
- **Done when**: each invariant test fails when its rule is broken on purpose (one deliberate break per
  test, reverted) and passes on `main`.

### 4.9 Compiler checks as errors (one cleanup bolt)

- **Files**: new `Directory.Build.props` at the repository root (`TreatWarningsAsErrors`,
  `AnalysisLevel` latest, `EnforceCodeStyleInBuild`), new `.editorconfig` with analyzer severities;
  then the code changes needed to build clean.
- **Order**: first run with warnings only at the raised level and list the counts per rule; agree the
  rule set with the owner (some rules are noise for this codebase); then flip to errors and clean up.
  Coordinate the `NU1902` advisories with the dependency-hardening bolt (054) in flight.
- **Done when**: `dotnet build` is clean, the full API test suite still passes (run in sequential
  namespace batches per CLAUDE.md), and `memory-bank/standards/tech-stack.md` records the analyzer level.

---

## 5. How we will know it worked

The success measure is the one bolt-process.md already names: the **severity-weighted count of new
findings in each bolt's first review pass**, read from `reviews/<target>/metrics.jsonl`
(`new_findings`; weights **🔴 5 · 🟠 3 · 🟡 1 · ⚪ 0.5**, the same weights the prevention sweep uses).

Two normalisations make the comparison fair across bolts of different size and across a reviewer that
keeps improving: findings **per thousand lines added** (from `git diff --stat` of the bolt's branch
against its base), and the **number of review lenses** in the pass (`lenses` in the metrics line).

**The control group.** The wave-1 bolts in flight on 2026-09-04 (054, 057, 047-048, 066-067, 085-086)
are being built *without* this plan and will be reviewed by the same reviewer in the same weeks as the
first bolts built *with* it. Their first-pass lines are the baseline; older targets are a secondary
baseline only. This window closes when wave 1 is reviewed, so their review-v1 metrics are recorded
before anything is compared.

**Both sides of the ledger.** Every stage session already appends a session-cost row. Per bolt, add up
the construction cost (all stage rows) and the review cost (the metrics lines' `cost`). Prevention that
works moves findings from the review column to the construction gates (the design check's rows and the
micro-review's findings, counted from the artifacts), then makes the total shrink.

**Decision rule, fixed now**: the plan stays if, of the first four bolts built under it, at least three
score below the control group's median on the weighted, per-KLOC measure, *and* their total cost
(construction + review) is not above the control group's median. Otherwise the plan is decoration —
change it.

---

## 6. Out of scope, and the decisions taken

**Out of scope**: any change to the review loop's passes, lenses or records (this plan changes what
reaches it, not how it reviews); generating the web app's data types from the API's contract (a real,
smaller lever, later); rewriting definition-of-done's prose; mutation testing with Stryker (the
wrapper's single-line mutation is the cheap version; Stryker is the future note in
`docs/agent-systems/future/test-quality-system.md`).

**Decisions taken 2026-09-04**:
1. The one rule (3.1) is a hard stage-exit condition, enforced at commit and in CI, not guidance.
2. Construction stamps live in `memory-bank/bolts/<id>/test-stamps.jsonl`; the failure-mode list in
   `memory-bank/bolts/<id>/failure-modes.jsonl`. Both are keyed by bolt, not by unit, because the check
   is keyed by bolt and a unit spans several bolts.
3. Red means non-zero runner exit. The mutation is applied by the wrapper.
4. One weight scheme everywhere: 🔴 5 · 🟠 3 · 🟡 1 · ⚪ 0.5.
5. The analyzer rule set for 4.9 is decided after its warning-only dry run.
6. The backfill (4.7) runs after a re-budget and after the reviews in progress finish.

**Order of building**: 4.1 → 4.2 → 4.3 → 4.4 → 4.5 → 4.6 in one branch; 4.7, 4.8 and 4.9 are each a
bolt of their own.
