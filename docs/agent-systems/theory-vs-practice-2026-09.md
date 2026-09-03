---
type: cross-analysis
status: resolved — rulings applied on the branch 2026-09-03; thesis wording pending owner approval
created: 2026-09-02
owner: Matei Barba
scope: docs/agent-systems + thesis (this branch) versus reviews/ + .claude/skills (main)
companion: https://claude.ai/code/artifact/d66bbfc8-0827-4716-8d45-87dda38eb0f2 ("Blueprint and Workbench", the readable version)
---

# Blueprint and workbench — the agent-systems theory against the review machinery that got built

Two things were made this summer without looking at each other. In June, on this branch, a
**blueprint**: specs for an AI "software organization" — a bug-hunter that inspects the code, a
knowledge-builder that remembers intent, a contract between them, and concept notes for a
reviewer, a conductor and more. From mid-June to September, on `main`, a **workbench**: the
review loop under `reviews/` and four skills, built by hand while reviewing the bolt branches,
run on six real features, and reviewed by itself.

This document compares the two, concept by concept, decides which side holds the better
version of each idea, and lists what to do next. Neither side wins by default. The rule was:
measured evidence beats design intent when they conflict; design intent fills the gaps evidence
never reached.

---

## 1. The one-page version

**What happened.** The blueprint drew a bug-hunter and planned to build it last-but-one, after
a knowledge-builder, with a code reviewer deferred to the very end. Meanwhile the workbench
built the bug-hunter's engine first — blind hunting lenses, a defect ledger, dedup, fix
verification by reverting the fix and re-running the test, a seeded-bug experiment, a
false-certification counter — and called it "review", because it ran on one pull request at a
time, before merge. Neither side's documents mention the other. Zero cross-references.

**Is the workbench a reviewer or a bug-hunter?** A bug-hunter, running on a reviewer's
calendar. Of the eleven hunting lenses, eight hunt defects and three check reviewer things
(requirements met, design quality, test adequacy). Of the serious findings with recorded lens
data, 56% came from defect lenses alone, 21% from reviewer lenses alone, the rest from both.
Every piece of the trust machinery — ledger, severity, dedup, verification, escapes, backlog —
is inspector machinery. The blueprint's actual Reviewer (intent fidelity against a contract,
design verdicts) is still mostly unbuilt.

**Where they contradict.** Seven places (section 5). The two that matter most: the blueprint
wants dismissed findings to become suppression patterns; the workbench refuses to suppress and
attaches the past decision instead — and has evidence that suppression would have been wrong
(3 of the first 5 re-raised findings were overturned). The blueprint confirms bugs by running
them in a sandbox; the workbench confirms bugs by argument and only confirms *fixes* by
execution — and paid for it (tests that "passed for the wrong reason").

**What each side must learn from the other.** The blueprint does not know the workbench's
dearest lessons: one review pass is a *sample* of the defects, not a sweep; fixes are the
dominant source of new defects; records need a gate of their own; the owner's reading load is
the real bottleneck. The workbench lacks what the blueprint designed on paper: a map of the
application (index, reachability), a run budget, deterministic scanners, execution-confirmed
bugs, a single-writer store layout, and a hand-off that makes a fix a reviewed change rather
than a patch inside the review loop.

**The merge.** Git-wise trivial — a fast-forward, no shared files. Conceptually: keep the
workbench as the one engine, re-baseline the blueprint to say so, re-scope the 43 planned
bug-hunter briefs to the 16 genuinely missing pieces, and let the thesis stand on the
workbench's data. Both sides converge on one unanswered question: **what fraction of planted
serious bugs does the loop actually find?** The seeded run that answers it (~2–2.5M tokens) has
been deferred twice. Everything else in both worlds is cheaper than that, and rests on it.

---

## 2. The two sides, in plain words

### 2.1 The blueprint (this branch, 3–23 June 2026)

Eight commits, docs only. The core is `docs/agent-systems/`:

| Document | What it is | Size |
|---|---|---|
| `bug-hunter-build-guide.md` | The Inspector. Six permanent pipeline slots (Map → Hunt → Verify → Triage → Report → Learn) plus Remediate, filled in five additive phases through **43 numbered construction briefs**, each to be built with `skill-creator`. | 1,321 lines |
| `knowledge-builder-build-guide.md` | The Librarian / oracle. Seven stages that distil ratified intent from `memory-bank/**` into checkable contracts, behind a firewall that keeps "what the code does" from being mistaken for "what was intended". | 1,138 lines |
| `integration-contract.md` | The normative interface: one writer per store, a single history on `main`, a run mutex, a query envelope, a `correlation_id` that keys the bug → fix → verify loop, operating profiles. | 327 lines |
| `ARCHITECTURE.md`, `operating-profiles.md`, `README.md` | The map of the org, how to deploy it, the index. | ~480 lines |
| `future/*.md` | Concept notes for the deferred systems: **code-review** (the Reviewer), conductor, analyst, planner, test-quality, observability. | ~550 lines |
| `thesis/` | A bachelor-thesis proposal built on the sole-writer ledger + closed verification loop, and an annotated bibliography. | 192 lines |

Plus the inception for the bug-hunter (intent 035, bolts 085–094, all `planned`), inceptions
for ten other intents (bolts 054–084: hardening, layering, e2e, environments, EU study), two
agent definitions (`bolt-parallel-planner`, `bolt-wave-orchestrator`), and analysis/planning
records from June.

The blueprint's own roster (`future/README.md`) lists the bug-hunter as *"specced, ready to
build"* and the code-review system as *"planned (deferred) — missing"*.

### 2.2 The workbench (main, 18 June – 2 September 2026)

Built while reviewing the bolt PRs, one PR at a time, and hardened by reviewing itself.

- **Process**: `reviews/README.md` (router with 18 rows, entry tiers, stop rule, unattended
  policy), two runbooks, doc contracts, a metrics schema.
- **Skills**: `loop-driver` (picks and drives the next pass), `fix-review` (the fixer's
  contract), `owner-summary`, `reconcile-findings` (same-problem matching, id minting).
- **Code**: 136 files under `reviews/lib/` — router, autonomy policy, discovery workflow (11
  lenses + skeptics + synthesis), revert-and-rerun fix verifier, records renderer, records
  auditor, doc gate (lint + Sonnet judge), speed report, convergence rule; 24 test files, 742
  assertions.
- **Records**: 698 archived ledger rows across six targets, a 255-row backlog, a global
  `PPW-<n>` id counter at 711, per-pass `metrics.jsonl`, a certification track record, a
  meta-ledger of 47 findings against the machinery itself.

Track record (pass tokens as recorded; fix rounds were never metered):

| Target | Passes | Serious rows 🔴/🟠 | Outcome | Pass tokens |
|---|---|---|---|---|
| 035 payment idempotency | 10 | 1 / 11 | closed, retroactive sign-off, no certification | not recorded |
| 042 thumbnail cache | 9 | 3 / 30 | closed, no certification | ~10.9M |
| 043 cloud storage | 9 | 2 / 31 | **certified** (v9, single pass) | 11.8M |
| 015 sameday shipping | 6 | 8 / 35 | **certified** (v5); 47 later fixes never blind-searched | 10.1M (+2.3M lost) |
| 044-045 observability | 6 | 10 / 39 | closed, no certification, 2 lenses owed | 4.3M |
| 038-039 invoicing | 17 | 34 / 84 | **closed by owner ruling, not certified**; 11 serious rows still open, all fix-caused | 16.1M |

Across all: ~53M recorded pass tokens, 234 fixes verified by revert-and-rerun, 6 reopened
(2.6%), 2 certifications under watch, 0 post-certification escapes so far. "A zero-serious
full pass has never been observed on any target."

### 2.3 Timeline and the gap

```
June 3 ──── June 15 ── June 23                                              Sept 2
  │  blueprint: analysis → specs → contract → future notes → thesis           │
  │                                                                            │
  │       June 18 ── July 4 ── July 13 ── July 22/28 ── Aug 10–12 ── Aug 27 ── Sept 2
  │       workbench: 035 rounds · self-driving notes · seeded run 1 · two certifications
  │                  · records redesign + SF meta-review · fix-round redesign · 038-039 closed
  └── merge-base = origin/main today; the branch adds docs only
```

The blueprint stopped on 23 June. The workbench started five days after the blueprint's
specs were finalized and never read them: `grep` finds no "bug-hunter", "knowledge-builder"
or "integration-contract" anywhere under `reviews/` or the skills, and no "reviews/" anywhere
under `docs/agent-systems/` or `thesis/`.

---

## 3. What did we build — a reviewer or a bug-hunter?

The blueprint draws a sharp line (`future/code-review-system.md`):

| | Reviewer | Inspector (bug-hunter) |
|---|---|---|
| When | at the moment of change, pre-merge | periodic runs |
| On what | one diff / bolt | the whole standing codebase |
| Looks for | intent drift, design degradation, standards violations, missing tests, comment rot | latent defects |
| Output | a verdict: accept / block / revise | a ledger of confirmed bugs, fix-requests |

The workbench, sorted against that line:

**Trigger and scope are the Reviewer's.** The loop fires at bolt-process stage 6, pre-merge,
on one feature branch; its scope is the branch diff plus the unchanged code it touches. It
ends in a verdict (`request-changes` / `approve-with-followups` / `approved`).

**The lenses are mostly the Inspector's.** Eleven lenses in the manifest
(`reviews/lib/records/schema.mjs`):

| Lens | Hunts | Blueprint role |
|---|---|---|
| `correctness` | edge inputs, off-by-one, weakened guards, resource lifetime, TOCTOU | Inspector |
| `security` | authz bypass, IDOR, injection, replay/double-charge, PII in logs | Inspector (`security-auditor`) |
| `race` | check-then-act windows, non-idempotent writes, crash between steps | Inspector (`concurrency-auditor`) |
| `db-parity` | design-time vs runtime type drift, DDL never exercised by a test | Inspector |
| `input-validation` | caps, fail-open on null, over-accepting parsers | Inspector |
| `observability` | swallowed exceptions, indistinguishable incidents, partial-state failures | Inspector |
| `frontend-ux` | per-user-type branches, in-flight dedup, RxJS races, dead-end redirects | Inspector |
| `requirements` | every acceptance criterion delivered, docs match code, no undocumented scope | **Reviewer** (intent fidelity) |
| `quality` | reuse, simplification, right layer — report-only | **Reviewer** (design quality) |
| `tests-coverage` | which test goes red if this bug is injected; tests passing for the wrong reason | **Reviewer** (test adequacy) |
| `completeness-critic` | what this review is likely to under-review | meta (neither) |

**The machinery is entirely the Inspector's.** Blind parallel hunters; a canonical ledger with
never-reused ids; severity with a convergence-weighted second look; same-problem
reconciliation; fixes closed only by reverting them and watching the test go red; a seeded-bug
recall experiment; a false-certification (escape) counter; a backlog of unfixed minors. In the
blueprint these are `ledger-io`, `deduplication`, `severity-scoring`, `bug-verifier`,
`fix-verification`, `eval-corpus`/`eval-metrics`, `bug-lifecycle`. None of them is in the
Reviewer note.

**The numbers agree.** Per-finding lens attribution exists for two targets (the metrics layer
was added in August): 152 new serious findings.

| Caught by | Findings | Share |
|---|---|---|
| defect lenses only | 85 | 56% |
| reviewer lenses only (`requirements`, `quality`, `tests-coverage`) | 32 | 21% |
| both kinds | 19 | 12.5% |
| `completeness-critic` only | 12 | 8% |
| no lens recorded | 4 | 3% |

Per lens, serious findings it touched: `completeness-critic` 37, `correctness` 36,
`tests-coverage` 28, `observability` 24, `requirements` 23, `frontend-ux` 22, `race` 19,
`input-validation` 11, `db-parity` 10, `security` 9, `quality` 8.

**Verdict.** The owner's reading is right: the workbench is a primordial bug-hunter. The
nuance: it is a bug-hunter *running in the blueprint's own "pre-merge" mode* — the guide
already allows "a pre-merge run … on a feature-branch worktree" but makes it read-only and
advisory (findings go to a PR comment). The workbench made that mode the stateful main event:
records, ledger, verification and certification all live there. Two consequences:

1. The blueprint's roster is wrong about status. The bug-hunter is not "ready to build"; its
   Phase 1 and half of Phases 2, 4 and 5 exist (section 8). The "missing" Reviewer is not
   missing either — three of its five dimensions run as lenses; the other two (intent fidelity
   against a *contract*, comment/doc accuracy) and the accept/block synthesis do not.
2. The stop-rule problem the workbench has fought all summer (SF14: "certified under a stop
   rule whose gating experiment never ran") is a consequence of the posture, not of the
   engine. A periodic inspector over standing code never has to declare a feature "done"; a
   pre-merge gate must. Running the Inspector's engine on the Reviewer's calendar creates the
   hardest question in the system.

---

## 4. Concept by concept

Legend for *Relationship*: **same** (both have it, compatible) · **drift** (both have it,
different shape) · **conflict** (they disagree) · **blueprint only** · **workbench only**.

| # | Concept | Blueprint says | Workbench built | Relationship | Stronger | Merged truth |
|---|---|---|---|---|---|---|
| 1 | Defect ledger | One JSON ledger + md mirror under `bug-hunting/`, sole writer, signature `path::symbol::bug_type`, content hash, ids never reused | One md ledger per target under `reviews/<target>/`, global `PPW-<n>` never reused, flips written only by the renderer, closed targets roll into `state/backlog.md` | drift | workbench on readability and gating; blueprint on queryability | Keep per-target md as the human record; add one machine-readable index across targets (the metrics files already half do this) |
| 2 | Identity / dedup | Signature match is a *candidate* duplicate, never auto-collapse | Reconciler: same mechanism at same site; split when unsure; fix-residuals get `residual-of` lineage; scored against hand-labelled ground truth, 0 over-merges | same | workbench (tested) | Blueprint's `deduplication` brief should adopt lineage, `hinted`, and the ground-truth gate |
| 3 | Severity and confidence | Critical/High/Medium/Low × confidence (execution > corroboration > heuristic) × reachability → risk 0–100 | 🔴🟠🟡⚪ ("critical/blocker" banned words) + skeptic verdict (confirmed/plausible/refuted) weighted by how many independent lenses converged | drift | workbench on the second-look mechanics; blueprint on reachability | Four levels + convergence as confidence; add reachability when a Map exists |
| 4 | Confirming a *bug* | Sandbox execution, disprove-first, proof run twice, flake-guarded, commit-matched; static fallback never silent | Skeptics argue from code (trace-first); no execution | conflict | blueprint | 🔴 findings need an executable proof (a red test) before they count; 🟠 may stay argued |
| 5 | Confirming a *fix* | `fix-verification` re-runs the harvested proving test; `verified-fixed` / `fix-failed` / `closed-unverified`; never on the Builder's word | `verify-fixes.mjs`: revert the fix, run the test, watch it go red, restore; verdicts `held`, `test-never-red`, `revert-broke-build`, `no-test`; fixer ≠ verifier; 234 verified, 6 reopened | same | workbench (richer verdicts, real data) | The blueprint's state machine is right; import the workbench's verdict vocabulary into it |
| 6 | Who writes the proving test | The inspector's `regression-harvest`, owner-approved; the Builder never grades its own fix | The fixer writes red-first tests; three "passed for the wrong reason" → a test-meaning audit was added (SF45) | conflict | blueprint in principle; workbench has the mitigation | Separate authorship: test spec from the finding's fix brief, test audited by a non-fixer — the workbench now does this; the blueprint should keep its stricter rule |
| 7 | Fix loop plumbing | Fix-request mailbox keyed by `correlation_id`; the Builder (AI-DLC) fixes as a bolt; inspector notices `complete` and verifies | Fixer is a subagent inside the loop; resolution file + worklog; router routes verification; `PPW-<n>` is the key | drift | blueprint for separation; workbench for speed | A fix round is a mini-bolt: keep in-loop speed but give it the Builder's gates (a protocol block, a composition review — already added as SF41/SF44) |
| 8 | Learning from dismissals | `suppression-learning`: dismissal reasons → validated suppression patterns (proposed, not auto) | Decisions attached verbatim to re-finds, **never suppressed**; 3 of the first 5 re-raises were overturned | conflict | workbench (evidence) | Never suppress a hunter; attach the prior ruling and let a skeptic re-argue it |
| 9 | Learning that improves the *builder* | none (Learn slot improves the hunter only) | Prevention-sweep idea: mine the ledgers for recurring defect classes, hand the builder a mandatory self-sweep before review | workbench only | workbench | Add a "feed the builder" output to the Learn slot |
| 10 | Breadth and repetition | One run per trigger; specialists added in Phase 3 | Lens manifest front-loaded; **one pass is a sample** (035: 15·15·18 near-disjoint findings; 043 pair overlap 12%, ≈19 serious findable vs 12 found); repeated blind passes; capture–recapture only on frozen commits | workbench only | workbench | The guide must state that a single run's recall is a sample and design for repetition |
| 11 | Blinding | not mentioned | Lenses see no prior records, git history or ids; `hinted` flag for planted agreement; blinding auditor still unbuilt | workbench only | workbench | Contract-level rule for any judgment agent |
| 12 | Fix-caused defects | `bug-lifecycle` handles Reopened vs new-via-`related`; `fix-proposal` never applied | Dominant cost driver: ~¼ of 035's problems, 13 of 24 in 042 v6, 16 of 28 in 038-039 v15, all 24 in v17; seed rate `s(r)`, convergence rule (two rounds seeding one component at s ≥ 0.3 → design pass) | workbench only | workbench | The blueprint's remediation phase needs the seed-rate concept and a design-pass exit |
| 13 | Stop rule / done | none — periodic, "stopped on budget" | Certified = no 🔴 survives, every 🟠 has a recorded decision, every lens ran, a blind pass followed the last fix, seed rate 0; "certified means exactly that — not zero defects"; stop rule itself unproven (SF14) | workbench only | — | Pre-merge mode needs it; periodic mode does not. Keep both modes and say which rule applies to which |
| 14 | Scope and map | Whole codebase; Map slot: `app-mapping`, `code-index`, `reachability`, flow ids; incremental scanning default | Diff + unchanged collaborators; lenses added by touched paths; no map, no index; 255 backlog rows are the standing-code debt | blueprint only | blueprint | Build the Map slot; run a periodic standing sweep that drains the backlog |
| 15 | Deterministic tools | `tool-ingest`: pinned, checksum-verified scanners as untrusted data; `dependency-audit`, `config-auditor` | none (LLM lenses; deterministic only in tests/verification) | blueprint only | blueprint | Cheap wins first (dependency audit, SAST) through an ingest step |
| 16 | Intent grounding | Knowledge-builder oracle; `intent-lookup`; findings tagged `intent-unconfirmed` when no contract | `requirements` lens reads the bolt's own docs directly; 23 serious findings on two targets | drift | undecided | Direct reading works pre-merge; the firewalled oracle is a later, whole-codebase concern |
| 17 | Budget | Budget unit = hunter dispatches + sandbox sessions; on exhaustion finish Triage/Report/Close, record "stopped on budget"; cheap-first | Delta passes capped at 600k; full passes 2.5–3M; ~293k tokens per serious finding; fix rounds unmetered; router cost table stale | drift | blueprint on mechanism; workbench on real numbers | Adopt the budget unit; meter fix rounds |
| 18 | Human role | Owner at checkpoints; approvals async, never gate a run; triage with mandatory reason | Owner = rate limiter and trust anchor; gates; unattended policy parks decisions (`gate-parked`) for a batched ruling; reading load is the recorded pain; summaries ≤ 60 lines with "reasons to doubt" | same | workbench (lived it) | "Parked" is the async inbox; the summary format is the missing piece in the blueprint |
| 19 | Records | Three-audience bug record (plain summary, developer detail, reproduction); per-run report; SARIF, tickets | Immutable `review-v<n>.md`, ledger detail block, resolution, summary; doc contracts with size caps; deterministic lint + Sonnet judge on every round's files | drift | both | Three-audience record inside the workbench's gated templates |
| 20 | Store discipline | One writer per store; runs only in the integration worktree on `main`; run-lock; path-scoped publish commits | Records ride the feature branch; renderer and stamper are sole writers of their files; a duplicate-id alarm was needed for parallel worktrees (SF18) | drift | blueprint | Owner decision: records on the branch (travel with the PR) vs one store on `main` (the contract's model) |
| 21 | Self-review | Eval harness, curator health summary | The machinery is itself a review target: 47 `SF` findings, scorecard 5.1/10, fixes verified by revert-and-rerun | same | workbench | Keep; the blueprint should name it |
| 22 | Portability | Core invariant; profile = (TriggerPolicy, CommitPolicy); `solo-local` active | Option D "package the kit" argued, not built | same goal | — | Later; one external adopter first |
| 23 | Build method | 43 briefs, each via `skill-creator`, three test prompts per component, phase by phase | Hand-built scripts and skills, grown by running on real targets and fixing what broke; "the doc gate has never run a fresh target from v1 to close" | conflict | workbench | Thin slice on a real target beats a 43-brief plan; the Reviewer note already recommends exactly this |

---

## 5. Where the two sides contradict each other

**A. Build order.** Blueprint: knowledge-builder and bug-hunter first, Reviewer last. Reality:
the bug-hunter's engine exists as a pre-merge gate, the Reviewer is one-third built inside it,
and the knowledge-builder has nothing. *Resolution:* stop treating them as three future
systems. There is one engine with two possible postures. Update the roster.

**B. Suppression versus attaching decisions.** Blueprint: dismissals become suppression
patterns. Workbench: never suppress; attach the decision; 3 of 5 early re-raises were
overturned on re-argument. *Resolution:* the workbench is right. Drop `suppression-learning`
as specified; replace with decision attachment plus a skeptic re-check.

**C. Bug confirmation by argument versus by execution.** Blueprint: sandbox, disprove-first,
run twice. Workbench: skeptics argue; execution only when a fix is verified. The workbench's
own audit found tests recorded as passing for the wrong reason (SF45) and "two of that round's
own tests are recorded as passing for the wrong reason" on 038-039. *Resolution:* the
blueprint is right for 🔴. Cost is the owner's call; a red test per 🔴 finding is the cheapest
form and doubles as the proving test the fix verifier already needs.

**D. Scope and the stop rule.** Blueprint: whole codebase, periodic, no "done". Workbench:
one diff, must end, stop rule unproven, zero-serious pass never observed, 255 backlog rows
nobody sweeps. *Resolution:* these are complementary, not competing. The pre-merge pass stays
bounded and cheap; a periodic standing sweep (the blueprint's posture, with a Map slot) drains
the backlog and catches what fixes seeded later. The stop rule then only has to be good enough
for a gate, because the sweep is the safety net behind it.

**E. Who fixes.** Blueprint: the inspector emits a fix-request; the Builder fixes as a bolt;
the fix gets the same gates as a feature. Workbench: a fixer inside the loop patches finding
by finding — and fixes became the main source of new serious defects. *Resolution:* the
blueprint's separation is what the workbench arrived at piecemeal (protocol-first clusters,
one composition review per round). Name the fix round a mini-bolt and give it the gates
explicitly rather than by accretion.

**F. Where records live.** Blueprint: one store on `main`, one writer, run-lock. Workbench:
per-target folders on the feature branch, archived after close, a real duplicate-id incident.
*Resolution:* owner decision. Records on the branch travel with the PR and are what made the
loop work as a gate; a single store is what makes a standing sweep and cross-target queries
possible. Both can hold if the branch folder is the working copy and `main` is the archive —
which is roughly what `archive/` already is.

**G. Vocabulary.** Same things, different names (Appendix A). Harmless until someone builds
"the bug-hunter" beside "the review loop" and both keep a ledger.

---

## 6. What the blueprint does not know yet

Lessons paid for on the workbench, absent from all 2,786 lines of the two guides and the
contract:

1. **One pass is a sample.** Three blind audits of the same feature found 15, 15 and 18
   problems, nearly disjoint. Design for repetition and breadth; treat "the reviewer went
   quiet" as a fact about the reviewer.
2. **Fixes seed defects.** Between a quarter and all of a late round's new serious findings
   were caused by earlier fixes. The guide's remediation phase has no concept of this.
3. **Blinding is a mechanism, not a hope.** Hunters must not see prior records, ids or
   history; agreement planted by shared hints must be flagged.
4. **Never suppress a hunter.** Attach the earlier ruling instead. Early re-raises were
   overturned 3 times out of 5.
5. **The verifier is never the fixer, and the fixer must not grade its own test.** The
   contract has the first rule for systems; the workbench needed it *inside* one system.
6. **Records need their own gate.** A wrong review that follows every template still passes —
   so the workbench separates record quality (lint + judge) from review truth (seeded recall)
   and says so out loud.
7. **The owner's reading load is the throughput limit.** Summaries capped at 60 lines, a
   computed "reasons to doubt" section, decisions parked and batched. The blueprint's inbox
   has no size discipline.
8. **A stop rule is a claim that needs an experiment.** The workbench has certified twice under
   a rule whose gating experiment never ran and records that as its own top open finding.
9. **Machinery should review itself.** 47 findings against the loop, 18 fixed and verified by
   the loop's own method.
10. **Build by running.** Every component the workbench trusts was shaped by a failure on a real
    target; the paths never exercised on a real target (v1 → close under the new gates) are the
    ones it distrusts.

---

## 7. What the workbench is missing that the blueprint designed

1. **A map.** Application map, symbol index, reachability, flow ids. Today lens selection is
   "which paths did the diff touch". Unknown reachability should weigh severity and cannot.
2. **Execution-confirmed bugs.** A sandbox (or at minimum a red test) before a 🔴 counts.
3. **Deterministic scanners through an ingest step.** Dependency CVEs, SAST, config lint —
   cheap, pinned, treated as untrusted data.
4. **A run budget with a mechanism.** Dispatch count + sandbox sessions, "stopped on budget"
   as a recorded outcome; fix rounds metered.
5. **A single-writer store layout with a run lock.** Would have prevented SF18 by construction.
6. **A fix hand-off that is a reviewed change.** `correlation_id`-keyed request → bolt →
   verification, rather than a patch inside the loop.
7. **A standing-code posture.** Periodic sweeps that drain the 255-row backlog; today "every
   new bolt must sweep its area" is the only periodic mechanism and it is manual.
8. **Intent grounding with a firewall**, eventually. Direct reading of bolt docs works
   pre-merge; a whole-codebase inspector needs an oracle that the Builder cannot write to.
9. **Injection resistance as a measured property.** The poison fixture; "obeying a suppression
   comment in code is a regression".
10. **Three-audience bug records.** Plain summary, developer detail, reproduction steps — the
    ledger's detail block is developer-only.

---

## 8. The 43 briefs against what exists

Judgment, not measurement: ✓ built in spirit · ◐ partial · ✗ missing.

| Phase | Brief | Workbench equivalent | State |
|---|---|---|---|
| 1 | `ledger-io` | `records/ledger.mjs`, `mint-id.mjs`, `render-records.mjs` | ✓ |
| 1 | `bug-documentation` | ledger row + fix brief (no three-audience record) | ◐ |
| 1 | `deduplication` | `reconcile-findings` skill, ground-truth scored | ✓ |
| 1 | `report-rendering` | `review-v<n>.md`, `summary-v<n>.md`, doc gate | ✓ |
| 1 | `triage-intake` | owner gates, parked decisions, decisions attached to re-finds | ✓ |
| 1 | `general-hunter` | the core six lenses | ✓ |
| 1 | `orchestrator` | `loop-driver` + router + `discovery-review.wf.js` | ✓ |
| 2 | `severity-scoring` | four levels + convergence weight; no reachability, no risk score | ◐ |
| 2 | `tool-ingest` | — | ✗ |
| 2 | `bug-verifier` | skeptics (argument only, no execution) | ◐ |
| 2 | `git-revision-tracking` | affirmed sha, `verify/git.mjs`; no moved/fixed detection across runs | ◐ |
| 2 | orchestrator wiring (11b) | router rows | ✓ |
| 3 | `app-mapping` | — | ✗ |
| 3 | `code-index` | — | ✗ |
| 3 | `reachability` (+14b) | — | ✗ ✗ |
| 3 | `flow-tracing` | lens prompts trace flows by hand | ◐ |
| 3 | `taint-analysis` | — | ✗ |
| 3 | `flow-tracer-agent` | lenses | ◐ |
| 3 | `file-sweeper-agent` | lenses (no tools-first) | ◐ |
| 3 | `security-auditor-agent` | `security` lens | ✓ |
| 3 | `dependency-audit-agent` | — | ✗ |
| 3 | `config-auditor-agent` | — | ✗ |
| 3 | `concurrency-auditor-agent` | `race` lens | ✓ |
| 3 | `root-cause-clustering` | fixer clusters, reconciler lineage | ◐ |
| 3 | `intent-lookup` | `requirements` lens reads bolt docs directly (no oracle) | ✗ |
| 3 | oracle/scale extensions (24b, 24c) | — | ✗ ✗ |
| 3 | orchestrator scale ext (24d) | delta cap, lens selection by touched area; no budget unit | ◐ |
| 4 | `suppression-learning` | replaced by decision attachment (by design) | ✗ |
| 4 | `bug-lifecycle` | statuses, reopen, lineage | ✓ |
| 4 | `eval-corpus` | seeded run 1 protocol; no standing corpus, no poison fixture | ◐ |
| 4 | `eval-metrics` | `metrics.jsonl`, track record; recall unproven | ◐ |
| 4 | `curator-agent` (+29b) | system self-review, speed report (manual) | ◐ ◐ |
| 5 | `regression-harvest` | fixer's red-first tests + test-meaning audit | ◐ |
| 5 | `fix-verification` | `verify-fixes.mjs` | ✓ |
| 5 | mailbox scan (31b) | router's verification row | ✓ |
| 5 | `fix-proposal` | fixer applies directly (blueprint: never apply) | ◐ |
| 5 | `fix-request-emit` | — (fixer is in-loop) | ✗ |
| opt | SARIF, `issue-sync`, `ci-gate` | — | ✗ ✗ ✗ |

**Totals: 12 built in spirit · 15 partial · 16 missing.** Phase 1 is done. Phase 3 (map,
breadth, scale) is the hole. The 16 missing pieces are the honest scope of a re-planned
intent 035, not 43.

---

## 9. The thesis can stand on the workbench today

The proposal's research question: *can a sole-writer ledger plus a correlation-id-keyed
verification loop guarantee conflict-free coordination and sound fix-confirmation, while
detecting real defects at a useful rate?* Its evaluation plan asks for detection rate,
false-positive rate, loop-closure correctness, time-to-verify, and a case study.

The workbench already holds, on a real .NET + Angular codebase:

- a ledger with never-reused ids, sole-writer renderer and stamper, immutable review files —
  and one recorded concurrency incident (duplicate id minting across worktrees, SF18) that
  motivates the single-history rule better than any argument;
- a closed verification loop with 234 verified fixes and 6 reopened (loop-closure correctness
  ≈ 97%), plus 2 certifications with 0 escapes so far;
- skeptic verdicts (`refuted`) as a false-positive proxy on every pass;
- a seeded-defect protocol and one run (10/10 recall, which the notes correctly call
  uninformative), with the second, harder run designed and costed;
- time and cost per finding (median 25 minutes per fixed finding; ~293k tokens per serious
  finding).

What is missing for the thesis is exactly what is missing for the workbench: the recall
number from seeded run 2. The Inspector pipeline the plan's M2 wants to "implement" is, in
its Phase-1-and-a-half form, already implemented; M2 becomes "extend and formalize".

---

## 10. The merge: mechanics and hygiene

**Git.** `origin/main` is the merge base; the branch is a fast-forward of docs, two agent
definitions and two `.github` edits. No file is touched on both sides. Under the README's own
entry tiers this is a docs-only change: one quick pass or skip.

**Hygiene the merge should carry or immediately follow:**

| Item | Where | Count |
|---|---|---|
| Says SQLite is the local/test database; `main` has been Postgres-only since 2026-08-20 | `future/test-quality-system.md`, intents 029/032/033, bolt 070, both `docs/analysis/` files | 17 files |
| Cites `docs/agent-systems/reviews/cross-system-review-v*.md`, deleted in the same branch | both build guides | 8 citations |
| Renames `docs/architecture-analysis-2026-05-25.md` → `docs/analysis/…`, breaking links from `main`-side files | intents 013–022, bolt 041 plan, story-index, one archived resolution | 23 links |
| `architect-analyst` description says 10 proposals, body says 20 | `.github/agents/architect-analyst.agent.md` | 1 |
| Intent 035 says 42 stories; 43 exist | `requirements.md` | 1 |
| "main is stale, 43 commits behind" — June state | `docs/planning/bolt-parallel-plan-2026-06-05.md` | 1 |
| Inception 035 "human review" checkbox unchecked | `inception-log.md` | 1 |
| Roster/status lines contradicted by this document | `future/README.md`, `ARCHITECTURE.md` §1, `future/code-review-system.md`, `README.md` | 4 docs |

**Merge inventory (the rest of the branch, one line each):**

| Group | Kind | Status | Touches agentic tooling |
|---|---|---|---|
| `docs/agent-systems/` specs + `future/` | design | specced / captured | yes |
| `thesis/` | proposal | draft | describes the systems |
| Intent 035 + bolts 085–094 | bug-hunter inception | planned; 091 gated on a knowledge-builder | yes |
| `.claude/agents/` planner + wave-orchestrator | agent defs | built | yes |
| `.github/` architect-analyst edits | agent def | edit | yes |
| `docs/analysis/`, `docs/planning/` | June records | historical | partly |
| Intents 025–034 + bolts 054–084 | product inceptions (hardening, layering, tests, e2e, environments, refunds, EU study) | planned; none implemented on `main` | no |
| 21 `bolt.md` status normalisations, `maintenance-log.md`, `story-index.md` (+881 lines) | bookkeeping | — | no |

---

## 11. Next steps — in the order the evidence suggests

Options with cost. The owner sets the sequence; the roadmap (bolts → AI infra → e2e/regression
→ environments → EU → deploy) is untouched by any of these.

| # | Step | What it settles | Effort / cost |
|---|---|---|---|
| 0 | **Merge the branch, then fix the hygiene table** (§10) | Stale SQLite claims, dangling citations, broken links stop propagating | Docs; one short session |
| 1 | **Re-baseline the blueprint to reality.** Roster: bug-hunter "partially built as the review loop (pre-merge mode)"; Reviewer "three dimensions run as lenses; verdict synthesis and contract-fidelity missing"; ARCHITECTURE §1 gets the built box; intent 035 re-scoped from 43 briefs to the 16 missing (§8) with the workbench's components named as the Phase 1–2 implementation | The two worlds share one map; future work starts from what exists | Docs; one session |
| 2 | **Decide the posture: one engine, two modes.** Pre-merge gate (today) + periodic standing sweep (the blueprint's posture; drains the backlog). Recommended over building a second inspector from the briefs | Whether bolts 085–094 mean "extend the loop" or "build another ledger beside it" | Owner decision; write it as an ADR |
| 3 | **Run seeded-bug run 2.** Different implanter, harder seeds, per-severity recall. The only item both worlds rest on: the workbench's stop rule (SF14), every certification, and the thesis's central metric | The recall number | ~2–2.5M tokens; deferred twice; the owner's call |
| 4 | **Feed the workbench from the blueprint**, cheapest and most evidenced first: (a) budget unit + metered fix rounds; (b) a red test before any 🔴 counts; (c) dependency/SAST ingest; (d) the Map slot (index, reachability); (e) fix round as a mini-bolt with named gates | The Phase 2–3 gaps, without a second system | (a)–(c) afternoons each; (d) a bolt; (e) docs + one skill edit |
| 5 | **Feed the blueprint from the workbench.** The ten lessons of §6 into the guide and the contract: sampling and repetition, seed rate, blinding, never-suppress, verifier ≠ fixer inside a system, records gate, summary discipline, self-review, build-by-running | The guide stops re-deriving what the workbench paid for | Docs; one session |
| 6 | **Ground the thesis on the workbench's data** and re-phrase M2 as "extend and formalize" | The thesis gains a real dataset now | Writing |
| — | *Not next:* the knowledge-builder build, the Conductor, the `team-ci` profile, packaging the kit (needs an external adopter), deployment | | |

Practice-side items that stand regardless (`reviews/notes/open-items.md`): merge PR #12 into
the invoicing branch or drop it; run one fresh target through the rebuilt loop v1 → close;
first shakedown of the skill evals.

---

## 12. Questions for the owner

1. **Posture.** One engine with two modes, or a separate bug-hunter built from the briefs
   beside the review loop? (§5 D, §11 step 2.)
2. **Records home.** Keep per-target records on the feature branch with `archive/` on `main`,
   or move to the contract's single store on `main`? (§5 F.)
3. **Execution proof for 🔴.** Require a red test before a 🔴 finding counts, accepting the
   cost? (§5 C.)
4. **Seeded run 2.** Approve at ~2–2.5M tokens now, or explicitly keep building on unproven
   recall? Both worlds ask this same question. (§11 step 3.)
5. **Intent 035.** Re-scope to the 16 missing pieces, or keep the 43-brief plan as written?
6. **Knowledge-builder priority.** Direct reading of bolt docs found 23 serious findings on two
   targets without an oracle; the Map slot is the bigger practical hole. Does the KB stay
   ahead of the Map in the build order?

---

## 13. Rulings and outcome (2026-09)

The owner answered the six questions of §12 on 2 September 2026 — ruling D6 was corrected the
same day — and the plan that follows from them
([reconciliation-plan-2026-09.md](reconciliation-plan-2026-09.md)) was executed on this branch
in six phases and 42 commits before this section's own. In one sentence: there is one inspection engine, not two — the
review loop that already exists — and the blueprint was rewritten to say so. Its roster,
architecture summary, build guides and integration contract now describe the loop as the
engine's **pre-merge mode** and name a scheduled **standing sweep** over `main` as the second
mode, still to be built. The ten lessons the loop paid for are written into the bug-hunter
guide; intent 035 was re-scoped from a build-from-scratch plan into the 31 pieces the loop is
missing; and each side now links to the other through this document. Nothing was built and
nothing was run: the whole change set is documents. Sections 1–12 above are left exactly as
they were written on 2 September — they are the analysis as it stood; this section is what came
of it.

### 13.1 The rulings

| Decision | Ruling | Applied by |
|---|---|---|
| **D1 — Posture: what is the engine?** | **a — one engine, two modes.** The review loop is the one Inspector engine. Pre-merge mode (what runs today on a branch before it merges) is built; standing-sweep mode (a scheduled sweep of the whole codebase on `main`) is not. | P2.1, P2.2, P2.5, P2.7 |
| **D2 — Where records live** | **c — working copy on the branch, canonical store on `main`.** A target's folder on the feature branch is the working copy; the records become the canonical ones under `reviews/` once the branch merges. Each open target reserves a range of finding ids at open, so two worktrees running in parallel cannot hand out the same id. | P2.7, P5.1 |
| **D3 — Execution proof before a 🔴 counts** | **b — a failing test, written by someone who is not the fixer.** Without that test the finding is recorded one severity lower and tagged `unproven-high`. | P2.7, P3.2 |
| **D4 — Build order** | **c — cheapest gaps first.** Run budget and metered fix rounds → the proof rule of D3 → scanner ingest (dependency and static-analysis tools feeding the loop) → the Map slot (code index and reachability) → standing-sweep mode → the knowledge-builder only if findings start showing the code drifting from its stated intent. | P2.1, P2.6, P2.7, P4.* |
| **D5 — Shape of the intent 035 re-scope** | **b — rewritten in place.** Intent 035 keeps its number and is re-scoped; bolts 085 and 086 are retired as already satisfied by the loop; bolts 087–094 remain, re-briefed around the 31 missing or partial pieces of §8. | P4.1–P4.4 |
| **D6 — The Reviewer's missing parts** | **d — build deferred, open decision settled.** The Reviewer's remaining dimensions stay deferred; the open decision in the concept note is closed: reviewing error handling and silent failures belongs to the Inspector, in its `observability` lens. | P2.3 |
| **D7 — Seeded-bug run 2** | **a — after the merge, scheduled by the owner.** | P5.2 (open item 6) |

### 13.2 The smaller calls the plan took itself (S1–S8)

Phase 0 of the plan lists eight decisions it settled without asking; each stays open to being
overturned.

- **S1** — `suppression-learning` (the blueprint's idea of teaching the system to stop raising a
  finding) is replaced by attaching the decision to the finding: never suppress a hunter. Three
  of the first five re-raised findings were later overturned.
- **S2** — Lessons enter the guides as additive "extends" top-ups under new changelog blocks
  (bug-hunter v3.7, knowledge-builder v3.6, contract v1.6); nothing is thrown away.
- **S3** — Both vocabularies stay; the Rosetta table (Appendix A) is placed in both READMEs; no
  renames on either side.
- **S4** — Historical analyses get a dated "snapshot" note rather than a rewrite; planning
  documents still marked `planned` are corrected to the Postgres-only reality.
- **S5** — The one old link inside `reviews/archive/038-039-invoicing/resolution-v9.md` stays;
  archived records are historical.
- **S6** — This document is the bridge both READMEs link to, and it gains this closing section.
- **S7** — The contract gains a `reviews/**` store row and the review loop as a consumer, rather
  than a second contract being written.
- **S8** — Thesis edits are proposed as a diff the owner approves before anything is committed.

### 13.3 What changed, by phase

- **Before the phases** — this analysis and the plan itself (`1c69cd1`), then the corrected D6
  ruling recorded in the plan (`2ff0c78`).
- **Phase 1 — hygiene** (8 commits): the eight citations of deleted review files became
  git-history pointers, the links broken by the May-analysis rename were repaired, the SQLite
  claims in the June planning documents were corrected to Postgres-only, and three small stale
  facts (proposal count, brief count, stale-branch note) were fixed.
  `73b978f`, `d6ac571`, `736c1b8`, `fcf37dc`, `05905c8`, `b2c18d5`, `73d73f8`, `86006e7`.
- **Phase 2 — the blueprint re-baselined to reality** (14): the future roster and the
  architecture summary gained a "what exists today" account; the code-review concept note went
  to partial with its open decision closed; the specs index and the READMEs link to the loop;
  the bug-hunter guide maps its 43 briefs onto what the loop already does and defines the two
  modes; the knowledge-builder moved behind the Inspector gaps in the build order; and the
  integration contract reached v1.6 with the records rule (D2), the proof rule (D3), the build
  order (D4) and the loop as a named consumer. `996bfd5`, `fca6f9a`, `e874289`, `cb512b0`,
  `3569f7f`, `49b33bc`, `397ad59`, `4eac711`, `ce74b73`, `ce02a87`, `d6bc2a0`, `3238757`,
  `60ce803`, `9768bc0`.
- **Phase 3 — the loop's lessons into the blueprint** (6): the ten lessons of §6 written into
  the bug-hunter guide, brief extensions for Phases 1–2 and 4–5 (dedup, records, triage,
  verifier, seed rate, escapes, the fix round as a mini-bolt), every suppression-learning
  mention marked superseded, and tests added to the new extensions. `d1f20ea`, `ae103ea`,
  `92d1e75`, `caec8c6`, `60ca72d`, `36af383`.
- **Phase 4 — intent 035 re-scoped** (8): requirements re-scoped to the gaps the loop leaves,
  units re-cut around them, bolts 085–086 retired and 087–094 re-briefed at the loop's seams in
  `reviews/lib`, and the story index, maintenance log, pinned decisions and inception record
  brought in line. `963133b`, `adf1444`, `1238c9b`, `f37c609`, `5004d54`, `b415e40`, `e35d989`,
  `1b6d92c`.
- **Phase 5 — the workbench points back** (4): `reviews/README.md` now names the blueprint it
  implements, the design notes and open items record the reconciliation (seeded run 2 is open
  item 6), and the contract's sections are named by the rules they hold, including the
  standing-sweep mode. `7afcecd`, `0428314`, `7b7ff40`, `3ac17f8`.
- **Phase 6 — thesis wording**: no commit. The edits are proposed as a patch and await the
  owner's approval (S8).
- **Phase 7 — bridge, verification, hand-back**: this section is its first commit; a
  whole-branch verification and an independent review follow. The companion artifact page was
  republished with the rulings after the final review.

### 13.4 Known residue

Left deliberately for the final review or for the owner, one line each:

- The thesis proposal's M3 and §2 still read as if the Inspector does not exist — part of the
  Phase 6 patch that awaits approval.
- 34 story acceptance criteria under intent 035 still say "created via skill-creator"; the
  construction box on each bolt governs instead.
- The inception log's `Human review complete` checkbox is still unticked — an owner action, not
  an agent's.
- `ARCHITECTURE.md` §5's mermaid diagram still draws the June build interleave; it is labelled
  superseded rather than redrawn.
- The contract's §6 heading, "Twin-name discipline", under-describes the judgment-agent rules
  the section now holds.
- A dead `DatabaseProvider` environment variable lingers in `docker-compose.yml` and
  `docker-compose.prod.yml` — outside this docs-only plan.
- `docs/prevention-sweep-idea.md` says "not scheduled" in its frontmatter and "adopted into the
  blueprint" in its body; which one is true is the owner's call.
- The seeded-bug run 2 is still the one measurement both sides rest on.

### 13.5 What this means for §11 and §12

Of §11's steps, 0, 1, 2 and 5 were done here — the hygiene before the merge rather than after
it, the re-baseline in Phases 2 and 4, the posture written into the contract, the lessons in
Phase 3. Step 6 (the thesis) is written but waits on the owner's approval. Steps 3 (seeded run
2) and 4 (feeding the workbench: budget, proof rule, scanner ingest, the Map slot, the fix round
as a mini-bolt) are post-merge work, in the order D4 sets.

§12's six questions are answered above: posture by D1, records home by D2, execution proof by
D3, seeded run 2 by D7, intent 035 by D5, and the knowledge-builder's place in the build order
by D4.

---

## Appendix A — Rosetta stone

| Blueprint | Workbench | Note |
|---|---|---|
| bug-hunter / Inspector | the review loop, "the machinery" | same engine, different posture |
| run | pass | a pass has a type: discovery, delta, verification, certification |
| hunter (general, flow-tracer, file-sweeper, specialists) | lens | 11 keyed lenses in the manifest |
| bug-verifier | skeptic | argument, not execution |
| bug ledger `bug-hunting/bug-ledger.json` | `reviews/<target>/ledger.md` + `state/backlog.md` | per target vs single |
| bug id / `correlation_id` | `PPW-<n>` (system target: `SF<n>`) | both never reused |
| deduplication | reconciliation (`reconcile-findings`) | workbench adds lineage |
| Critical / High / Medium / Low | 🔴 / 🟠 / 🟡 / ⚪ | "critical/blocker" banned words in the workbench |
| `fix-verification`, `verified-fixed` | `verify-fixes.mjs`, `verified` | revert-and-rerun |
| `closed-unverified` | `no-test`, `test-never-red`, `revert-broke-build` | workbench is finer-grained |
| fix-request mailbox | resolution file + router row | in-loop vs hand-off |
| triage-intake, inbox | owner gate, `gate-parked`, owner summary | async in both |
| `suppression_patterns` | decisions attached to re-finds | conflict; workbench wins |
| `eval-corpus` / `eval-metrics` | seeded-bug experiment / `metrics.jsonl` + track record | recall unproven in both |
| curator health summary | system self-review (`SF` ledger), speed report | |
| run budget, "stopped on budget" | delta cap 600k, router cost table | mechanism missing in the workbench |
| application map / code index / reachability | — | missing in the workbench |
| knowledge-builder oracle, `intent-lookup` | `requirements` lens reading bolt docs | firewall missing in the workbench |
| pre-merge run (read-only, advisory) | the whole loop | the workbench made the advisory mode the main event |
| certification | — | the blueprint has no "done" |

## Appendix B — Sources

Blueprint: `docs/agent-systems/*.md`, `docs/agent-systems/future/*.md`, `thesis/*.md`,
`memory-bank/intents/035-bug-hunter-agent-system/**`, `memory-bank/bolts/085–094`.
Workbench: `reviews/README.md`, `reviews/runbooks/`, `reviews/rules/`, `reviews/notes/`
(`self-driving-loop-design.md`, `rationale.md`, `open-items.md`), `reviews/state/`,
`reviews/system/ledger.md`, `reviews/archive/*/metrics.jsonl`, `reviews/lib/**`,
`.claude/skills/{loop-driver,fix-review,owner-summary,reconcile-findings}/SKILL.md`,
`docs/prevention-sweep-idea.md`. Lens-attribution counts computed from the archived
`metrics.jsonl` of 038-039 and 044-045 (the only targets with per-finding lens data), new
findings only, severities `high` and `medium`.
