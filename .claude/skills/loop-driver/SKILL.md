---
name: loop-driver
description: >-
  Drive the review loop for a target: mechanically pick the next pass, announce its cost,
  stop at every owner gate, run it, and leave the records clean — one pass per invocation,
  or drive the whole loop under the written policy when the owner says "unattended".
  Use this whenever the user says "continue the review loop for <target>", "drive the loop",
  "next pass for <target>", "what's next for <target>", "keep the loop going", or names a
  review target and asks to review/re-review/certify/verify it — even if they don't say the
  word "loop". Also use it when unsure where a target stands in the review process: the
  router readout is the answer. The mechanical router is the decision surface — hand-read
  the README router table only when the router itself abstains ("no row matched").
---

# Loop driver

The executable form of the README's standing instruction. One invocation = one **reviewed
unit** — a pass, or a fix round together with the verification that closes it — driven end to
end: **route → announce → (gate) → execute → record → report**. The README and the
runbooks own every rule; this skill only sequences them — when in doubt about a rule, the
rule file wins.

Why this exists: the router used to be a table a human executed from memory, and the
recorded failure modes of that (passes launched without cost warnings, records appended
inconsistently, gates drifted into "deviations") are what this skill closes. Each step below
exists because skipping it has already cost real money or trust at least once.

## 1 · Audit, then route — before anything else

```
node reviews/lib/records-auditor.mjs <target>     # must exit clean FIRST
node reviews/lib/route-next-pass.mjs <target>     # state → next pass → cost → gates
```

- **Auditor errors → repair the records before trusting any routing.** Broken records mean
  the router is deciding on wrong inputs. Repairs follow metrics-schema.md: past lines are
  never edited — a correction is its own appended line.
- Router exit **2** (owner gate) or **3** (judgment call): relay its output to the owner as
  a plain question and **stop this invocation**. Never pre-answer a gate. Certification
  always waits for an explicit go-ahead, even when it looks inevitable — a certification is
  the owner deciding to spend millions of tokens, not a step that happens to them.
  Stamp the wait: append `gate-open` (with the question as `reason`) to
  `reviews/<target>/worklog.jsonl` when relaying, and the invocation that consumes the
  answer appends `gate-closed` first thing — that span is the loop's measured
  blocked-on-owner time.
- A `QUEUED:` line means open 🟠 below the queue threshold: they wait rather than earn a
  round of their own, and they must drain in a **sweep** fix round before certification —
  both the router and the autonomy policy answer `fix round` for that sweep. At the
  loop-close gate the router prints no `QUEUED:` line by design, because 🟠 open at a
  certification are the documented norm; the close reads the open 🟠 from the ledger itself
  (section 6).
- If the router prints "no row matched", route by hand from
  [reviews/README.md](../../../reviews/README.md)'s router table using the facts it printed,
  and say in the report that this pass was hand-routed.
- The target's folder may not exist yet (first review). The router then says full discovery;
  the entry tier (README, "Entry tiers") decides full-loop vs ordinary treatment — that
  choice is the owner's if the change touches money, auth, data loss, concurrency,
  migrations, or new external input.

## 2 · Announce, then hold

Before anything runs, one line to the owner: current state → next pass → expected cost (the
router prints both; repeat them, don't re-derive). What "hold" means, precisely:

- **Router exit 0, cheap pass** (verification, fix-round handoff): announce, then proceed in
  this same invocation.
- **Router exit 0, discovery-scale** (full or delta discovery): the announce line comes
  first in the reply, then the launch — same invocation, per the README standing rule
  ("state the pass type and expected cost in one line" — a statement, not a question). If
  the owner has said they want launches held for an ack, that preference wins from then on.
- **Certification-grade launches and loop closures: never in the same breath.** Stop and
  wait for the explicit go-ahead, every time — these are the owner deciding to spend
  millions of tokens or to end a loop, and the recorded deviations cluster exactly here.
  Inside an unattended run, the standing approval is that go-ahead (see "Unattended runs").

**Session-model guard:** discovery-scale fan-outs launched from a Fable session have died on
the session limit mid-run (recorded in runbook-discovery, Launch). If the current session is
Fable, say so in the announce line and let the owner choose: switch sessions, or proceed
resume-ready. On any mid-run death, resume — `Workflow({ scriptPath, resumeFromRunId })` —
completed agents return from cache; relaunching from scratch re-spends everything, and the
run journal (not guesswork) explains an empty result.

## 3 · Execute the pass

| Router says | Do |
|---|---|
| full discovery · delta discovery · certification | Follow `reviews/runbooks/runbook-discovery.md` exactly — scoping, lens manifest, launch args, synthesis. The delta lens cap (5) and token budget (600k) are script-enforced; don't fight them. |
| lens-coverage discovery (<lens>) | A discovery pass per the same runbook, full scope, lenses = the one owed lens + completeness-critic. It exists to clear lens-coverage debt before certification (README note ³). |
| design pass (owner gate) | Only on the owner's go-ahead: a fix round per the `/fix-review` skill whose first artifact is a component-level protocol block, reimplementation against it, then discovery. Its metrics `notes` must carry `design-pass:<area>` — that is how the router counts the one-per-component cap. |
| verification (reviewed unit) | Follow `reviews/runbooks/runbook-verification.md`. A fix round and its verification are **one reviewed unit**: run the verification immediately after the round's hand-back, in this same invocation, at the round's tip — then render the unit's records and sit its single doc gate (section 4). You must not be the fixer — sole exception: the test-only rule written in its step 1. Self-verification is the exact bias the loop exists to prevent. |
| fix round | Invoke the **/fix-review** skill for the fixing — it is the sole owner of the fixer contract, and you never fix findings inline. You do not stop at its hand-back, though: the round is half a reviewed unit, so its verification, records and gate are yours in this same invocation. A sweep of queued 🟠 routes here too; drain the ids the router named. |
| loop CLOSED | Report the closure line and stop. The target is under watch: a new serious finding in its files re-arms the loop and may be a `post-cert-escape` (reviews/state/track-record.md). |

### Persistent fixer

Inside one target, keep **one** fixer subagent and continue it: send the next unit's findings
to the **same** agent with `SendMessage` (its id or name from the first launch) — never a
fresh `Agent` call, which starts a new agent with none of the context. A fixer that already carries this target's protocol
blocks, cluster map, trigger classifications and revert proofs re-reads none of it, and its
round-scope review gets sharper across units.

Retire the fixer — the next unit gets a fresh agent — after **any** discovery-type pass (full,
delta, lens-coverage, certification), **any reopened fix**, or the target's close. Those are
exactly the moments a carried-over mental model is the hazard: a blind pass exists to find what
the fixer could not see, and a reopened fix is proof its author's reading of the code was
wrong. The rule is the same in an unattended run, and the run-end report names how many units
each fixer served.

## 4 · Records — the pass didn't happen until they exist

**Stamp the pass's runtime** — through the stamper, never by hand (it owns the timestamp and
enforces each event's required fields):

```
node reviews/lib/wl.mjs <target> pass-launch --pass <pass> --type verification
node reviews/lib/wl.mjs <target> pass-records-done --pass <pass>
```

`pass-launch` goes immediately before the pass's work starts — for a reviewed unit that
means before the records commit, not after it (step 1 below). `pass-records-done` goes **before**
the renderer runs, not after the auditor: the renderer measures the
`pass-launch`→`pass-records-done` span and refuses to render an unclosed one. The metrics
line's `runtime: {started, ended}` copies those two timestamps. Fix rounds stamp themselves
(the `/fix-review` skill owns its own worklog events).

**Two numbers, never the same one.** `<pass>` is the verification's own pass number — the
next free one in `metrics.jsonl`, counted across every pass type (a target can read
discovery v1, verification v2, v3, v4, delta v6). `<round>` is the fix round's number, which
is also its resolution's version. The stamps and the renderer take `v<pass>`; the doc gate
takes `<round>`, because the round's resolution is the only file the unit writes.

**The reviewed unit — a fix round and its verification record once, together.** When the fixer
hands back (`round-end` stamped, code and resolution committed, `status: resolved`), continue
in this same invocation:

1. Stamp `pass-launch` **first**, then commit every pending `reviews/<target>/` record append
   — one records commit, `docs(review): …` subject — so the stamp itself is inside that
   commit. The order matters: `worklog.jsonl` is tracked, so a stamp taken after the commit
   dirties the tree again and `verify-fixes.mjs` exits 2 before doing anything. The commit
   also picks up whatever worklog stamps the fixer left uncommitted.
2. Verify at the round's tip: `node reviews/lib/verify-fixes.mjs <target>`. The mechanical
   revert-and-rerun covers **every** `fixed` row of the resolution — `--only PPW-1,PPW-2`
   narrows a **re-run**, never the first one, and the runbook's evidence-audit sampling audits
   the fixer's recorded claims rather than replacing this run. Then the runbook's judgment
   items. The script buffers its `verify-result` events and flushes them once after the last
   row, so the run **ends with `worklog.jsonl` dirty**: that is by design, and committing it
   is yours.

   A row the script cannot verdict mechanically — `revert-broke-build`, `env-missing`,
   `no-test`, anything you settled with a hand proof — gets its stamp from you, explicitly:
   `node reviews/lib/wl.mjs <target> verify-result --id PPW-<n> --verdict held --commit <sha>`.
   Skip it and `render-records.mjs --verification` finds no result for that id and leaves the
   row at `fixed` — a fix that reads unverified forever.
3. Stamp `pass-records-done`, then render both halves — the round first (it reads the
   resolution's Findings rows), the verification second (it reads the span's `verify-result`
   events):

```
node reviews/lib/render-records.mjs <target> --outcome "<what the round did>"
node reviews/lib/render-records.mjs <target> --verification v<pass> --outcome "<what held>" [--commit <sha>]
```

   `--outcome` is mandatory on both, at most 50 words, and may carry neither a `|` nor a line
   break. Every check runs before the first write, so a refusal leaves `metrics.jsonl`,
   `index.md` and `ledger.md` all untouched — read what it named, repair that record, re-run.
4. Commit the records the renderer just wrote, then **push the branch** — the round's tip and
   the records commit both. The auditor refuses a commit that is reachable from no pushed ref
   ("evidence is single-machine"), and step 3 just wrote fresh shas into the records, so the
   auditor cannot pass before the push.
5. `node reviews/lib/records-auditor.mjs <target>` — must exit clean.
6. **One doc-gate sitting for the whole unit** (below): `node reviews/lib/doc-gate.mjs
   <target> <round>` plus one judge dispatch over the round's and the verification's changed
   files together, never one per half.

**Resuming a round you did not run.** A fix round handed back in an earlier invocation is the
same unit: the router answers `verification (reviewed unit — render records once, after it)`,
and you enter the sequence at step 1 exactly as above — nothing in it needs you to have been
the fixer, and nothing may be re-rendered ahead of the verification.

For a reviewed unit the fix-round and verification records are **rendered, not hand-written**
— the renderer writes the two metrics lines, the two index rows and the ledger status flips
the `verify-result` events imply; the prose that stays a human's is the resolution body and
the summary. Discovery-type passes keep their existing records flow, below.

In the runbooks' order: ledger update via **reconcile-findings** (discovery-type passes,
*before* the review file so it can reference the minted ids; verification updates statuses per its own
runbook) · metrics.jsonl line (schema v3 — discovery lines carry the per-finding `findings[]`
array and every pass line carries `runtime`) · index.md row · summary page via
**owner-summary** (decision passes only — a verification pass writes **no files**; its
outcome is the ledger flips, worklog, metrics and index row, reported at the owner gate in
chat, per `reviews/rules/doc-contracts.md`). These are written at synthesis time because they are
unreconstructable later — that is the metrics schema's founding lesson. Then:

```
node reviews/lib/records-auditor.mjs <target>     # must exit clean before hand-back
```

**The doc gate — before anything reaches the owner.** After the records, run both halves:

```
node reviews/lib/doc-gate.mjs <target> <pass>     # structure lint — must exit clean
```

then spawn the **Sonnet judge** (Agent, `model: sonnet`): input = `reviews/rules/doc-contracts.md`
plus the round's new/changed `reviews/` files. Its scope is the **hand-written prose** —
language against the vocabulary, evidence links that support the claims they hang on, real
reasons in "Reasons to doubt" — not the rendered cells. Its output is approve, or disapprove
with, per violation, the **exact replacement text** to use; a correction it cannot make
without changing a recorded fact (a number, an id, a sha, a verdict) comes back as a
**question** instead of text. Append a `doc-gate` worklog event with the verdict.

**The judge is dispatched once per reviewed unit** — one sitting over the round's and the
verification's files together. On disapprove, apply its returned replacement text **verbatim**
and re-run the lint; re-judge only the items it returned as questions, once you have answered
them. Re-judging text it wrote itself is a second dispatch that decides nothing. Neither half
of the gate edits anything — the lint reports, the judge writes the text, you apply it; don't
game the lint. The summary is not handed to the owner and the review file is not declared
immutable until both halves pass.

## 5 · Close out

Re-run `route-next-pass.mjs`. Report to the owner: what ran, the headline result in one
plain sentence (the summary page carries the depth — don't restate it), the new state, and
what the router says comes next with its cost. Then **stop** — unless the owner said
**"until a gate"** this invocation, in which case loop back to step 1 and continue,
still stopping at every exit-2/3 gate and before every discovery-scale launch.

## Unattended runs — "run the review loop unattended for <target>"

The "until a gate" mode, extended by the written delegation in
`reviews/lib/autonomy-policy.mjs`. One run = the whole remaining loop, driven to
`loop CLOSED`, certification included. The owner's "unattended" instruction is the
standing approval (2026-08-20): it is the explicit go-ahead the Never list requires for
certification-grade launches and the owner's word for the close — the run behaves as if
the owner approved each step, and reports every delegated decision at the end.
Everything else in this skill still applies per pass — audit, records, doc gate.
Consulting the written policy is not pre-answering a gate: the delegation is the owner's
standing decision, and any gate kind without one stops the run. There is no token or pass
limit on an unattended run — the owner removed them on purpose; do not invent one. The
run stops early only when it needs something no rule can supply: a policy `stop`
(including an unknown gate kind), a fixer question only the owner can answer, records
that stay broken after one repair, or the no-progress guard.

**Open the run.** Append `run-start` to the worklog. Announce in one line: target, state,
and that the run drives to close — certification included — reporting every decision at
the end.

**Each iteration:**

1. Audit + route as in step 1. Auditor red → one repair attempt; still red → end the run.
2. Router exit 0: stamp `pass-launch` as usual — before the records commit, per section 4's
   step 1 — and execute the pass in a subagent (table below). If a fix-round subagent reports a decision blocking a 🔴 fix (the
   `/fix-review` skill's Blocker exception), end the run with that question in the report.
3. Router exit 2/3: run `node reviews/lib/autonomy-policy.mjs <target> decide
   <GATE_KIND>` — in an unattended run, an exit 3 always goes to the policy this way and
   is never hand-routed from the README table (step 1's manual fallback does not apply
   here). `ACTION: auto` → append `gate-parked` (`{kind, default, reason}`) and take the
   printed `NEXT`. The policy's whole vocabulary is `fix round` · `delta discovery` ·
   `lens-coverage discovery (<lens>)` · `certification (pair)` · `certification (single)` ·
   `close the loop`, and every one of them is executed exactly like a router answer (back to
   2) — including `fix round`, which is how the policy answers a pre-certification sweep of
   open 🟠, and `lens-coverage discovery (<lens>)`, the lean full-scope pass on one owed lens.
   `close the loop` executes section 6's close sequence — the standing approval is the owner's
   word it requires. `ACTION: stop` → end the run with the gate's question in the report; a
   `design-pass` gate always stops, as does any gate override logged after the run started.
4. No-progress guard — measured in **units**, not passes: a fix round and its verification
   append their metrics lines together, at the unit's end, so a round mid-unit legitimately
   shows no new line. If the routed work repeats the previous unit's shape and
   `metrics.jsonl` gained no line across that whole completed unit, end the run — something
   is not recording. This is a breakage detector, not a limit.
5. Sweep stall detector: when a `fix round` is routed to drain queued 🟠, stamp the id set
   the router or the policy named, so a restarted run recovers the basis instead of
   re-deriving it — `node reviews/lib/wl.mjs <target> note --reason "sweep basis" --ids
   PPW-1,PPW-2`. If a later sweep is routed and the ledger's open-🟠 set is the same or
   larger than the newest such note, the sweep is not draining — end the run and report those
   ids with what each round did to them. A queue that never drains is a loop that can never
   certify, and it will otherwise route `fix round` forever without ever repeating a pass
   type.
6. Router prints `loop CLOSED` → the run is done; close it out.

<!-- generated:gate-kinds -->
| Gate kind | Router exit | The router means | The written policy answers |
|---|---|---|---|
| `loop-close` | 2 | certification passed and no post-cert fix round is pending | auto — `close the loop` on the standing approval |
| `delta-worthiness` | 3 | the latest line is a clean verification | auto — `delta discovery` when the round fixed a 🔴, else the certification answer |
| `certification-go-ahead` | 2 | loop quiet, nothing open, lens coverage complete | auto — `certification (pair)` or `certification (single)`, unless open work or a convergence blocker answers first |
| `design-pass` | 2 | two consecutive rounds seeded the same component at s ≥ 0.3 | stop — reimplementing a component is the owner's call |
| `no-metrics` | 3 | review files exist but there is no `metrics.jsonl` | stop — no written delegation |
| `records-broken` | 3 | `metrics.jsonl` carries no usable pass line | stop — no written delegation |
| `no-row-matched` | 3 | no router row matched mechanically | stop — no written delegation |
<!-- /generated:gate-kinds -->

**Pass execution — always in subagents in this mode** (the driver only routes, records,
and reports; subagents return a summary of at most 20 lines, and state is re-read from
the records, never from the subagent's prose):

| Pass | How |
|---|---|
| full / delta discovery / certification | as section 3 — the workflow script already fans out; run synthesis + records per runbook-discovery (certification pair = two blinded passes per README note ²) |
| lens-coverage discovery (<lens>) | the same runbook, full scope, lenses = the one owed lens + completeness-critic; it clears one lens of the coverage debt that refuses certification |
| reviewed unit (fix round + verification) | one subagent — the **persistent fixer** above — instructed to load the `/fix-review` skill and follow its **Unattended variant** section; it hands back at `round-end` plus the round's commit. Then, in the driver and in this same iteration: `pass-launch` and then the records commit that contains it, `verify-fixes.mjs` at that tip — the full mechanical run over every `fixed` row, plus the runbook's sampled evidence audit of the fixer's recorded proofs when the round recorded per-fix levers (`--only` is for re-runs) — one subagent for the runbook's judgment items — given the script's JSON output, the round review's findings, the resolution and the fix diff — then the two renderer calls, the records commit, the **push** of the round tip and that commit, the auditor, and ONE doc-gate sitting for the unit (section 4) |
| design pass | never automatic: `autonomy-policy.mjs` answers `stop` on the `design-pass` gate kind, because reimplementing a component is the owner's call. It also stops before a fix round of its own — armed or sweep — once the convergence brake declares that component non-convergent, the same brake the router applies to every fix-round answer (README, "Where the convergence rule bites"). The run ends with that question |

The session-model guard still applies: on a Fable session, discovery-scale launches
proceed resume-ready, and the workflow runId goes into the worklog event.

**Close the run.** Append `run-end` (`{passes, parked}`). Report in one message: each
pass with its one-line outcome, every parked item (kind, the default taken, what needs
the owner's ruling), how many units each fixer served before it was retired (the persistent-fixer
rule above), and how the run ended (loop closed, or the question it stopped on).
This report is the batched owner sitting — each ruling made on it is recorded where that
round's rules say (resolution `Decisions`, ledger rows, the backlog).

## 6 · Closing a loop — the owner said "close it"

Archiving is the last step of recording the close, in exactly this order (README, Files &
conventions):

1. `closed: <date> — <how>` into the ledger frontmatter; the index row records the story.
2. **Backlog rollup — read the ledger, not the router.** At the close gate the router prints
   no `QUEUED:` line: 🟠 still open when a certification passes are the documented norm, and
   they must not pre-empt the owner's close decision. So open `ledger.md` and give one line
   in `reviews/state/backlog.md` (`PPW-<n>`, target, severity, what, area — template
   `templates/backlog.md`) to every row still at `backlog` **and** to every 🟠 still
   `open`/`in-progress`, which stands down here rather than arming another round. 🔴,
   reopened fixes and fix-caused 🟠 regressions still arm the loop even at this gate — if one
   is open, the close is not what comes next.
3. `archived: <date>` on the target's index row.
4. `git mv reviews/<target> reviews/archive/<target>` — contents unchanged, nothing rewritten.

Dormancy: when routing shows a target with no pass in 30+ days and nothing serious open,
*offer* archiving in the report — never move a folder the owner didn't ask about. A
`post-cert-escape` on an archived target moves its folder back out of `archive/` before the
re-armed pass runs.

## Never

- Launch a certification-grade pass without an explicit go-ahead given in this invocation.
  An unattended run's opening instruction is that go-ahead (standing approval 2026-08-20).
- Close a loop yourself — `closed:` goes into the ledger frontmatter only on the owner's
  word, and the index row records how it closed. An unattended run carries that word
  (standing approval 2026-08-20); the close is reported at run end.
- Mark anything `verified` while being the fixer, outside the written test-only exemption.
- Render or gate a fix round before its verification has run — the unit records once, after
  the verification, and a rendered half-unit is a record that has to be corrected later.
- Edit a `review-v*.md`, skip a record, or hand back with the auditor red.
- Create a `reviews/<target>/` folder except by executing a pass the owner requested for
  that target — a defect noticed along the way is proposed at the round's owner gate, and
  the owner either routes it into `reviews/state/backlog.md` (one row, the next `PPW-<n>` from
  `reviews/state/id-counter`) or drops it, with the ruling written into that round's resolution
  `Decisions`. No owner decision is ever recorded that the owner did not state in so many
  words, and nothing is dropped without one.
- Chain past a gate because the answer "seems obvious" — obvious-looking gates are where
  the recorded deviations clustered.
